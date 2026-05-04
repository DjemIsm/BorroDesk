namespace BorroDesk.Api.DTOs.Auth;

public sealed class LoginResponse
{
    public required string AccessToken { get; init; }

    public required string TokenType { get; init; }

    public DateTime ExpiresAt { get; init; }

    public int UserId { get; init; }

    public string? UserName { get; init; }

    public string? Email { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }
}
