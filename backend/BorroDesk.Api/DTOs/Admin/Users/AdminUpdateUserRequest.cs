using System.ComponentModel.DataAnnotations;

namespace BorroDesk.Api.DTOs.Admin.Users;

public sealed class AdminUpdateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string UserName { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyCollection<string> Roles { get; init; } = [];
}
