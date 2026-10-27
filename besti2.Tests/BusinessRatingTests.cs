using besti2.Domain.Entities;
using besti2.Infrastructure.Persistence;
using besti2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace besti2.Tests;

// "Best vurdert" and "4★ og over" in the search
public class BusinessRatingTests
{
    private static readonly Guid KlippOgStil = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FjordBygg = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Nordlys = new("33333333-3333-3333-3333-333333333333");

    private static async Task<(BusinessSearchService Service, AppDbContext Db)> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return (new BusinessSearchService(db, new FakeImageStorage()), db);
    }

    private static void AddReviews(AppDbContext db, Guid businessId, params int[] stars)
    {
        foreach (var star in stars)
        {
            db.Reviews.Add(new Review
            {
                Id = Guid.NewGuid(), BookingId = Guid.NewGuid(), BusinessId = businessId, CustomerUserId = "kunde",
                CustomerName = "Kari", Stars = star, CreatedUtc = DateTime.UtcNow
            });
        }
    }

    // check if one 5★ review scores lower than many good reviews
    [Fact]
    public void RatingScore_OneFiveStarReview_LosesToManyGoodReviews()
    {
        var oneReview = BusinessSearchService.RatingScore(5.0, 1);
        var manyReviews = BusinessSearchService.RatingScore(4.6, 20);

        Assert.True(manyReviews > oneReview);
        Assert.Equal(0.0, BusinessSearchService.RatingScore(null, 0));
    }

    // check if "Best vurdert" puts the most trusted rating first and businesses without reviews last
    [Fact]
    public async Task GetAllAsync_SortByRating_UsesBayesianAverage()
    {
        var (service, db) = await CreateAsync();
        AddReviews(db, FjordBygg, 5);          // 5,0 but only one review
        AddReviews(db, KlippOgStil, 5, 4, 5);  // 4,7 from three reviews
        await db.SaveChangesAsync();

        var result = await service.GetAllAsync(sort: "rating");

        Assert.Equal(new[] { KlippOgStil, FjordBygg, Nordlys }, result.Select(b => b.Id));
    }

    // check if "4★ og over" leaves out low ratings, but keeps new businesses with under 3 reviews
    [Fact]
    public async Task GetAllAsync_MinRating4_LeavesOutLowRatingsOnly()
    {
        var (service, db) = await CreateAsync();
        AddReviews(db, FjordBygg, 3, 3, 3);
        AddReviews(db, KlippOgStil, 4, 4, 4);
        AddReviews(db, Nordlys, 1);
        await db.SaveChangesAsync();

        var result = await service.GetAllAsync(minRating: 4);

        Assert.Equal(new[] { KlippOgStil, Nordlys }, result.Select(b => b.Id));
    }

    // check if a business with under 3 reviews shows no average yet ("Ny på Besti"), only the count
    [Fact]
    public async Task GetAllAsync_UnderThreeReviews_HidesAverage()
    {
        var (service, db) = await CreateAsync();
        AddReviews(db, FjordBygg, 5, 1);
        AddReviews(db, KlippOgStil, 5, 5, 1);
        await db.SaveChangesAsync();

        var result = await service.GetAllAsync();

        var fjord = result.Single(b => b.Id == FjordBygg);
        Assert.Null(fjord.AverageRating);
        Assert.Equal(2, fjord.ReviewCount);
        Assert.Equal(3.7, result.Single(b => b.Id == KlippOgStil).AverageRating);
    }

    // check if the list shows the average stars and the number of reviews for each business
    [Fact]
    public async Task GetAllAsync_ShowsAverageRatingAndReviewCount()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var klippId = new Guid("11111111-1111-1111-1111-111111111111");
        foreach (var stars in new[] { 5, 4, 5 })
        {
            db.Reviews.Add(new Review
            {
                Id = Guid.NewGuid(), BookingId = Guid.NewGuid(), BusinessId = klippId, CustomerUserId = "kunde",
                CustomerName = "Kari", Stars = stars, CreatedUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();
        var service = new BusinessSearchService(db, new FakeImageStorage());

        var result = await service.GetAllAsync();

        var klipp = result.Single(b => b.Id == klippId);
        Assert.Equal(4.7, klipp.AverageRating);
        Assert.Equal(3, klipp.ReviewCount);
        var fjord = result.Single(b => b.Name == "Fjord Bygg AS");
        Assert.Null(fjord.AverageRating);
        Assert.Equal(0, fjord.ReviewCount);
    }
}
