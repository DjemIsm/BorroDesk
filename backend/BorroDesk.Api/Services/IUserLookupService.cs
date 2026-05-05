using BorroDesk.Api.DTOs.Users;

namespace BorroDesk.Api.Services;

public interface IUserLookupService
{
    Task<IReadOnlyCollection<AssignableUserResponse>> GetAssignableUsersAsync(CancellationToken cancellationToken);
}
