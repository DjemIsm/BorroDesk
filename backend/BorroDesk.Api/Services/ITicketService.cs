using System.Security.Claims;
using BorroDesk.Api.DTOs.Tickets;

namespace BorroDesk.Api.Services;

public interface ITicketService
{
    Task<TicketServiceResult<PagedResponse<TicketSummaryResponse>>> GetTicketsAsync(
        ClaimsPrincipal user,
        TicketQueryParameters queryParameters,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketResponse>> GetTicketAsync(
        ClaimsPrincipal user,
        int id,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketResponse>> CreateTicketAsync(
        ClaimsPrincipal user,
        CreateTicketRequest request,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketResponse>> UpdateTicketAsync(
        ClaimsPrincipal user,
        int id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketResponse>> ChangeTicketStatusAsync(
        ClaimsPrincipal user,
        int id,
        ChangeTicketStatusRequest request,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketResponse>> AssignTicketAsync(
        ClaimsPrincipal user,
        int id,
        AssignTicketRequest request,
        CancellationToken cancellationToken);

    Task<TicketServiceResult> DeleteTicketAsync(
        ClaimsPrincipal user,
        int id,
        CancellationToken cancellationToken);
}
