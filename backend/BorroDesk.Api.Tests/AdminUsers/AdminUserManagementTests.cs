using System.Net;
using System.Net.Http.Json;
using BorroDesk.Api.Authorization;
using BorroDesk.Api.DTOs.Admin.Users;
using BorroDesk.Api.Tests.Infrastructure;
using FluentAssertions;

namespace BorroDesk.Api.Tests.AdminUsers;

public sealed class AdminUserManagementTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Admin_Should_Create_User()
    {
        using var client = await ClientFactory.CreateAdminClientAsync();
        var request = new AdminCreateUserRequest
        {
            UserName = "created-support-user",
            Email = "created-support-user@borrodesk.test",
            Password = "CreatedPassword1!",
            Roles = [ApplicationRoles.Support]
        };

        var response = await client.PostAsJsonAsync("/api/admin/users", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdUser = await TestData.FindUserByEmailAsync(request.Email);
        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be(request.UserName);

        var roles = await TestData.GetRolesAsync(createdUser);
        roles.Should().ContainSingle().Which.Should().Be(ApplicationRoles.Support);
    }

    [Fact]
    public async Task Admin_Should_Not_Deactivate_Self()
    {
        var admin = await Factory.CreateAdminUserAsync();
        using var client = ClientFactory.CreateClientForUser(admin, ApplicationRoles.Admin);

        var response = await client.DeleteAsync($"/api/admin/users/{admin.Id}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Conflict);

        var storedAdmin = await TestData.FindUserByIdAsync(admin.Id);
        storedAdmin.Should().NotBeNull();
        storedAdmin!.IsActive.Should().BeTrue();
    }
}
