using BorroDesk.Api.Authorization;
using BorroDesk.Api.DTOs.Users;
using BorroDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BorroDesk.Api.Controllers;

[Authorize(Roles = ApplicationRoles.Support + "," + ApplicationRoles.Admin)]
[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserLookupService userLookupService) : ControllerBase
{
    [HttpGet("assignable")]
    [ProducesResponseType<IReadOnlyCollection<AssignableUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<AssignableUserResponse>>> GetAssignableUsers(
        CancellationToken cancellationToken)
    {
        var users = await userLookupService.GetAssignableUsersAsync(cancellationToken);
        return Ok(users);
    }
}
