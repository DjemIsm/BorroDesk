using System.ComponentModel.DataAnnotations;
using BorroDesk.Api.Entities.Enums;

namespace BorroDesk.Api.DTOs.Tickets;

public sealed class UpdateTicketRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; init; }

    [Required]
    [MinLength(1)]
    public required string Description { get; init; }

    [Required]
    public TicketPriority Priority { get; init; }
}
