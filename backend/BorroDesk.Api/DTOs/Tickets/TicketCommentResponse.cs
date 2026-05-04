namespace BorroDesk.Api.DTOs.Tickets;

public sealed class TicketCommentResponse
{
    public int Id { get; init; }

    public int TicketId { get; init; }

    public required TicketUserResponse Author { get; init; }

    public required string Text { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}
