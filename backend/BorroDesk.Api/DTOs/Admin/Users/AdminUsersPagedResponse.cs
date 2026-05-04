namespace BorroDesk.Api.DTOs.Admin.Users;

public sealed class AdminUsersPagedResponse
{
    public required IReadOnlyCollection<AdminUserResponse> Items { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
