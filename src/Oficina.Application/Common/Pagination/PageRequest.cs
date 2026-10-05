namespace Oficina.Application.Common.Pagination;

public sealed record PageRequest(int Page, int PageSize)
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - FirstPage) * PageSize;
}
