using BorroDesk.Api.Authorization;
using BorroDesk.Api.Data;
using BorroDesk.Api.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BorroDesk.Api.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string DefaultPassword = "TestPassword1!";

    private readonly string databaseName = $"BorroDeskTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BorroDeskDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BorroDeskDbContext>>();
            services.AddDbContext<BorroDeskDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services
                .AddAuthentication(TestAuthHandler.AuthenticationScheme)
                .AddScheme<TestAuthOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme,
                    _ => { });
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BorroDeskDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await EnsureRolesAsync(scope.ServiceProvider);
    }

    public Task<User> CreateUserAsync(
        string? userName = null,
        string? email = null,
        string password = DefaultPassword)
    {
        return CreateUserWithRoleAsync(ApplicationRoles.User, userName, email, password);
    }

    public Task<User> CreateSupportUserAsync(
        string? userName = null,
        string? email = null,
        string password = DefaultPassword)
    {
        return CreateUserWithRoleAsync(ApplicationRoles.Support, userName, email, password);
    }

    public Task<User> CreateAdminUserAsync(
        string? userName = null,
        string? email = null,
        string password = DefaultPassword)
    {
        return CreateUserWithRoleAsync(ApplicationRoles.Admin, userName, email, password);
    }

    public async Task<User> CreateUserWithRoleAsync(
        string role,
        string? userName = null,
        string? email = null,
        string password = DefaultPassword)
    {
        using var scope = Services.CreateScope();
        await EnsureRolesAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var normalizedRole = NormalizeRole(role);
        var userNumber = Guid.NewGuid().ToString("N")[..12];
        var user = new User
        {
            UserName = userName ?? $"{normalizedRole.ToLowerInvariant()}-{userNumber}",
            Email = email ?? $"{normalizedRole.ToLowerInvariant()}-{userNumber}@borrodesk.test",
            EmailConfirmed = true,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        EnsureSucceeded(createResult, "create test user");

        var roleResult = await userManager.AddToRoleAsync(user, normalizedRole);
        EnsureSucceeded(roleResult, $"assign {normalizedRole} role");

        return user;
    }

    private static async Task EnsureRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        foreach (var role in ApplicationRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<int>(role));
            EnsureSucceeded(result, $"create {role} role");
        }
    }

    private static string NormalizeRole(string role)
    {
        var matchingRole = ApplicationRoles.All.SingleOrDefault(
            applicationRole => string.Equals(applicationRole, role, StringComparison.OrdinalIgnoreCase));

        return matchingRole
            ?? throw new ArgumentException($"Unsupported test user role '{role}'.", nameof(role));
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Could not {action}: {errors}");
    }
}
