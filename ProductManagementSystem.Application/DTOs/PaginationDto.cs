namespace ProductManagementSystem.Application.DTOs;

public record PaginationRequest(
    int PageNumber = 1,
    int PageSize = 10
)
{
    public int GetPageNumber() => PageNumber < 1 ? 1 : PageNumber;
    public int GetPageSize() => PageSize < 1 || PageSize > 100 ? 10 : PageSize;
}

public record PagedResponse<T>(
    IEnumerable<T> Data,
    int PageNumber,
    int PageSize,
    int TotalRecords,
    int TotalPages
)
{
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResponse<T> Create(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalRecords)
    {
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        return new PagedResponse<T>(data, pageNumber, pageSize, totalRecords, totalPages);
    }
}
