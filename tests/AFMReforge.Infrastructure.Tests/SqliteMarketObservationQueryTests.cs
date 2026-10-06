using AFMReforge.Core;
using AFMReforge.Infrastructure;

namespace AFMReforge.Infrastructure.Tests;

public sealed class SqliteMarketObservationQueryTests
{
    [Fact]
    public void FiltersByItemType()
    {
        using var db = TemporaryDatabase.Create();
        var store = Seed(db.Path);

        var rows = store.Query(new MarketRecordQuery(ItemTypeId: "ITEM_B"));

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal("ITEM_B", row.ItemTypeId));
    }

    [Fact]
    public void FiltersByLocation()
    {
        using var db = TemporaryDatabase.Create();
        var store = Seed(db.Path);

        var rows = store.Query(new MarketRecordQuery(LocationId: "2002"));

        Assert.Single(rows);
        Assert.Equal("ITEM_B", rows[0].ItemTypeId);
    }

    [Fact]
    public void FiltersByQualityAndEnchantment()
    {
        using var db = TemporaryDatabase.Create();
        var store = Seed(db.Path);

        var rows = store.Query(new MarketRecordQuery(QualityLevel: 5, EnchantmentLevel: 4));

        Assert.Single(rows);
        Assert.Equal("ITEM_B", rows[0].ItemTypeId);
    }

    [Fact]
    public void FiltersByResponseKind()
    {
        using var db = TemporaryDatabase.Create();
        var store = Seed(db.Path);

        var rows = store.Query(new MarketRecordQuery(ResponseKind: MarketResponseKind.Requests));

        Assert.Single(rows);
        Assert.Equal(MarketResponseKind.Requests, rows[0].ResponseKind);
    }

    [Fact]
    public void FiltersByObservationId()
    {
        using var db = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(db.Path);
        var observation = new MarketObservationFactory().Create(CreateInput(1, "ITEM_A", "1001", MarketResponseKind.Offers, "2026-10-02T12:00:00Z"));
        store.Persist(observation);

        var rows = store.Query(new MarketRecordQuery(ObservationId: observation.ObservationId));

        Assert.Single(rows);
        Assert.Equal(observation.ObservationId, rows[0].ObservationId);
    }

    [Fact]
    public void FiltersByCapturedAtRange()
    {
        using var db = TemporaryDatabase.Create();
        var store = Seed(db.Path);

        var rows = store.Query(new MarketRecordQuery(
            From: DateTimeOffset.Parse("2026-10-02T12:00:01Z"),
            To: DateTimeOffset.Parse("2026-10-02T12:00:02Z")));

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void RejectsInvalidDateRange()
    {
        using var db = TemporaryDatabase.Create();
        var store = Seed(db.Path);

        Assert.Throws<ArgumentException>(() => store.Query(new MarketRecordQuery(
            From: DateTimeOffset.Parse("2026-10-03T00:00:00Z"),
            To: DateTimeOffset.Parse("2026-10-02T00:00:00Z"))));
    }

    private static SqliteMarketObservationStore Seed(string path)
    {
        var store = new SqliteMarketObservationStore(path);
        store.Persist(CreateInput(1, "ITEM_A", "1001", MarketResponseKind.Offers, "2026-10-02T12:00:00Z"));
        store.Persist(CreateInput(2, "ITEM_B", "2002", MarketResponseKind.Requests, "2026-10-02T12:00:01Z", quality: 5, enchantment: 4));
        store.Persist(CreateInput(3, "ITEM_B", "2003", MarketResponseKind.Offers, "2026-10-02T12:00:02Z"));
        return store;
    }

    private static MarketObservationInput CreateInput(
        int id,
        string item,
        string location,
        MarketResponseKind kind,
        string capturedAt,
        byte quality = 1,
        byte enchantment = 0)
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
            [new MarketRecord((ulong)id, item, "GROUP", location, quality, enchantment, 1234, 2,
                MarketOrderType.Unknown, "2030-01-01T00:00:00Z", 0)]);

    private sealed class TemporaryDatabase : IDisposable
    {
        private TemporaryDatabase(string path) => Path = path;
        public string Path { get; }

        public static TemporaryDatabase Create() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"afm-reforge-query-{Guid.NewGuid():N}.db"));

        public void Dispose()
        {
            try { File.Delete(Path); } catch { }
        }
    }
}
