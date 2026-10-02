using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class MarketObservationScopeTests
{
    [Fact]
    public void EmptyCollectionProducesEmptyScope()
    {
        var scope = Calculate();

        Assert.Empty(scope.ItemTypeIds);
        Assert.Empty(scope.LocationIds);
        Assert.Empty(scope.QualityLevels);
        Assert.Empty(scope.EnchantmentLevels);
        Assert.Empty(scope.AuctionTypes);
        Assert.False(scope.HasMultipleItemTypes);
        Assert.False(scope.HasMultipleLocations);
        Assert.False(scope.HasMultipleQualities);
        Assert.False(scope.HasMultipleEnchantments);
        Assert.False(scope.HasMultipleAuctionTypes);
    }

    [Fact]
    public void SingleRecordHasSingleValuedDimensions()
    {
        var scope = Calculate(Create(1, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer));

        Assert.Equal(["ITEM_A"], scope.ItemTypeIds);
        Assert.Equal(["CITY_A"], scope.LocationIds);
        Assert.Equal([4], scope.QualityLevels);
        Assert.Equal([2], scope.EnchantmentLevels);
        Assert.Equal([MarketOrderType.Offer], scope.AuctionTypes);
        Assert.False(scope.HasMultipleItemTypes);
        Assert.False(scope.HasMultipleLocations);
        Assert.False(scope.HasMultipleQualities);
        Assert.False(scope.HasMultipleEnchantments);
        Assert.False(scope.HasMultipleAuctionTypes);
    }

    [Fact]
    public void MultipleItemTypesAreDescribed()
    {
        var scope = Calculate(
            Create(1, "ITEM_A"),
            Create(2, "ITEM_B"));

        Assert.Equal(["ITEM_A", "ITEM_B"], scope.ItemTypeIds);
        Assert.True(scope.HasMultipleItemTypes);
    }

    [Fact]
    public void MultipleLocationsAreDescribed()
    {
        var scope = Calculate(
            Create(1, locationId: "CITY_A"),
            Create(2, locationId: "CITY_B"));

        Assert.Equal(["CITY_A", "CITY_B"], scope.LocationIds);
        Assert.True(scope.HasMultipleLocations);
    }

    [Fact]
    public void MultipleQualitiesAreDescribed()
    {
        var scope = Calculate(
            Create(1, quality: 1),
            Create(2, quality: 2));

        Assert.Equal([1, 2], scope.QualityLevels);
        Assert.True(scope.HasMultipleQualities);
    }

    [Fact]
    public void MultipleEnchantmentsAreDescribed()
    {
        var scope = Calculate(
            Create(1, enchantment: 0),
            Create(2, enchantment: 1));

        Assert.Equal([0, 1], scope.EnchantmentLevels);
        Assert.True(scope.HasMultipleEnchantments);
    }

    [Fact]
    public void MultipleAuctionTypesAreDescribed()
    {
        var scope = Calculate(
            Create(1, auctionType: MarketOrderType.Offer),
            Create(2, auctionType: MarketOrderType.Request));

        Assert.Equal([MarketOrderType.Offer, MarketOrderType.Request], scope.AuctionTypes);
        Assert.True(scope.HasMultipleAuctionTypes);
    }

    [Fact]
    public void SameDimensionValuesRemainSingleValued()
    {
        var scope = Calculate(
            Create(1, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer),
            Create(2, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer));

        Assert.Single(scope.ItemTypeIds);
        Assert.Single(scope.LocationIds);
        Assert.Single(scope.QualityLevels);
        Assert.Single(scope.EnchantmentLevels);
        Assert.Single(scope.AuctionTypes);
        Assert.False(scope.HasMultipleItemTypes);
        Assert.False(scope.HasMultipleLocations);
        Assert.False(scope.HasMultipleQualities);
        Assert.False(scope.HasMultipleEnchantments);
        Assert.False(scope.HasMultipleAuctionTypes);
    }

    [Fact]
    public void DuplicateOrderIdsDoNotChangeScopeByDeduplication()
    {
        var records = new[]
        {
            Create(42, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer),
            Create(42, "ITEM_B", "CITY_A", 4, 2, MarketOrderType.Offer)
        };

        var scope = Calculate(records);

        Assert.Equal(["ITEM_A", "ITEM_B"], scope.ItemTypeIds);
        Assert.True(scope.HasMultipleItemTypes);
    }

    [Fact]
    public void ScopeCalculationIsIndependentOfGroupingKey()
    {
        var records = new[]
        {
            Create(1, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer),
            Create(2, "ITEM_A", "CITY_A", 5, 2, MarketOrderType.Offer)
        };

        var scope = Calculate(records);

        Assert.Equal(["ITEM_A"], scope.ItemTypeIds);
        Assert.Equal([4, 5], scope.QualityLevels);
        Assert.True(scope.HasMultipleQualities);
    }

    [Fact]
    public void InputCollectionAndRecordsAreNotMutated()
    {
        var first = Create(1, "ITEM_A");
        var second = Create(2, "ITEM_B");
        var records = new[] { first, second };

        var before = records.ToArray();
        _ = Calculate(records);

        Assert.Equal(before, records);
        Assert.Equal(first, records[0]);
        Assert.Equal(second, records[1]);
    }

    [Fact]
    public void ItemGroupTypeIdIsNotAGroupingKeyDimensionButCanRemainUnrepresented()
    {
        var scope = Calculate(
            Create(1, itemGroupTypeId: "GROUP_A"),
            Create(2, itemGroupTypeId: "GROUP_B"));

        Assert.Single(scope.ItemTypeIds);
        Assert.False(scope.HasMultipleItemTypes);
    }

    private static MarketObservationScope Calculate(params MarketRecord[] records)
        => new MarketObservationScopeCalculator().Calculate(records);

    private static MarketRecord Create(
        ulong orderId,
        string itemTypeId = "ITEM_A",
        string locationId = "CITY_A",
        byte quality = 4,
        byte enchantment = 2,
        MarketOrderType auctionType = MarketOrderType.Offer,
        string itemGroupTypeId = "GROUP_A")
        => new(
            orderId,
            itemTypeId,
            itemGroupTypeId,
            locationId,
            quality,
            enchantment,
            100UL,
            1U,
            auctionType,
            "2030-01-01T00:00:00Z",
            0UL);
}
