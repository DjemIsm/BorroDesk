using System.ComponentModel.DataAnnotations;

namespace BorroDesk.Api.DTOs.Tickets;

public sealed class CreateTicketCommentRequest
{
    [Required]
    [MinLength(1)]
    public required string Text { get; init; }
}
