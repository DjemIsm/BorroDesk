using BorroDesk.Api.Entities.Enums;

namespace BorroDesk.Api.Entities;

public class Ticket
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public int CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public int? AssignedToUserId { get; set; }

    public User? AssignedToUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
