namespace BorroDesk.Api.DTOs.Users;

public sealed class AssignableUserResponse
{
    public int Id { get; init; }

    public string? UserName { get; init; }

    public string? Email { get; init; }
}
