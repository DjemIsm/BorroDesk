using System.ComponentModel.DataAnnotations;

namespace BorroDesk.Api.DTOs.Admin.Users;

public sealed class AdminResetUserPasswordRequest
{
    [Required]
    [MinLength(6)]
    public required string Password { get; init; }
}
