using System.ComponentModel.DataAnnotations;
using BorroDesk.Api.Entities.Enums;

namespace BorroDesk.Api.DTOs.Tickets;

public sealed class TicketQueryParameters
{
    public TicketStatus? Status { get; init; }

    public TicketPriority? Priority { get; init; }

    public int? AssignedToUserId { get; init; }

    public int? CreatedByUserId { get; init; }

    [StringLength(200)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;
}
