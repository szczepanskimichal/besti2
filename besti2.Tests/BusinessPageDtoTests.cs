using besti2.Application.Businesses;

namespace besti2.Tests;

public class BusinessPageDtoTests
{
    private static List<BusinessListItemDto> Businesses(int count) =>
        Enumerable.Range(1, count)
            .Select(n => new BusinessListItemDto(Guid.NewGuid(), $"Bedrift {n}", "Frisor", null, "Oslo", null, null))
            .ToList();

    // check if 50 businesses become 3 pages: 24 + 24 + 2, in the same order
    [Fact]
    public void From_CutsTheListIntoPagesOf24()
    {
        var all = Businesses(50);

        var third = BusinessPageDto.From(all, 3);

        Assert.Equal(3, third.PageCount);
        Assert.Equal(50, third.TotalCount);
        Assert.Equal(["Bedrift 49", "Bedrift 50"], third.Items.Select(b => b.Name));
    }

    // check if a page number that does not exist shows the nearest real page
    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(999, 3)]
    public void From_UnknownPage_ShowsNearestPage(int? page, int expected)
    {
        Assert.Equal(expected, BusinessPageDto.From(Businesses(50), page).Page);
    }

    // check if an empty search still gives one (empty) page
    [Fact]
    public void From_NoBusinesses_OneEmptyPage()
    {
        var result = BusinessPageDto.From([], null);

        Assert.Equal(1, result.PageCount);
        Assert.Empty(result.Items);
    }
}