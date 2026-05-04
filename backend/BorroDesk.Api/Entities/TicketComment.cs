namespace BorroDesk.Api.Entities;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public required string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
