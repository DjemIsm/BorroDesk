namespace BorroDesk.Api.DTOs.Admin.Users;

public sealed class AdminUserResponse
{
    public int Id { get; init; }

    public string? UserName { get; init; }

    public string? Email { get; init; }

    public bool EmailConfirmed { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }
}
