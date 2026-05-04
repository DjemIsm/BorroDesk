using System.Globalization;
using System.Security.Claims;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.DTOs.Admin.Users;
using BorroDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BorroDesk.Api.Services;

public sealed class AdminUserService(
    UserManager<User> userManager) : IAdminUserService
{
    private const int MaxPageSize = 100;

    public async Task<AdminUserServiceResult<AdminUsersPagedResponse>> GetUsersAsync(
        AdminUserQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateQuery(queryParameters);
        if (validationMessage is not null)
        {
            return BadRequest<AdminUsersPagedResponse>(validationMessage);
        }

        var query = userManager.Users.AsNoTracking();

        if (queryParameters.IsActive.HasValue)
        {
            query = query.Where(user => user.IsActive == queryParameters.IsActive.Value);
        }

        var search = queryParameters.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.UserName != null && user.UserName.Contains(search)
                || user.Email != null && user.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Role))
        {
            var role = NormalizeRole(queryParameters.Role);
            if (role is null)
            {
                return BadRequest<AdminUsersPagedResponse>($"Unsupported role '{queryParameters.Role}'.");
            }

            var usersInRole = await userManager.GetUsersInRoleAsync(role);
            var userIdsInRole = usersInRole.Select(user => user.Id).ToArray();
            query = query.Where(user => userIdsInRole.Contains(user.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)queryParameters.PageSize);

        var users = await query
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Id)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var response = new AdminUsersPagedResponse
        {
            Items = await ToResponsesAsync(users),
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return AdminUserServiceResult<AdminUsersPagedResponse>.Success(response);
    }

    public async Task<AdminUserServiceResult<AdminUserResponse>> GetUserAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound<AdminUserResponse>();
        }

        return AdminUserServiceResult<AdminUserResponse>.Success(await ToResponseAsync(user));
    }

    public async Task<AdminUserServiceResult<AdminUserResponse>> CreateUserAsync(
        AdminCreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeUserFields(request.UserName, request.Email, out var userName, out var email, out var validationMessage))
        {
            return BadRequest<AdminUserResponse>(validationMessage);
        }

        if (!TryNormalizeRoles(request.Roles, out var roles, out validationMessage))
        {
            return BadRequest<AdminUserResponse>(validationMessage);
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            IsActive = request.IsActive
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest<AdminUserResponse>(FormatErrors(createResult));
        }

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return BadRequest<AdminUserResponse>(FormatErrors(roleResult));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var createdUser = await userManager.Users
            .AsNoTracking()
            .SingleAsync(createdUser => createdUser.Id == user.Id, cancellationToken);

        return AdminUserServiceResult<AdminUserResponse>.Success(await ToResponseAsync(createdUser));
    }

    public async Task<AdminUserServiceResult<AdminUserResponse>> UpdateUserAsync(
        ClaimsPrincipal actor,
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(actor, out var actorUserId))
        {
            return Unauthorized<AdminUserResponse>();
        }

        if (!TryNormalizeUserFields(request.UserName, request.Email, out var userName, out var email, out var validationMessage))
        {
            return BadRequest<AdminUserResponse>(validationMessage);
        }

        if (!TryNormalizeRoles(request.Roles, out var requestedRoles, out validationMessage))
        {
            return BadRequest<AdminUserResponse>(validationMessage);
        }

        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            return NotFound<AdminUserResponse>();
        }

        var currentRoles = (await userManager.GetRolesAsync(user)).ToArray();
        var isSelf = actorUserId == user.Id;
        var removesOwnAdminRole = isSelf
            && currentRoles.Contains(ApplicationRoles.Admin)
            && !requestedRoles.Contains(ApplicationRoles.Admin);
        if (removesOwnAdminRole)
        {
            return Conflict<AdminUserResponse>("You cannot remove your own admin role.");
        }

        if (isSelf && !request.IsActive)
        {
            return Conflict<AdminUserResponse>("You cannot deactivate your own admin account.");
        }

        if (await WouldRemoveLastActiveAdminAsync(user, currentRoles, requestedRoles, request.IsActive))
        {
            return Conflict<AdminUserResponse>("At least one active admin user must remain.");
        }

        user.UserName = userName;
        user.Email = email;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest<AdminUserResponse>(FormatErrors(updateResult));
        }

        var rolesToRemove = currentRoles.Except(requestedRoles).ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeRolesResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeRolesResult.Succeeded)
            {
                return BadRequest<AdminUserResponse>(FormatErrors(removeRolesResult));
            }
        }

        var rolesToAdd = requestedRoles.Except(currentRoles).ToArray();
        if (rolesToAdd.Length > 0)
        {
            var addRolesResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addRolesResult.Succeeded)
            {
                return BadRequest<AdminUserResponse>(FormatErrors(addRolesResult));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var updatedUser = await userManager.Users
            .AsNoTracking()
            .SingleAsync(updatedUser => updatedUser.Id == user.Id, cancellationToken);

        return AdminUserServiceResult<AdminUserResponse>.Success(await ToResponseAsync(updatedUser));
    }

    public async Task<AdminUserServiceResult> ResetPasswordAsync(
        int id,
        AdminResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            return AdminUserServiceResult.Failure(AdminUserServiceResultStatus.NotFound, "User was not found.");
        }

        if (await userManager.HasPasswordAsync(user))
        {
            var removePasswordResult = await userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                return AdminUserServiceResult.Failure(
                    AdminUserServiceResultStatus.BadRequest,
                    FormatErrors(removePasswordResult));
            }
        }

        var addPasswordResult = await userManager.AddPasswordAsync(user, request.Password);
        if (!addPasswordResult.Succeeded)
        {
            return AdminUserServiceResult.Failure(
                AdminUserServiceResultStatus.BadRequest,
                FormatErrors(addPasswordResult));
        }

        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return AdminUserServiceResult.Failure(
                AdminUserServiceResultStatus.BadRequest,
                FormatErrors(updateResult));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return AdminUserServiceResult.Success();
    }

    public async Task<AdminUserServiceResult> DeactivateUserAsync(
        ClaimsPrincipal actor,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(actor, out var actorUserId))
        {
            return AdminUserServiceResult.Failure(
                AdminUserServiceResultStatus.Unauthorized,
                "Authenticated user id is missing or invalid.");
        }

        var user = await userManager.FindByIdAsync(id.ToString(CultureInfo.InvariantCulture));
        if (user is null)
        {
            return AdminUserServiceResult.Failure(AdminUserServiceResultStatus.NotFound, "User was not found.");
        }

        if (actorUserId == user.Id)
        {
            return AdminUserServiceResult.Failure(
                AdminUserServiceResultStatus.Conflict,
                "You cannot deactivate your own admin account.");
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        if (await WouldRemoveLastActiveAdminAsync(user, roles, roles, isActive: false))
        {
            return AdminUserServiceResult.Failure(
                AdminUserServiceResultStatus.Conflict,
                "At least one active admin user must remain.");
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return AdminUserServiceResult.Failure(
                AdminUserServiceResultStatus.BadRequest,
                FormatErrors(updateResult));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return AdminUserServiceResult.Success();
    }

    private async Task<IReadOnlyCollection<AdminUserResponse>> ToResponsesAsync(IEnumerable<User> users)
    {
        var responses = new List<AdminUserResponse>();
        foreach (var user in users)
        {
            responses.Add(await ToResponseAsync(user));
        }

        return responses;
    }

    private async Task<AdminUserResponse> ToResponseAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AdminUserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = roles.Order(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private static string? ValidateQuery(AdminUserQueryParameters queryParameters)
    {
        if (queryParameters.PageNumber < 1)
        {
            return "Page number must be greater than zero.";
        }

        if (queryParameters.PageSize is < 1 or > MaxPageSize)
        {
            return $"Page size must be between 1 and {MaxPageSize}.";
        }

        return null;
    }

    private async Task<bool> WouldRemoveLastActiveAdminAsync(
        User targetUser,
        IReadOnlyCollection<string> currentRoles,
        IReadOnlyCollection<string> requestedRoles,
        bool isActive)
    {
        var targetIsActiveAdmin = targetUser.IsActive && currentRoles.Contains(ApplicationRoles.Admin);
        var targetWillBeActiveAdmin = isActive && requestedRoles.Contains(ApplicationRoles.Admin);
        if (!targetIsActiveAdmin || targetWillBeActiveAdmin)
        {
            return false;
        }

        var admins = await userManager.GetUsersInRoleAsync(ApplicationRoles.Admin);
        return !admins.Any(user => user.Id != targetUser.Id && user.IsActive);
    }

    private static bool TryGetActorUserId(ClaimsPrincipal actor, out int userId)
    {
        var rawUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawUserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out userId);
    }

    private static bool TryNormalizeUserFields(
        string? userName,
        string? email,
        out string normalizedUserName,
        out string normalizedEmail,
        out string validationMessage)
    {
        normalizedUserName = userName?.Trim() ?? string.Empty;
        normalizedEmail = email?.Trim() ?? string.Empty;

        if (normalizedUserName.Length == 0)
        {
            validationMessage = "User name is required.";
            return false;
        }

        if (normalizedUserName.Length > 100)
        {
            validationMessage = "User name cannot exceed 100 characters.";
            return false;
        }

        if (normalizedEmail.Length == 0)
        {
            validationMessage = "Email is required.";
            return false;
        }

        if (normalizedEmail.Length > 256)
        {
            validationMessage = "Email cannot exceed 256 characters.";
            return false;
        }

        validationMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeRoles(
        IReadOnlyCollection<string>? requestedRoles,
        out string[] roles,
        out string validationMessage)
    {
        var normalizedRoles = new List<string>();
        foreach (var requestedRole in requestedRoles ?? [])
        {
            var role = NormalizeRole(requestedRole);
            if (role is null)
            {
                roles = [];
                validationMessage = $"Unsupported role '{requestedRole}'.";
                return false;
            }

            if (!normalizedRoles.Contains(role))
            {
                normalizedRoles.Add(role);
            }
        }

        roles = normalizedRoles.Count == 0 ? [ApplicationRoles.User] : normalizedRoles.ToArray();
        validationMessage = string.Empty;
        return true;
    }

    private static string? NormalizeRole(string? role)
    {
        var trimmedRole = role?.Trim();
        return ApplicationRoles.All.FirstOrDefault(applicationRole =>
            string.Equals(applicationRole, trimmedRole, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }

    private static AdminUserServiceResult<T> Unauthorized<T>()
    {
        return AdminUserServiceResult<T>.Failure(
            AdminUserServiceResultStatus.Unauthorized,
            "Authenticated user id is missing or invalid.");
    }

    private static AdminUserServiceResult<T> NotFound<T>()
    {
        return AdminUserServiceResult<T>.Failure(AdminUserServiceResultStatus.NotFound, "User was not found.");
    }

    private static AdminUserServiceResult<T> BadRequest<T>(string message)
    {
        return AdminUserServiceResult<T>.Failure(AdminUserServiceResultStatus.BadRequest, message);
    }

    private static AdminUserServiceResult<T> Conflict<T>(string message)
    {
        return AdminUserServiceResult<T>.Failure(AdminUserServiceResultStatus.Conflict, message);
    }
}
