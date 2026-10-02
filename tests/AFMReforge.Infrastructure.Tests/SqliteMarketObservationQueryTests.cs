using AFMReforge.Core;
using AFMReforge.Infrastructure;

namespace AFMReforge.Infrastructure.Tests;

public sealed class SqliteMarketObservationQueryTests
{
    [Fact]
    public void RecentReturnsNewestStorageRecordsFirst()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var results = store.Query(new MarketRecordQuery(Limit: 2));

        Assert.Equal(2, results.Count);
        Assert.Equal([3L, 2L], results.Select(x => x.StorageRecordId));
    }

    [Fact]
    public void FiltersByItemTypeId()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var results = store.Query(new MarketRecordQuery(ItemTypeId: "ITEM_B"));

        Assert.Equal(2, results.Count);
        Assert.All(results, x => Assert.Equal("ITEM_B", x.ItemTypeId));
    }

    [Fact]
    public void FiltersByLocationId()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var results = store.Query(new MarketRecordQuery(LocationId: "2002"));

        Assert.Single(results);
        Assert.Equal("ITEM_B", results[0].ItemTypeId);
    }

    [Fact]
    public void AppliesCombinedFiltersInSqlite()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var results = store.Query(new MarketRecordQuery(
            ItemTypeId: "ITEM_B",
            LocationId: "2002",
            QualityLevel: "5",
            EnchantmentLevel: "4",
            ResponseKind: MarketResponseKind.Offers));

        Assert.Single(results);
        Assert.Equal("ITEM_B", results[0].ItemTypeId);
        Assert.Equal("2002", results[0].LocationId);
        Assert.Equal("5", results[0].QualityLevel);
        Assert.Equal("4", results[0].EnchantmentLevel);
        Assert.Equal(MarketResponseKind.Offers, results[0].ResponseKind);
    }

    [Fact]
    public void FiltersByResponseKind()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var results = store.Query(new MarketRecordQuery(ResponseKind: MarketResponseKind.Requests));

        Assert.Single(results);
        Assert.Equal(MarketResponseKind.Requests, results[0].ResponseKind);
    }

    [Fact]
    public void FiltersByCapturedAtRangeWithoutDeclaringItObservationTime()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var from = DateTimeOffset.Parse("2026-10-02T12:00:01Z");
        var to = DateTimeOffset.Parse("2026-10-02T12:00:02Z");

        var results = store.Query(new MarketRecordQuery(From: from, To: to));

        Assert.Equal(2, results.Count);
        Assert.Equal([3L, 2L], results.Select(x => x.StorageRecordId));
    }

    [Fact]
    public void SupportsLimitAndOffset()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        var results = store.Query(new MarketRecordQuery(Limit: 1, Offset: 1));

        Assert.Single(results);
        Assert.Equal(2L, results[0].StorageRecordId);
    }

    [Fact]
    public void DoesNotDeduplicateRepeatedOrderIds()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        store.Persist(CreateInput(1, "ITEM_A", "1001", MarketResponseKind.Offers, "2026-10-02T12:00:00Z"));
        store.Persist(CreateInput(1, "ITEM_A", "1001", MarketResponseKind.Offers, "2026-10-02T12:00:01Z"));

        var results = store.Query(new MarketRecordQuery(ItemTypeId: "ITEM_A"));

        Assert.Equal(2, results.Count);
        Assert.All(results, x => Assert.Equal("1", x.OrderId));
    }

    [Fact]
    public void RejectsInvalidPaginationAndRange()
    {
        using var database = TemporaryDatabase.Create();
        var store = Seed(database.Path);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            store.Query(new MarketRecordQuery(Limit: 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            store.Query(new MarketRecordQuery(Offset: -1)));
        Assert.Throws<ArgumentException>(() =>
            store.Query(new MarketRecordQuery(
                From: DateTimeOffset.Parse("2026-10-03T00:00:00Z"),
                To: DateTimeOffset.Parse("2026-10-02T00:00:00Z"))));
    }

    private static SqliteMarketObservationStore Seed(string path)
    {
        var store = new SqliteMarketObservationStore(path);
        store.Persist(CreateInput(1, "ITEM_A", "1001", MarketResponseKind.Offers, "2026-10-02T12:00:00Z"));
        store.Persist(CreateInput(2, "ITEM_B", "2002", MarketResponseKind.Requests, "2026-10-02T12:00:01Z",
            quality: "5", enchantment: "4"));
        store.Persist(CreateInput(3, "ITEM_B", "2003", MarketResponseKind.Offers, "2026-10-02T12:00:02Z"));
        return store;
    }

    private static MarketObservationInput CreateInput(
        int id,
        string item,
        string location,
        MarketResponseKind kind,
        string capturedAt,
        string quality = "1",
        string enchantment = "0")
        => new(
            kind switch
            {
                MarketResponseKind.Offers => "AuctionGetOffersResponse",
                MarketResponseKind.Requests => "AuctionGetRequestsResponse",
                _ => "AuctionGetLoadoutOffersResponse"
            },
            kind,
            null,
            DateTimeOffset.Parse(capturedAt),
            [new MarketOrderInput(id, item, "GROUP", location, quality, enchantment, "1234", "2",
                "MOCK_AUCTION_TYPE", "2030-01-01T00:00:00Z", "0", "MOCK_LOCATION")]);

    private sealed class TemporaryDatabase : IDisposable
    {
        private TemporaryDatabase(string path) => Path = path;
        public string Path { get; }

        public static TemporaryDatabase Create() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"afm-reforge-step8-{Guid.NewGuid():N}.db"));

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
