using BorroDesk.Api.DTOs.Tickets;
using BorroDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BorroDesk.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController(ITicketService ticketService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<TicketSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<TicketSummaryResponse>>> GetTickets(
        [FromQuery] TicketQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.GetTicketsAsync(User, queryParameters, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> GetTicket(int id, CancellationToken cancellationToken)
    {
        var result = await ticketService.GetTicketAsync(User, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TicketResponse>> CreateTicket(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.CreateTicketAsync(User, request, cancellationToken);
        if (result.Status != TicketServiceResultStatus.Success || result.Value is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetTicket), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> UpdateTicket(
        int id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.UpdateTicketAsync(User, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> ChangeTicketStatus(
        int id,
        ChangeTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.ChangeTicketStatusAsync(User, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("{id:int}/assignment")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> AssignTicket(
        int id,
        AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.AssignTicketAsync(User, id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/comments")]
    [ProducesResponseType<TicketCommentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketCommentResponse>> AddTicketComment(
        int id,
        CreateTicketCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.AddTicketCommentAsync(User, id, request, cancellationToken);
        if (result.Status != TicketServiceResultStatus.Success || result.Value is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetTicket), new { id }, result.Value);
    }

    [HttpPost("{id:int}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<TicketAttachmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketAttachmentResponse>> UploadTicketScreenshot(
        int id,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.UploadTicketScreenshotAsync(User, id, file, cancellationToken);
        if (result.Status != TicketServiceResultStatus.Success || result.Value is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetTicketAttachment),
            new { id, attachmentId = result.Value.Id },
            result.Value);
    }

    [HttpGet("{id:int}/attachments/{attachmentId:int}")]
    [Produces("image/png", "image/jpeg", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketAttachment(
        int id,
        int attachmentId,
        [FromQuery] bool download,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.GetTicketAttachmentFileAsync(User, id, attachmentId, cancellationToken);
        if (result.Status != TicketServiceResultStatus.Success || result.Value is null)
        {
            return ToFileFailureActionResult(result);
        }

        return PhysicalFile(
            result.Value.PhysicalPath,
            result.Value.ContentType,
            download ? result.Value.FileName : null,
            enableRangeProcessing: true);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTicket(int id, CancellationToken cancellationToken)
    {
        var result = await ticketService.DeleteTicketAsync(User, id, cancellationToken);
        if (result.Status == TicketServiceResultStatus.Success)
        {
            return NoContent();
        }

        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(TicketServiceResult<T> result)
    {
        return result.Status switch
        {
            TicketServiceResultStatus.Success => Ok(result.Value),
            TicketServiceResultStatus.Unauthorized => this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                result.Message),
            TicketServiceResultStatus.Forbidden => this.ApiProblem(
                StatusCodes.Status403Forbidden,
                "Access denied.",
                result.Message),
            TicketServiceResultStatus.NotFound => this.ApiProblem(
                StatusCodes.Status404NotFound,
                "Ticket was not found.",
                result.Message),
            TicketServiceResultStatus.BadRequest => this.ApiProblem(
                StatusCodes.Status400BadRequest,
                "Invalid ticket request.",
                result.Message),
            TicketServiceResultStatus.Conflict => this.ApiProblem(
                StatusCodes.Status409Conflict,
                "Ticket request conflict.",
                result.Message),
            _ => this.ApiProblem(
                StatusCodes.Status500InternalServerError,
                "Unexpected ticket error.")
        };
    }

    private IActionResult ToActionResult(TicketServiceResult result)
    {
        return result.Status switch
        {
            TicketServiceResultStatus.Unauthorized => this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                result.Message),
            TicketServiceResultStatus.Forbidden => this.ApiProblem(
                StatusCodes.Status403Forbidden,
                "Access denied.",
                result.Message),
            TicketServiceResultStatus.NotFound => this.ApiProblem(
                StatusCodes.Status404NotFound,
                "Ticket was not found.",
                result.Message),
            TicketServiceResultStatus.BadRequest => this.ApiProblem(
                StatusCodes.Status400BadRequest,
                "Invalid ticket request.",
                result.Message),
            TicketServiceResultStatus.Conflict => this.ApiProblem(
                StatusCodes.Status409Conflict,
                "Ticket request conflict.",
                result.Message),
            _ => this.ApiProblem(
                StatusCodes.Status500InternalServerError,
                "Unexpected ticket error.")
        };
    }

    private IActionResult ToFileFailureActionResult<T>(TicketServiceResult<T> result)
    {
        return result.Status switch
        {
            TicketServiceResultStatus.Unauthorized => this.ApiProblem(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                result.Message),
            TicketServiceResultStatus.Forbidden => this.ApiProblem(
                StatusCodes.Status403Forbidden,
                "Access denied.",
                result.Message),
            TicketServiceResultStatus.NotFound => this.ApiProblem(
                StatusCodes.Status404NotFound,
                "Attachment was not found.",
                result.Message),
            TicketServiceResultStatus.BadRequest => this.ApiProblem(
                StatusCodes.Status400BadRequest,
                "Invalid attachment request.",
                result.Message),
            TicketServiceResultStatus.Conflict => this.ApiProblem(
                StatusCodes.Status409Conflict,
                "Attachment request conflict.",
                result.Message),
            _ => this.ApiProblem(
                StatusCodes.Status500InternalServerError,
                "Unexpected attachment error.")
        };
    }
}
