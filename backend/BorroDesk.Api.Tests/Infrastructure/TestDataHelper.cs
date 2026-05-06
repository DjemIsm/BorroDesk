using BorroDesk.Api.Data;
using BorroDesk.Api.Entities;
using BorroDesk.Api.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BorroDesk.Api.Tests.Infrastructure;

public sealed class TestDataHelper(CustomWebApplicationFactory factory)
{
    public async Task<Ticket> CreateTicketAsync(
        User owner,
        string? title = null,
        string? description = null,
        TicketPriority priority = TicketPriority.Normal,
        TicketStatus status = TicketStatus.Open,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        var ticket = new Ticket
        {
            Title = title ?? $"Test ticket {Guid.NewGuid():N}",
            Description = description ?? "Test ticket description.",
            CreatedByUserId = owner.Id,
            Priority = priority,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = updatedAt
        };

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BorroDeskDbContext>();

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        return ticket;
    }

    public async Task<TicketsForTwoUsers> CreateTicketsForTwoUsersAsync()
    {
        var userA = await factory.CreateUserAsync();
        var userB = await factory.CreateUserAsync();
        var userATicket = await CreateTicketAsync(
            userA,
            "User A ticket",
            "Visible only to User A.");
        var userBTicket = await CreateTicketAsync(
            userB,
            "User B ticket",
            "Visible only to User B.");

        return new TicketsForTwoUsers(userA, userB, userATicket, userBTicket);
    }

    public async Task<int> CountCommentsAsync(int ticketId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BorroDeskDbContext>();

        return await dbContext.TicketComments.CountAsync(comment => comment.TicketId == ticketId);
    }

    public async Task<int> CountAttachmentsAsync(int ticketId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BorroDeskDbContext>();

        return await dbContext.TicketAttachments.CountAsync(attachment => attachment.TicketId == ticketId);
    }

    public async Task<User?> FindUserByEmailAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        return await userManager.FindByEmailAsync(email);
    }

    public async Task<User?> FindUserByIdAsync(int userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        return await userManager.FindByIdAsync(userId.ToString());
    }

    public async Task<IList<string>> GetRolesAsync(User user)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        return await userManager.GetRolesAsync(user);
    }
}

public sealed record TicketsForTwoUsers(User UserA, User UserB, Ticket UserATicket, Ticket UserBTicket);
