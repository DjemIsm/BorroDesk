using System.Globalization;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.Entities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BorroDesk.Api.Tests.Infrastructure;

public sealed class AuthenticatedClientFactory(CustomWebApplicationFactory factory)
{
    public async Task<HttpClient> CreateUserClientAsync(
        string? userName = null,
        string? email = null,
        string password = CustomWebApplicationFactory.DefaultPassword)
    {
        var user = await factory.CreateUserAsync(userName, email, password);

        return CreateClientForUser(user, ApplicationRoles.User);
    }

    public async Task<HttpClient> CreateSupportClientAsync(
        string? userName = null,
        string? email = null,
        string password = CustomWebApplicationFactory.DefaultPassword)
    {
        var user = await factory.CreateSupportUserAsync(userName, email, password);

        return CreateClientForUser(user, ApplicationRoles.Support);
    }

    public async Task<HttpClient> CreateAdminClientAsync(
        string? userName = null,
        string? email = null,
        string password = CustomWebApplicationFactory.DefaultPassword)
    {
        var user = await factory.CreateAdminUserAsync(userName, email, password);

        return CreateClientForUser(user, ApplicationRoles.Admin);
    }

    public HttpClient CreateClientForUser(User user, params string[] roles)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, user.Id.ToString(CultureInfo.InvariantCulture));
        AddHeaderIfPresent(client, TestAuthHandler.UserNameHeader, user.UserName);
        AddHeaderIfPresent(client, TestAuthHandler.EmailHeader, user.Email);

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        }

        return client;
    }

    private static void AddHeaderIfPresent(HttpClient client, string headerName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            client.DefaultRequestHeaders.Add(headerName, value);
        }
    }
}
