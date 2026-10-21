namespace besti2.Application.Businesses;

// One page of the business list. The search is filtered and sorted first (also by distance and
// free times), and only then cut into pages, so no close or early business is ever skipped
public record BusinessPageDto(IReadOnlyList<BusinessListItemDto> Items, int Page, int PageCount, int TotalCount)
{
    public const int PageSize = 24;

    public static BusinessPageDto From(IReadOnlyList<BusinessListItemDto> all, int? page)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
        // Page 0, -3 or 999 in the address: show the nearest real page instead of an error
        var current = Math.Clamp(page ?? 1, 1, pageCount);
        var items = all.Skip((current - 1) * PageSize).Take(PageSize).ToList();
        return new BusinessPageDto(items, current, pageCount, all.Count);
    }
}
