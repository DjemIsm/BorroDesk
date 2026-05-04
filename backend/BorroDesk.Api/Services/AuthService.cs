using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BorroDesk.Api.Configuration;
using BorroDesk.Api.DTOs.Auth;
using BorroDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BorroDesk.Api.Services;

public sealed class AuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var accessToken = GenerateJwt(user, roles, expiresAt);

        return new LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = roles
        };
    }

    private string GenerateJwt(User user, IEnumerable<string> roles, DateTime expiresAt)
    {
        var options = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var userId = user.Id.ToString(CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId)
        };

        if (!string.IsNullOrWhiteSpace(user.UserName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
