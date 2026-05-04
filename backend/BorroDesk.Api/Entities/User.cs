using BorroDesk.Api.Enums;

namespace BorroDesk.Api.Entities;

public class User
{
    public int Id { get; set; }

    public required string UserName { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

    public ICollection<TicketComment> TicketComments { get; set; } = new List<TicketComment>();

    public ICollection<TicketAttachment> TicketAttachments { get; set; } = new List<TicketAttachment>();
}
