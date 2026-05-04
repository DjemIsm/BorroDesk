namespace BorroDesk.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "BorroDesk.Api";

    public string Audience { get; set; } = "BorroDesk.Client";

    public string? SigningKey { get; set; }

    public int ExpirationMinutes { get; set; } = 60;
}
