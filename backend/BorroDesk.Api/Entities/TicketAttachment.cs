namespace BorroDesk.Api.Entities;

public class TicketAttachment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public int UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    public required string FileName { get; set; }

    public required string StoredFileName { get; set; }

    public required string FilePath { get; set; }

    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
