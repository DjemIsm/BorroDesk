namespace BorroDesk.Api.Authorization;

public static class ApplicationRoles
{
    public const string User = "User";
    public const string Support = "Support";
    public const string Admin = "Admin";

    public static readonly string[] All = [User, Support, Admin];
}
