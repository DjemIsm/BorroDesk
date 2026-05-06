using System.Net;
using System.Net.Http.Json;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.DTOs.Tickets;
using BorroDesk.Api.Tests.Infrastructure;
using FluentAssertions;

namespace BorroDesk.Api.Tests.Tickets;

public sealed class TicketCommentPermissionTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task User_Should_Not_Comment_On_Other_Users_Ticket()
    {
        var userA = await Factory.CreateUserAsync();
        var userB = await Factory.CreateUserAsync();
        var ticket = await TestData.CreateTicketAsync(
            userA,
            "User A private ticket",
            "User B must not be able to comment on this ticket.");

        using var client = ClientFactory.CreateClientForUser(userB, ApplicationRoles.User);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticket.Id}/comments",
            new CreateTicketCommentRequest { Text = "This comment should not be saved." });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        var commentCount = await TestData.CountCommentsAsync(ticket.Id);
        commentCount.Should().Be(0);
    }
}
