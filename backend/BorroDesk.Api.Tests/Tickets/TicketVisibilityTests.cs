using System.Net;
using System.Net.Http.Json;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.DTOs.Tickets;
using BorroDesk.Api.Tests.Infrastructure;
using FluentAssertions;

namespace BorroDesk.Api.Tests.Tickets;

public sealed class TicketVisibilityTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task User_Should_Only_See_Own_Tickets()
    {
        var (userA, _, userATicket, userBTicket) = await TestData.CreateTicketsForTwoUsersAsync();

        using var client = ClientFactory.CreateClientForUser(userA, ApplicationRoles.User);

        var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<PagedResponse<TicketSummaryResponse>>();
        tickets.Should().NotBeNull();
        tickets!.Items.Select(ticket => ticket.Id).Should()
            .Contain(userATicket.Id)
            .And.NotContain(userBTicket.Id);
    }

    [Fact]
    public async Task Support_Should_See_All_Tickets()
    {
        var (_, _, userATicket, userBTicket) = await TestData.CreateTicketsForTwoUsersAsync();

        using var client = await ClientFactory.CreateSupportClientAsync();

        var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<PagedResponse<TicketSummaryResponse>>();
        tickets.Should().NotBeNull();
        tickets!.Items.Select(ticket => ticket.Id).Should()
            .Contain(userATicket.Id)
            .And.Contain(userBTicket.Id);
    }

    [Fact]
    public async Task Admin_Should_See_All_Tickets()
    {
        var (_, _, userATicket, userBTicket) = await TestData.CreateTicketsForTwoUsersAsync();

        using var client = await ClientFactory.CreateAdminClientAsync();

        var response = await client.GetAsync("/api/tickets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<PagedResponse<TicketSummaryResponse>>();
        tickets.Should().NotBeNull();
        tickets!.Items.Select(ticket => ticket.Id).Should()
            .Contain(userATicket.Id)
            .And.Contain(userBTicket.Id);
    }
}
