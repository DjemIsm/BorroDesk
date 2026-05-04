using System.Security.Claims;
using BorroDesk.Api.DTOs.Admin.Users;

namespace BorroDesk.Api.Services;

public interface IAdminUserService
{
    Task<AdminUserServiceResult<AdminUsersPagedResponse>> GetUsersAsync(
        AdminUserQueryParameters queryParameters,
        CancellationToken cancellationToken);

    Task<AdminUserServiceResult<AdminUserResponse>> GetUserAsync(
        int id,
        CancellationToken cancellationToken);

    Task<AdminUserServiceResult<AdminUserResponse>> CreateUserAsync(
        AdminCreateUserRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserServiceResult<AdminUserResponse>> UpdateUserAsync(
        ClaimsPrincipal actor,
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserServiceResult> ResetPasswordAsync(
        int id,
        AdminResetUserPasswordRequest request,
        CancellationToken cancellationToken);

    Task<AdminUserServiceResult> DeactivateUserAsync(
        ClaimsPrincipal actor,
        int id,
        CancellationToken cancellationToken);
}
