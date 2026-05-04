using System.Security.Claims;
using BorroDesk.Api.DTOs.Tickets;
using Microsoft.AspNetCore.Http;

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

    Task<TicketServiceResult<TicketCommentResponse>> AddTicketCommentAsync(
        ClaimsPrincipal user,
        int ticketId,
        CreateTicketCommentRequest request,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketAttachmentResponse>> UploadTicketScreenshotAsync(
        ClaimsPrincipal user,
        int ticketId,
        IFormFile file,
        CancellationToken cancellationToken);

    Task<TicketServiceResult<TicketAttachmentFileResponse>> GetTicketAttachmentFileAsync(
        ClaimsPrincipal user,
        int ticketId,
        int attachmentId,
        CancellationToken cancellationToken);

    Task<TicketServiceResult> DeleteTicketAsync(
        ClaimsPrincipal user,
        int id,
        CancellationToken cancellationToken);
}
