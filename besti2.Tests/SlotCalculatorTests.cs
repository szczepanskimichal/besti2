using besti2.Application.Bookings;

namespace besti2.Tests;

public class SlotCalculatorTests
{
    // Poniedziałek 12.10.2026. Czas letni w Oslo, więc UTC+2: 09:00 w Oslo = 07:00 UTC
    private static readonly DateOnly Monday = new(2026, 10, 12);
    private static readonly DateTime LongAgo = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EmptyDay_ReturnsSlotsFromOpeningUntilServiceStillFits()
    {
        var slots = SlotCalculator.GetFreeSlots(Monday, 60, [], LongAgo);

        Assert.Equal(15, slots.Count);
        Assert.Equal(new DateTime(2026, 10, 12, 7, 0, 0, DateTimeKind.Utc), slots[0]);
        Assert.Equal(new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc), slots[^1]);
    }

    [Fact]
    public void ExistingBooking_BlocksOverlappingSlots()
    {
        // Zajęte 10:00–11:00 w Oslo = 08:00–09:00 UTC
        var busy = new List<(DateTime, DateTime)>
        {
            (new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc))
        };

        var slots = SlotCalculator.GetFreeSlots(Monday, 60, busy, LongAgo);

        Assert.Contains(new DateTime(2026, 10, 12, 7, 0, 0, DateTimeKind.Utc), slots);    // 09:00 kończy się o 10:00, więc OK
        Assert.DoesNotContain(new DateTime(2026, 10, 12, 7, 30, 0, DateTimeKind.Utc), slots); // 09:30–10:30 koliduje
        Assert.DoesNotContain(new DateTime(2026, 10, 12, 8, 0, 0, DateTimeKind.Utc), slots);
        Assert.DoesNotContain(new DateTime(2026, 10, 12, 8, 30, 0, DateTimeKind.Utc), slots);
        Assert.Contains(new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc), slots);    // 11:00 znowu wolne
    }

    [Fact]
    public void Weekend_ReturnsNoSlots()
    {
        var saturday = new DateOnly(2026, 10, 10);

        var slots = SlotCalculator.GetFreeSlots(saturday, 60, [], LongAgo);

        Assert.Empty(slots);
    }

    [Fact]
    public void PastTimes_AreNotOffered()
    {
        // „Teraz” = 10:15 w Oslo = 08:15 UTC
        var now = new DateTime(2026, 10, 12, 8, 15, 0, DateTimeKind.Utc);

        var slots = SlotCalculator.GetFreeSlots(Monday, 30, [], now);

        Assert.Equal(new DateTime(2026, 10, 12, 8, 30, 0, DateTimeKind.Utc), slots[0]); // pierwszy wolny: 10:30
    }
    
    [Fact]
    public void WinterTime_OpeningIsOneHourLaterInUtc()
    {
        var winterMonday = new DateOnly(2026, 11, 2); // czas zimowy w Oslo, UTC+1

        var slots = SlotCalculator.GetFreeSlots(winterMonday, 30, [], LongAgo);

        Assert.Equal(new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc), slots[0]); // 09:00 w Oslo
    }
}