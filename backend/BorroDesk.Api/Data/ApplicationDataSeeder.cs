using BorroDesk.Api.Authorization;
using BorroDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace BorroDesk.Api.Data;

public static class ApplicationDataSeeder
{
    public static async Task SeedDevelopmentAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ApplicationDataSeeder));
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(role));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed role '{role}': {FormatErrors(roleResult)}");
                }
            }
        }

        var adminSection = configuration.GetSection("SeedUsers:Admin");
        if (!adminSection.GetValue("Enabled", false))
        {
            return;
        }

        var email = adminSection["Email"];
        var password = adminSection["Password"];
        var userName = adminSection["UserName"] ?? email;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException("SeedUsers:Admin must define Email, UserName, and Password.");
        }

        var adminUser = await userManager.FindByEmailAsync(email);
        if (adminUser is null)
        {
            adminUser = new User
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(adminUser, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed test admin user: {FormatErrors(createResult)}");
            }

            logger.LogInformation("Seeded test admin user {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to assign admin role: {FormatErrors(addRoleResult)}");
            }
        }
    }

    private static string FormatErrors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}
