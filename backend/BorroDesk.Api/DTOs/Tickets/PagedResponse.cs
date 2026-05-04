namespace BorroDesk.Api.DTOs.Tickets;

public sealed class PagedResponse<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
