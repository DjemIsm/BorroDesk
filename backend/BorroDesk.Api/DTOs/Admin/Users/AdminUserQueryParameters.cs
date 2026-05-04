using System.ComponentModel.DataAnnotations;

namespace BorroDesk.Api.DTOs.Admin.Users;

public sealed class AdminUserQueryParameters
{
    [StringLength(200)]
    public string? Search { get; init; }

    [StringLength(50)]
    public string? Role { get; init; }

    public bool? IsActive { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;
}
