namespace BorroDesk.Api.DTOs.Tickets;

public sealed class TicketUserResponse
{
    public int Id { get; init; }

    public string? UserName { get; init; }

    public string? Email { get; init; }
}
