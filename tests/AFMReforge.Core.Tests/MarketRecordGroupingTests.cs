using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class MarketRecordGroupingTests
{
    [Fact]
    public void SameGroupingDimensionsProduceOneGroup()
    {
        var records = new[]
        {
            Create(1, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer),
            Create(2, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer)
        };

        var groups = MarketRecordGrouping.GroupByKey(records);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.Value.Count);
    }

    [Fact]
    public void DifferentItemProducesDifferentGroup()
    {
        var records = new[]
        {
            Create(1, "ITEM_A"),
            Create(2, "ITEM_B")
        };

        Assert.Equal(2, MarketRecordGrouping.GroupByKey(records).Count);
    }

    [Fact]
    public void DifferentLocationProducesDifferentGroup()
    {
        var records = new[]
        {
            Create(1, locationId: "CITY_A"),
            Create(2, locationId: "CITY_B")
        };

        Assert.Equal(2, MarketRecordGrouping.GroupByKey(records).Count);
    }

    [Fact]
    public void DifferentQualityProducesDifferentGroup()
    {
        var records = new[]
        {
            Create(1, quality: 4),
            Create(2, quality: 5)
        };

        Assert.Equal(2, MarketRecordGrouping.GroupByKey(records).Count);
    }

    [Fact]
    public void DifferentEnchantmentProducesDifferentGroup()
    {
        var records = new[]
        {
            Create(1, enchantment: 2),
            Create(2, enchantment: 3)
        };

        Assert.Equal(2, MarketRecordGrouping.GroupByKey(records).Count);
    }

    [Fact]
    public void DifferentAuctionTypeProducesDifferentGroup()
    {
        var records = new[]
        {
            Create(1, auctionType: MarketOrderType.Offer),
            Create(2, auctionType: MarketOrderType.Request)
        };

        Assert.Equal(2, MarketRecordGrouping.GroupByKey(records).Count);
    }

    [Fact]
    public void DuplicateOrderIdsAreNotDeduplicated()
    {
        var records = new[]
        {
            Create(42, price: 100, amount: 2),
            Create(42, price: 200, amount: 3)
        };

        var group = Assert.Single(MarketRecordGrouping.GroupByKey(records)).Value;

        Assert.Equal(2, group.Count);
        Assert.Equal(2, group.Select(x => x.OrderId).Count());
        Assert.Equal(800m, new ObservedOrderMetricsCalculator().Calculate(group).TotalNotionalSilver);
    }

    [Fact]
    public void StorageRecordIdentityDoesNotParticipateInGrouping()
    {
        var records = new[]
        {
            Create(1, price: 100),
            Create(2, price: 200)
        };

        var groups = MarketRecordGrouping.GroupByKey(records);

        Assert.Single(groups);
        Assert.Equal(2, groups.Single().Value.Count);
    }

    [Fact]
    public void ObservationScopeIsNotAnImplicitGroupingDimension()
    {
        var firstObservationRecords = new[] { Create(1, price: 100) };
        var secondObservationRecords = new[] { Create(2, price: 200) };

        var combined = firstObservationRecords.Concat(secondObservationRecords).ToArray();
        var groups = MarketRecordGrouping.GroupByKey(combined);

        Assert.Single(groups);
        Assert.Equal(2, groups.Single().Value.Count);
    }

    [Fact]
    public void EmptyCollectionProducesNoGroups()
    {
        var groups = MarketRecordGrouping.GroupByKey(Array.Empty<MarketRecord>());

        Assert.Empty(groups);
    }

    [Fact]
    public void GroupingPreservesEveryInputRecord()
    {
        var records = new[]
        {
            Create(1, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer),
            Create(2, "ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer),
            Create(3, "ITEM_A", "CITY_B", 4, 2, MarketOrderType.Offer),
            Create(4, "ITEM_B", "CITY_A", 4, 2, MarketOrderType.Request)
        };

        var groups = MarketRecordGrouping.GroupByKey(records);

        Assert.Equal(records.Length, groups.Sum(group => group.Value.Count));
        Assert.Equal(
            records.Select(x => x.OrderId).OrderBy(x => x),
            groups.SelectMany(group => group.Value).Select(x => x.OrderId).OrderBy(x => x));
    }

    [Fact]
    public void SameKeyUsesValueEquality()
    {
        var first = new MarketGroupingKey("ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer);
        var second = new MarketGroupingKey("ITEM_A", "CITY_A", 4, 2, MarketOrderType.Offer);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ItemGroupTypeIdDoesNotChangeCandidateKey()
    {
        var first = Create(1, itemGroupTypeId: "GROUP_A");
        var second = Create(2, itemGroupTypeId: "GROUP_B");

        var groups = MarketRecordGrouping.GroupByKey([first, second]);

        Assert.Single(groups);
        Assert.Equal(2, groups.Single().Value.Count);
    }

    private static MarketRecord Create(
        ulong orderId,
        string itemTypeId = "ITEM_A",
        string locationId = "CITY_A",
        byte quality = 4,
        byte enchantment = 2,
        MarketOrderType auctionType = MarketOrderType.Offer,
        ulong price = 100,
        uint amount = 1,
        string itemGroupTypeId = "GROUP_A")
        => new(
            orderId,
            itemTypeId,
            itemGroupTypeId,
            locationId,
            quality,
            enchantment,
            price,
            amount,
            auctionType,
            "2030-01-01T00:00:00Z",
            0UL);
}
