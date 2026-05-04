using System.ComponentModel.DataAnnotations;
using BorroDesk.Api.Entities.Enums;

namespace BorroDesk.Api.DTOs.Tickets;

public sealed class ChangeTicketStatusRequest
{
    [Required]
    public TicketStatus Status { get; init; }
}
