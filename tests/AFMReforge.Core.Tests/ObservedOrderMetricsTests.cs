using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class ObservedOrderMetricsTests
{
    [Fact]
    public void CalculatesCount()
    {
        var result = Calculate(100, 200, 300);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void CalculatesMinimum()
    {
        var result = Calculate(100, 200, 300);
        Assert.Equal(100UL, result.MinUnitPriceSilver);
    }

    [Fact]
    public void CalculatesMaximum()
    {
        var result = Calculate(100, 200, 300);
        Assert.Equal(300UL, result.MaxUnitPriceSilver);
    }

    [Fact]
    public void CalculatesExactDecimalAverage()
    {
        var result = Calculate(100, 101);
        Assert.Equal(100.5m, result.AverageUnitPriceSilver);
    }

    [Fact]
    public void CalculatesTotalAmount()
    {
        var records = new[] { Create(100, 2), Create(200, 3), Create(300, 5) };
        var result = new ObservedOrderMetricsCalculator().Calculate(records);
        Assert.Equal(10UL, result.TotalAmount);
    }

    [Fact]
    public void CalculatesTotalNotional()
    {
        var records = new[] { Create(100, 2), Create(200, 3) };
        var result = new ObservedOrderMetricsCalculator().Calculate(records);
        Assert.Equal(800m, result.TotalNotionalSilver);
    }

    [Fact]
    public void EmptyCollectionUsesNullForUndefinedStatistics()
    {
        var result = new ObservedOrderMetricsCalculator().Calculate(Array.Empty<MarketRecord>());

        Assert.Equal(0, result.Count);
        Assert.Equal(0UL, result.TotalAmount);
        Assert.Equal(0m, result.TotalNotionalSilver);
        Assert.Null(result.MinUnitPriceSilver);
        Assert.Null(result.MaxUnitPriceSilver);
        Assert.Null(result.AverageUnitPriceSilver);
    }

    [Fact]
    public void NotionalOverflowIsRejectedByCheckedPolicy()
    {
        var records = new[] { Create(ulong.MaxValue, uint.MaxValue) };

        Assert.Throws<OverflowException>(() =>
            new ObservedOrderMetricsCalculator().Calculate(records));
    }

    [Fact]
    public void AuctionTypesAreNotAutomaticallySeparatedOrMerged()
    {
        var records = new[]
        {
            Create(100, 2, MarketOrderType.Offer),
            Create(200, 3, MarketOrderType.Request)
        };

        var result = new ObservedOrderMetricsCalculator().Calculate(records);

        Assert.Equal(2, result.Count);
        Assert.Equal(150m, result.AverageUnitPriceSilver);
        Assert.Equal(5UL, result.TotalAmount);
    }

    [Fact]
    public void DuplicateOrderIdsRemainSeparateCalculationInputs()
    {
        var records = new[]
        {
            Create(100, 2, orderId: 42),
            Create(200, 3, orderId: 42)
        };

        var result = new ObservedOrderMetricsCalculator().Calculate(records);

        Assert.Equal(2, result.Count);
        Assert.Equal(800m, result.TotalNotionalSilver);
    }

    private static ObservedOrderMetrics Calculate(params ulong[] prices)
        => new ObservedOrderMetricsCalculator().Calculate(
            prices.Select((price, index) => Create(price, 1, orderId: (ulong)index + 1)).ToArray());

    private static MarketRecord Create(
        ulong unitPrice,
        uint amount,
        MarketOrderType auctionType = MarketOrderType.Unknown,
        ulong orderId = 1)
        => new(
            orderId,
            "MOCK_ITEM",
            "MOCK_GROUP",
            "MOCK_LOCATION",
            1,
            0,
            unitPrice,
            amount,
            auctionType,
            "2030-01-01T00:00:00Z",
            0UL);
}
