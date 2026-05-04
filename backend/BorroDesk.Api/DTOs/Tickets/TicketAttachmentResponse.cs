namespace BorroDesk.Api.DTOs.Tickets;

public sealed class TicketAttachmentResponse
{
    public int Id { get; init; }

    public int TicketId { get; init; }

    public required TicketUserResponse UploadedBy { get; init; }

    public required string FileName { get; init; }

    public required string StoredFileName { get; init; }

    public string? ContentType { get; init; }

    public long FileSizeBytes { get; init; }

    public DateTime UploadedAt { get; init; }
}
