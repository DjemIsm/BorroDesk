using BorroDesk.Api.Entities.Enums;

namespace BorroDesk.Api.DTOs.Tickets;

public sealed class TicketSummaryResponse
{
    public int Id { get; init; }

    public required string Title { get; init; }

    public TicketStatus Status { get; init; }

    public TicketPriority Priority { get; init; }

    public required TicketUserResponse CreatedBy { get; init; }

    public TicketUserResponse? AssignedTo { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public DateTime? ClosedAt { get; init; }

    public bool CanEdit { get; init; }

    public bool CanAssign { get; init; }

    public bool CanDelete { get; init; }
}
