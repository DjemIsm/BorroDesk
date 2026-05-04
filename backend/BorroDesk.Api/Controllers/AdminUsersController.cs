using BorroDesk.Api.Authorization;
using BorroDesk.Api.DTOs.Admin.Users;
using BorroDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BorroDesk.Api.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
[ApiController]
[Route("api/admin/users")]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AdminUsersPagedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUsersPagedResponse>> GetUsers(
        [FromQuery] AdminUserQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await adminUserService.GetUsersAsync(queryParameters, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserResponse>> GetUser(int id, CancellationToken cancellationToken)
    {
        var result = await adminUserService.GetUserAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        AdminCreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminUserService.CreateUserAsync(request, cancellationToken);
        if (result.Status != AdminUserServiceResultStatus.Success || result.Value is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> UpdateUser(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminUserService.UpdateUserAsync(User, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        int id,
        AdminResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminUserService.ResetPasswordAsync(id, request, cancellationToken);
        return result.Status == AdminUserServiceResultStatus.Success
            ? NoContent()
            : ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken cancellationToken)
    {
        var result = await adminUserService.DeactivateUserAsync(User, id, cancellationToken);
        return result.Status == AdminUserServiceResultStatus.Success
            ? NoContent()
            : ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(AdminUserServiceResult<T> result)
    {
        return result.Status switch
        {
            AdminUserServiceResultStatus.Success => Ok(result.Value),
            AdminUserServiceResultStatus.Unauthorized => this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                result.Message),
            AdminUserServiceResultStatus.Forbidden => this.ApiProblem(
                StatusCodes.Status403Forbidden,
                "Access denied.",
                result.Message),
            AdminUserServiceResultStatus.NotFound => this.ApiProblem(
                StatusCodes.Status404NotFound,
                "User was not found.",
                result.Message),
            AdminUserServiceResultStatus.BadRequest => this.ApiProblem(
                StatusCodes.Status400BadRequest,
                "Invalid user request.",
                result.Message),
            AdminUserServiceResultStatus.Conflict => this.ApiProblem(
                StatusCodes.Status409Conflict,
                "User request conflict.",
                result.Message),
            _ => this.ApiProblem(
                StatusCodes.Status500InternalServerError,
                "Unexpected user management error.")
        };
    }

    private IActionResult ToActionResult(AdminUserServiceResult result)
    {
        return result.Status switch
        {
            AdminUserServiceResultStatus.Unauthorized => this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                result.Message),
            AdminUserServiceResultStatus.Forbidden => this.ApiProblem(
                StatusCodes.Status403Forbidden,
                "Access denied.",
                result.Message),
            AdminUserServiceResultStatus.NotFound => this.ApiProblem(
                StatusCodes.Status404NotFound,
                "User was not found.",
                result.Message),
            AdminUserServiceResultStatus.BadRequest => this.ApiProblem(
                StatusCodes.Status400BadRequest,
                "Invalid user request.",
                result.Message),
            AdminUserServiceResultStatus.Conflict => this.ApiProblem(
                StatusCodes.Status409Conflict,
                "User request conflict.",
                result.Message),
            _ => this.ApiProblem(
                StatusCodes.Status500InternalServerError,
                "Unexpected user management error.")
        };
    }
}
