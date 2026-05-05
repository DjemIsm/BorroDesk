using BorroDesk.Api.Authorization;
using BorroDesk.Api.Data;
using BorroDesk.Api.DTOs.Users;
using Microsoft.EntityFrameworkCore;

namespace BorroDesk.Api.Services;

public sealed class UserLookupService(
    BorroDeskDbContext dbContext) : IUserLookupService
{
    public async Task<IReadOnlyCollection<AssignableUserResponse>> GetAssignableUsersAsync(
        CancellationToken cancellationToken)
    {
        var assignableRoles = new[] { ApplicationRoles.Support, ApplicationRoles.Admin };

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive
                && dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id
                    && dbContext.Roles.Any(role => role.Id == userRole.RoleId
                        && role.Name != null
                        && assignableRoles.Contains(role.Name))))
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Id)
            .Select(user => new AssignableUserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            })
            .ToArrayAsync(cancellationToken);
    }
}
