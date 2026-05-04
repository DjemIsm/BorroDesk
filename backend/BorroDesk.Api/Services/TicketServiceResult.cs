namespace BorroDesk.Api.Services;

public enum TicketServiceResultStatus
{
    Success,
    Unauthorized,
    Forbidden,
    NotFound,
    BadRequest,
    Conflict
}

public sealed record TicketServiceResult(TicketServiceResultStatus Status, string? Message = null)
{
    public static TicketServiceResult Success()
    {
        return new TicketServiceResult(TicketServiceResultStatus.Success);
    }

    public static TicketServiceResult Failure(TicketServiceResultStatus status, string message)
    {
        return new TicketServiceResult(status, message);
    }
}

public sealed record TicketServiceResult<T>(TicketServiceResultStatus Status, T? Value = default, string? Message = null)
{
    public static TicketServiceResult<T> Success(T value)
    {
        return new TicketServiceResult<T>(TicketServiceResultStatus.Success, value);
    }

    public static TicketServiceResult<T> Failure(TicketServiceResultStatus status, string message)
    {
        return new TicketServiceResult<T>(status, default, message);
    }
}
