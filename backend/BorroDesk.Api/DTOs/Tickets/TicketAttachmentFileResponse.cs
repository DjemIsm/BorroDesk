namespace BorroDesk.Api.DTOs.Tickets;

public sealed class TicketAttachmentFileResponse
{
    public required string PhysicalPath { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }
}
