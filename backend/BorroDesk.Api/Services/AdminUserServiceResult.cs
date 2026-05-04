namespace BorroDesk.Api.Services;

public enum AdminUserServiceResultStatus
{
    Success,
    Unauthorized,
    Forbidden,
    NotFound,
    BadRequest,
    Conflict
}

public sealed record AdminUserServiceResult(AdminUserServiceResultStatus Status, string? Message = null)
{
    public static AdminUserServiceResult Success()
    {
        return new AdminUserServiceResult(AdminUserServiceResultStatus.Success);
    }

    public static AdminUserServiceResult Failure(AdminUserServiceResultStatus status, string message)
    {
        return new AdminUserServiceResult(status, message);
    }
}

public sealed record AdminUserServiceResult<T>(
    AdminUserServiceResultStatus Status,
    T? Value = default,
    string? Message = null)
{
    public static AdminUserServiceResult<T> Success(T value)
    {
        return new AdminUserServiceResult<T>(AdminUserServiceResultStatus.Success, value);
    }

    public static AdminUserServiceResult<T> Failure(AdminUserServiceResultStatus status, string message)
    {
        return new AdminUserServiceResult<T>(status, default, message);
    }
}
