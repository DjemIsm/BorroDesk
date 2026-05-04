using System.Globalization;
using System.Security.Claims;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.Data;
using BorroDesk.Api.DTOs.Tickets;
using BorroDesk.Api.Entities;
using BorroDesk.Api.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BorroDesk.Api.Services;

public sealed class TicketService(
    BorroDeskDbContext dbContext,
    UserManager<User> userManager) : ITicketService
{
    private const int MaxPageSize = 100;

    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> StaffStatusTransitions =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.Open] =
                [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed],
            [TicketStatus.InProgress] =
                [TicketStatus.Open, TicketStatus.Resolved, TicketStatus.Closed],
            [TicketStatus.Resolved] =
                [TicketStatus.InProgress, TicketStatus.Closed, TicketStatus.Reopened],
            [TicketStatus.Closed] =
                [TicketStatus.Reopened],
            [TicketStatus.Reopened] =
                [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed]
        };

    private static readonly IReadOnlyDictionary<TicketStatus, TicketStatus[]> RequesterStatusTransitions =
        new Dictionary<TicketStatus, TicketStatus[]>
        {
            [TicketStatus.Open] =
                [TicketStatus.Closed],
            [TicketStatus.InProgress] =
                [TicketStatus.Closed],
            [TicketStatus.Resolved] =
                [TicketStatus.Closed, TicketStatus.Reopened],
            [TicketStatus.Closed] =
                [TicketStatus.Reopened],
            [TicketStatus.Reopened] =
                [TicketStatus.Closed]
        };

    public async Task<TicketServiceResult<PagedResponse<TicketSummaryResponse>>> GetTicketsAsync(
        ClaimsPrincipal user,
        TicketQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<PagedResponse<TicketSummaryResponse>>();
        }

        var validationMessage = ValidateTicketQuery(queryParameters);
        if (validationMessage is not null)
        {
            return BadRequest<PagedResponse<TicketSummaryResponse>>(validationMessage);
        }

        var query = dbContext.Tickets.AsNoTracking();

        if (actor.IsStaff)
        {
            if (queryParameters.CreatedByUserId.HasValue)
            {
                query = query.Where(ticket => ticket.CreatedByUserId == queryParameters.CreatedByUserId.Value);
            }

            if (queryParameters.AssignedToUserId.HasValue)
            {
                query = query.Where(ticket => ticket.AssignedToUserId == queryParameters.AssignedToUserId.Value);
            }
        }
        else
        {
            if (queryParameters.AssignedToUserId.HasValue
                || queryParameters.CreatedByUserId.HasValue && queryParameters.CreatedByUserId.Value != actor.UserId)
            {
                return Forbidden<PagedResponse<TicketSummaryResponse>>("Only support and admin users can filter tickets by user assignment.");
            }

            query = query.Where(ticket => ticket.CreatedByUserId == actor.UserId);
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(ticket => ticket.Status == queryParameters.Status.Value);
        }

        if (queryParameters.Priority.HasValue)
        {
            query = query.Where(ticket => ticket.Priority == queryParameters.Priority.Value);
        }

        var search = queryParameters.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearch(query, search);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)queryParameters.PageSize);

        var tickets = await query
            .Include(ticket => ticket.CreatedByUser)
            .Include(ticket => ticket.AssignedToUser)
            .OrderByDescending(ticket => ticket.UpdatedAt ?? ticket.CreatedAt)
            .ThenByDescending(ticket => ticket.Id)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<TicketSummaryResponse>
        {
            Items = tickets.Select(ticket => ToSummary(ticket, actor)).ToArray(),
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return TicketServiceResult<PagedResponse<TicketSummaryResponse>>.Success(response);
    }

    public async Task<TicketServiceResult<TicketResponse>> GetTicketAsync(
        ClaimsPrincipal user,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<TicketResponse>();
        }

        var ticket = await FindTicketWithUsersAsync(id, trackChanges: false, cancellationToken);
        if (ticket is null || !CanAccessTicket(ticket, actor))
        {
            return NotFound<TicketResponse>();
        }

        return TicketServiceResult<TicketResponse>.Success(ToDetail(ticket, actor));
    }

    public async Task<TicketServiceResult<TicketResponse>> CreateTicketAsync(
        ClaimsPrincipal user,
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<TicketResponse>();
        }

        if (!TryNormalizeTicketFields(request.Title, request.Description, out var title, out var description, out var validationMessage))
        {
            return BadRequest<TicketResponse>(validationMessage);
        }

        if (!IsDefined(request.Priority))
        {
            return BadRequest<TicketResponse>($"Unsupported ticket priority '{request.Priority}'.");
        }

        var ticket = new Ticket
        {
            Title = title,
            Description = description,
            Priority = request.Priority,
            CreatedByUserId = actor.UserId
        };

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdTicket = await FindTicketWithUsersAsync(ticket.Id, trackChanges: false, cancellationToken)
            ?? throw new InvalidOperationException($"Created ticket {ticket.Id} could not be loaded.");

        return TicketServiceResult<TicketResponse>.Success(ToDetail(createdTicket, actor));
    }

    public async Task<TicketServiceResult<TicketResponse>> UpdateTicketAsync(
        ClaimsPrincipal user,
        int id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<TicketResponse>();
        }

        if (!TryNormalizeTicketFields(request.Title, request.Description, out var title, out var description, out var validationMessage))
        {
            return BadRequest<TicketResponse>(validationMessage);
        }

        if (!IsDefined(request.Priority))
        {
            return BadRequest<TicketResponse>($"Unsupported ticket priority '{request.Priority}'.");
        }

        var ticket = await FindTicketWithUsersAsync(id, trackChanges: true, cancellationToken);
        if (ticket is null || !CanAccessTicket(ticket, actor))
        {
            return NotFound<TicketResponse>();
        }

        var editDecision = CanEditTicket(ticket, actor);
        if (!editDecision.Allowed)
        {
            return Failure<TicketResponse>(editDecision);
        }

        ticket.Title = title;
        ticket.Description = description;
        ticket.Priority = request.Priority;
        ticket.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return TicketServiceResult<TicketResponse>.Success(ToDetail(ticket, actor));
    }

    public async Task<TicketServiceResult<TicketResponse>> ChangeTicketStatusAsync(
        ClaimsPrincipal user,
        int id,
        ChangeTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<TicketResponse>();
        }

        if (!IsDefined(request.Status))
        {
            return BadRequest<TicketResponse>($"Unsupported ticket status '{request.Status}'.");
        }

        var ticket = await FindTicketWithUsersAsync(id, trackChanges: true, cancellationToken);
        if (ticket is null || !CanAccessTicket(ticket, actor))
        {
            return NotFound<TicketResponse>();
        }

        if (ticket.Status == request.Status)
        {
            return TicketServiceResult<TicketResponse>.Success(ToDetail(ticket, actor));
        }

        var transitionDecision = CanChangeStatus(ticket, request.Status, actor);
        if (!transitionDecision.Allowed)
        {
            return Failure<TicketResponse>(transitionDecision);
        }

        var now = DateTime.UtcNow;
        ticket.Status = request.Status;
        ticket.UpdatedAt = now;
        ticket.ClosedAt = request.Status == TicketStatus.Closed ? now : null;

        if (actor.IsSupport && !actor.IsAdmin && ticket.AssignedToUserId is null && request.Status == TicketStatus.InProgress)
        {
            ticket.AssignedToUserId = actor.UserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedTicket = await FindTicketWithUsersAsync(ticket.Id, trackChanges: false, cancellationToken)
            ?? throw new InvalidOperationException($"Updated ticket {ticket.Id} could not be loaded.");

        return TicketServiceResult<TicketResponse>.Success(ToDetail(updatedTicket, actor));
    }

    public async Task<TicketServiceResult<TicketResponse>> AssignTicketAsync(
        ClaimsPrincipal user,
        int id,
        AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<TicketResponse>();
        }

        var ticket = await FindTicketWithUsersAsync(id, trackChanges: true, cancellationToken);
        if (ticket is null || !CanAccessTicket(ticket, actor))
        {
            return NotFound<TicketResponse>();
        }

        var assignmentDecision = await CanAssignTicketAsync(ticket, request.AssignedToUserId, actor);
        if (!assignmentDecision.Allowed)
        {
            return Failure<TicketResponse>(assignmentDecision);
        }

        ticket.AssignedToUserId = request.AssignedToUserId;
        ticket.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedTicket = await FindTicketWithUsersAsync(ticket.Id, trackChanges: false, cancellationToken)
            ?? throw new InvalidOperationException($"Assigned ticket {ticket.Id} could not be loaded.");

        return TicketServiceResult<TicketResponse>.Success(ToDetail(updatedTicket, actor));
    }

    public async Task<TicketServiceResult<TicketCommentResponse>> AddTicketCommentAsync(
        ClaimsPrincipal user,
        int ticketId,
        CreateTicketCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return Unauthorized<TicketCommentResponse>();
        }

        if (!TryNormalizeCommentText(request.Text, out var text, out var validationMessage))
        {
            return BadRequest<TicketCommentResponse>(validationMessage);
        }

        var ticket = await dbContext.Tickets
            .SingleOrDefaultAsync(ticket => ticket.Id == ticketId, cancellationToken);
        if (ticket is null || !CanCommentOnTicket(ticket, actor))
        {
            return NotFound<TicketCommentResponse>();
        }

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            UserId = actor.UserId,
            Comment = text
        };

        ticket.UpdatedAt = DateTime.UtcNow;
        dbContext.TicketComments.Add(comment);

        await dbContext.SaveChangesAsync(cancellationToken);

        var createdComment = await dbContext.TicketComments
            .AsNoTracking()
            .Include(ticketComment => ticketComment.User)
            .SingleOrDefaultAsync(ticketComment => ticketComment.Id == comment.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Created ticket comment {comment.Id} could not be loaded.");

        return TicketServiceResult<TicketCommentResponse>.Success(ToComment(createdComment));
    }

    public async Task<TicketServiceResult> DeleteTicketAsync(
        ClaimsPrincipal user,
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(user, out var actor))
        {
            return TicketServiceResult.Failure(
                TicketServiceResultStatus.Unauthorized,
                "Authenticated user id is missing or invalid.");
        }

        var ticket = await dbContext.Tickets.SingleOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
        if (ticket is null || !CanAccessTicket(ticket, actor))
        {
            return TicketServiceResult.Failure(TicketServiceResultStatus.NotFound, "Ticket was not found.");
        }

        var deleteDecision = CanDeleteTicket(ticket, actor);
        if (!deleteDecision.Allowed)
        {
            return TicketServiceResult.Failure(deleteDecision.Status, deleteDecision.Message);
        }

        dbContext.Tickets.Remove(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TicketServiceResult.Success();
    }

    private static IQueryable<Ticket> ApplySearch(IQueryable<Ticket> query, string search)
    {
        if (int.TryParse(search, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticketId))
        {
            return query.Where(ticket => ticket.Id == ticketId
                || ticket.Title.Contains(search)
                || ticket.Description.Contains(search)
                || ticket.CreatedByUser != null
                    && ((ticket.CreatedByUser.UserName != null && ticket.CreatedByUser.UserName.Contains(search))
                        || (ticket.CreatedByUser.Email != null && ticket.CreatedByUser.Email.Contains(search)))
                || ticket.AssignedToUser != null
                    && ((ticket.AssignedToUser.UserName != null && ticket.AssignedToUser.UserName.Contains(search))
                        || (ticket.AssignedToUser.Email != null && ticket.AssignedToUser.Email.Contains(search))));
        }

        return query.Where(ticket =>
            ticket.Title.Contains(search)
            || ticket.Description.Contains(search)
            || ticket.CreatedByUser != null
                && ((ticket.CreatedByUser.UserName != null && ticket.CreatedByUser.UserName.Contains(search))
                    || (ticket.CreatedByUser.Email != null && ticket.CreatedByUser.Email.Contains(search)))
            || ticket.AssignedToUser != null
                && ((ticket.AssignedToUser.UserName != null && ticket.AssignedToUser.UserName.Contains(search))
                    || (ticket.AssignedToUser.Email != null && ticket.AssignedToUser.Email.Contains(search))));
    }

    private async Task<Ticket?> FindTicketWithUsersAsync(
        int id,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tickets.AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Include(ticket => ticket.CreatedByUser)
            .Include(ticket => ticket.AssignedToUser)
            .Include(ticket => ticket.Comments)
                .ThenInclude(comment => comment.User)
            .SingleOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);
    }

    private bool TryGetActor(ClaimsPrincipal user, out TicketActor actor)
    {
        var rawUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var parsed = int.TryParse(rawUserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId);
        actor = new TicketActor(
            parsed ? userId : 0,
            user.IsInRole(ApplicationRoles.Admin),
            user.IsInRole(ApplicationRoles.Support));

        return parsed;
    }

    private static string? ValidateTicketQuery(TicketQueryParameters queryParameters)
    {
        if (queryParameters.Status.HasValue && !IsDefined(queryParameters.Status.Value))
        {
            return $"Unsupported ticket status '{queryParameters.Status.Value}'.";
        }

        if (queryParameters.Priority.HasValue && !IsDefined(queryParameters.Priority.Value))
        {
            return $"Unsupported ticket priority '{queryParameters.Priority.Value}'.";
        }

        if (queryParameters.PageNumber < 1)
        {
            return "Page number must be greater than zero.";
        }

        if (queryParameters.PageSize is < 1 or > MaxPageSize)
        {
            return $"Page size must be between 1 and {MaxPageSize}.";
        }

        return null;
    }

    private async Task<RuleDecision> CanAssignTicketAsync(Ticket ticket, int? assignedToUserId, TicketActor actor)
    {
        if (!actor.IsStaff)
        {
            return RuleDecision.Forbidden("Only support and admin users can assign tickets.");
        }

        if (ticket.Status == TicketStatus.Closed && !actor.IsAdmin)
        {
            return RuleDecision.Conflict("Closed tickets cannot be reassigned unless an admin reopens or updates them.");
        }

        if (actor.IsSupport && ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value != actor.UserId)
        {
            return RuleDecision.Forbidden("Support users cannot reassign tickets owned by another support user.");
        }

        if (!assignedToUserId.HasValue)
        {
            return actor.IsAdmin || ticket.AssignedToUserId == actor.UserId || ticket.AssignedToUserId is null
                ? RuleDecision.Allow()
                : RuleDecision.Forbidden("Only admins can unassign another support user's ticket.");
        }

        if (actor.IsSupport && assignedToUserId.Value != actor.UserId)
        {
            return RuleDecision.Forbidden("Support users can only assign tickets to themselves.");
        }

        var assignee = await userManager.FindByIdAsync(assignedToUserId.Value.ToString(CultureInfo.InvariantCulture));
        if (assignee is null || !assignee.IsActive)
        {
            return RuleDecision.BadRequest("Assigned user was not found or is inactive.");
        }

        if (!await userManager.IsInRoleAsync(assignee, ApplicationRoles.Support)
            && !await userManager.IsInRoleAsync(assignee, ApplicationRoles.Admin))
        {
            return RuleDecision.BadRequest("Tickets can only be assigned to support or admin users.");
        }

        return RuleDecision.Allow();
    }

    private static RuleDecision CanEditTicket(Ticket ticket, TicketActor actor)
    {
        if (actor.IsAdmin)
        {
            return RuleDecision.Allow();
        }

        if (actor.IsSupport)
        {
            if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value != actor.UserId)
            {
                return RuleDecision.Forbidden("Support users cannot edit tickets assigned to another support user.");
            }

            return ticket.Status == TicketStatus.Closed
                ? RuleDecision.Conflict("Closed tickets cannot be edited unless an admin updates them.")
                : RuleDecision.Allow();
        }

        if (ticket.CreatedByUserId != actor.UserId)
        {
            return RuleDecision.Forbidden("You cannot edit this ticket.");
        }

        if (ticket.AssignedToUserId.HasValue)
        {
            return RuleDecision.Conflict("Assigned tickets can only be edited by support or admin users.");
        }

        return ticket.Status is TicketStatus.Open or TicketStatus.Reopened
            ? RuleDecision.Allow()
            : RuleDecision.Conflict("Requesters can edit only open or reopened tickets.");
    }

    private static RuleDecision CanChangeStatus(Ticket ticket, TicketStatus nextStatus, TicketActor actor)
    {
        if (actor.IsAdmin)
        {
            return CanTransition(ticket.Status, nextStatus, StaffStatusTransitions)
                ? RuleDecision.Allow()
                : RuleDecision.Conflict($"Cannot move ticket from {ticket.Status} to {nextStatus}.");
        }

        if (actor.IsSupport)
        {
            if (ticket.AssignedToUserId.HasValue && ticket.AssignedToUserId.Value != actor.UserId)
            {
                return RuleDecision.Forbidden("Support users cannot change status for tickets assigned to another support user.");
            }

            return CanTransition(ticket.Status, nextStatus, StaffStatusTransitions)
                ? RuleDecision.Allow()
                : RuleDecision.Conflict($"Cannot move ticket from {ticket.Status} to {nextStatus}.");
        }

        if (ticket.CreatedByUserId != actor.UserId)
        {
            return RuleDecision.Forbidden("You cannot change this ticket's status.");
        }

        return CanTransition(ticket.Status, nextStatus, RequesterStatusTransitions)
            ? RuleDecision.Allow()
            : RuleDecision.Conflict($"Cannot move ticket from {ticket.Status} to {nextStatus}.");
    }

    private static RuleDecision CanDeleteTicket(Ticket ticket, TicketActor actor)
    {
        if (actor.IsAdmin)
        {
            return RuleDecision.Allow();
        }

        if (actor.IsSupport)
        {
            return RuleDecision.Forbidden("Only admins can delete support-managed tickets.");
        }

        if (ticket.CreatedByUserId != actor.UserId)
        {
            return RuleDecision.Forbidden("You cannot delete this ticket.");
        }

        return ticket.Status == TicketStatus.Open && ticket.AssignedToUserId is null
            ? RuleDecision.Allow()
            : RuleDecision.Conflict("Requesters can delete only unassigned open tickets.");
    }

    private static bool CanAccessTicket(Ticket ticket, TicketActor actor)
    {
        return actor.IsStaff || ticket.CreatedByUserId == actor.UserId;
    }

    private static bool CanCommentOnTicket(Ticket ticket, TicketActor actor)
    {
        return actor.IsStaff || ticket.CreatedByUserId == actor.UserId;
    }

    private static bool CanTransition(
        TicketStatus currentStatus,
        TicketStatus nextStatus,
        IReadOnlyDictionary<TicketStatus, TicketStatus[]> transitions)
    {
        return transitions.TryGetValue(currentStatus, out var allowedStatuses)
            && allowedStatuses.Contains(nextStatus);
    }

    private static IReadOnlyCollection<TicketStatus> GetAvailableStatusTransitions(Ticket ticket, TicketActor actor)
    {
        if (actor.IsAdmin)
        {
            return GetTransitions(ticket.Status, StaffStatusTransitions);
        }

        if (actor.IsSupport)
        {
            return !ticket.AssignedToUserId.HasValue || ticket.AssignedToUserId.Value == actor.UserId
                ? GetTransitions(ticket.Status, StaffStatusTransitions)
                : Array.Empty<TicketStatus>();
        }

        return ticket.CreatedByUserId == actor.UserId
            ? GetTransitions(ticket.Status, RequesterStatusTransitions)
            : Array.Empty<TicketStatus>();
    }

    private static IReadOnlyCollection<TicketStatus> GetTransitions(
        TicketStatus status,
        IReadOnlyDictionary<TicketStatus, TicketStatus[]> transitions)
    {
        return transitions.TryGetValue(status, out var allowedStatuses)
            ? allowedStatuses
            : Array.Empty<TicketStatus>();
    }

    private static TicketSummaryResponse ToSummary(Ticket ticket, TicketActor actor)
    {
        return new TicketSummaryResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedBy = ToUser(ticket.CreatedByUserId, ticket.CreatedByUser),
            AssignedTo = ticket.AssignedToUserId.HasValue
                ? ToUser(ticket.AssignedToUserId.Value, ticket.AssignedToUser)
                : null,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ClosedAt = ticket.ClosedAt,
            CanEdit = CanEditTicket(ticket, actor).Allowed,
            CanAssign = CanAssignTicketWithoutAssigneeLookup(ticket, actor).Allowed,
            CanDelete = CanDeleteTicket(ticket, actor).Allowed
        };
    }

    private static TicketResponse ToDetail(Ticket ticket, TicketActor actor)
    {
        return new TicketResponse
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            CreatedBy = ToUser(ticket.CreatedByUserId, ticket.CreatedByUser),
            AssignedTo = ticket.AssignedToUserId.HasValue
                ? ToUser(ticket.AssignedToUserId.Value, ticket.AssignedToUser)
                : null,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ClosedAt = ticket.ClosedAt,
            CanEdit = CanEditTicket(ticket, actor).Allowed,
            CanAssign = CanAssignTicketWithoutAssigneeLookup(ticket, actor).Allowed,
            CanDelete = CanDeleteTicket(ticket, actor).Allowed,
            AvailableStatusTransitions = GetAvailableStatusTransitions(ticket, actor),
            Comments = ticket.Comments
                .OrderBy(comment => comment.CreatedAt)
                .ThenBy(comment => comment.Id)
                .Select(ToComment)
                .ToArray()
        };
    }

    private static TicketCommentResponse ToComment(TicketComment comment)
    {
        return new TicketCommentResponse
        {
            Id = comment.Id,
            TicketId = comment.TicketId,
            Author = ToUser(comment.UserId, comment.User),
            Text = comment.Comment,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    private static RuleDecision CanAssignTicketWithoutAssigneeLookup(Ticket ticket, TicketActor actor)
    {
        if (!actor.IsStaff)
        {
            return RuleDecision.Forbidden("Only support and admin users can assign tickets.");
        }

        if (actor.IsAdmin)
        {
            return RuleDecision.Allow();
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return RuleDecision.Conflict("Closed tickets cannot be reassigned by support users.");
        }

        return !ticket.AssignedToUserId.HasValue || ticket.AssignedToUserId.Value == actor.UserId
            ? RuleDecision.Allow()
            : RuleDecision.Forbidden("Support users cannot reassign tickets owned by another support user.");
    }

    private static TicketUserResponse ToUser(int userId, User? user)
    {
        return new TicketUserResponse
        {
            Id = userId,
            UserName = user?.UserName,
            Email = user?.Email
        };
    }

    private static bool TryNormalizeTicketFields(
        string? title,
        string? description,
        out string normalizedTitle,
        out string normalizedDescription,
        out string validationMessage)
    {
        normalizedTitle = title?.Trim() ?? string.Empty;
        normalizedDescription = description?.Trim() ?? string.Empty;

        if (normalizedTitle.Length == 0)
        {
            validationMessage = "Ticket title is required.";
            return false;
        }

        if (normalizedTitle.Length > 200)
        {
            validationMessage = "Ticket title cannot exceed 200 characters.";
            return false;
        }

        if (normalizedDescription.Length == 0)
        {
            validationMessage = "Ticket description is required.";
            return false;
        }

        validationMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeCommentText(
        string? text,
        out string normalizedText,
        out string validationMessage)
    {
        normalizedText = text?.Trim() ?? string.Empty;
        if (normalizedText.Length == 0)
        {
            validationMessage = "Comment text is required.";
            return false;
        }

        validationMessage = string.Empty;
        return true;
    }

    private static bool IsDefined<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value);
    }

    private static TicketServiceResult<T> Unauthorized<T>()
    {
        return TicketServiceResult<T>.Failure(
            TicketServiceResultStatus.Unauthorized,
            "Authenticated user id is missing or invalid.");
    }

    private static TicketServiceResult<T> Forbidden<T>(string message)
    {
        return TicketServiceResult<T>.Failure(TicketServiceResultStatus.Forbidden, message);
    }

    private static TicketServiceResult<T> NotFound<T>()
    {
        return TicketServiceResult<T>.Failure(TicketServiceResultStatus.NotFound, "Ticket was not found.");
    }

    private static TicketServiceResult<T> BadRequest<T>(string message)
    {
        return TicketServiceResult<T>.Failure(TicketServiceResultStatus.BadRequest, message);
    }

    private static TicketServiceResult<T> Failure<T>(RuleDecision decision)
    {
        return TicketServiceResult<T>.Failure(decision.Status, decision.Message);
    }

    private sealed record TicketActor(int UserId, bool IsAdmin, bool IsSupport)
    {
        public bool IsStaff => IsAdmin || IsSupport;
    }

    private sealed record RuleDecision(bool Allowed, TicketServiceResultStatus Status, string Message)
    {
        public static RuleDecision Allow()
        {
            return new RuleDecision(true, TicketServiceResultStatus.Success, string.Empty);
        }

        public static RuleDecision BadRequest(string message)
        {
            return new RuleDecision(false, TicketServiceResultStatus.BadRequest, message);
        }

        public static RuleDecision Conflict(string message)
        {
            return new RuleDecision(false, TicketServiceResultStatus.Conflict, message);
        }

        public static RuleDecision Forbidden(string message)
        {
            return new RuleDecision(false, TicketServiceResultStatus.Forbidden, message);
        }
    }
}
