using AFMReforge.Core;
using AFMReforge.Infrastructure;
using AlbionDataAvalonia.Network.Responses;

namespace AFMReforge.Infrastructure.Tests;

public sealed class SqliteMarketObservationStoreTests
{
    [Fact]
    public void PersistsAndReadsBackAllStoredFields()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var capturedAt = DateTimeOffset.Parse("2026-10-02T12:34:56.789Z");
        store.Persist(CreateInput(MarketResponseKind.Offers, capturedAt, 42));

        var record = Assert.Single(store.ReadAll());
        Assert.True(record.StorageRecordId > 0);
        Assert.Equal("Offers", record.ResponseKind);
        Assert.Equal(nameof(AuctionGetOffersResponse), record.ResponseType);
        Assert.Equal("42", record.OrderId);
        Assert.Equal(JsonString("T4_MOCK"), record.ItemTypeId);
        Assert.Equal("1001", record.LocationId);
        Assert.Equal("1", record.QualityLevel);
        Assert.Equal("0", record.EnchantmentLevel);
        Assert.Equal("1234", record.UnitPriceSilver);
        Assert.Equal("2", record.Amount);
        Assert.Equal(JsonString("MOCK_AUCTION_TYPE"), record.AuctionType);
        Assert.Equal(JsonString("2030-01-01T00:00:00Z"), record.Expires);
        Assert.Equal("0", record.DistanceFee);
        Assert.Equal(JsonString("MOCK_LOCATION"), record.ResolvedLocation);
        Assert.Equal(capturedAt.ToString("O"), record.CapturedAt);
    }

    [Fact]
    public void StoresMultipleInputsAndAllowsRepeatedMarketOrderIds()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        store.Persist(CreateInput(MarketResponseKind.Offers, DateTimeOffset.Parse("2026-10-02T12:00:00Z"), 1));
        store.Persist(CreateInput(MarketResponseKind.Requests, DateTimeOffset.Parse("2026-10-02T12:00:01Z"), 1));
        store.Persist(CreateInput(MarketResponseKind.LoadoutOffers, DateTimeOffset.Parse("2026-10-02T12:00:02Z"), 2));

        var records = store.ReadAll();

        Assert.Equal(3, records.Count);
        Assert.Equal([1L, 2L, 3L], records.Select(x => x.StorageRecordId));
        Assert.Equal(["Offers", "Requests", "LoadoutOffers"], records.Select(x => x.ResponseKind));
        Assert.Equal(["1", "1", "2"], records.Select(x => x.OrderId));
    }

    [Fact]
    public void EmptyInputDoesNotCreateAnOrderRecord()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        store.Persist(new MarketObservationInput(
            nameof(AuctionGetOffersResponse), MarketResponseKind.Offers, null,
            DateTimeOffset.Parse("2026-10-02T12:00:00Z"), []));

        Assert.Empty(store.ReadAll());
    }

    [Fact]
    public void OneInputWithMultipleOrdersIsStoredWithinOneTransaction()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var input = new MarketObservationInput(
            nameof(AuctionGetOffersResponse), MarketResponseKind.Offers, null,
            DateTimeOffset.Parse("2026-10-02T12:00:00Z"),
            [CreateOrder(10), CreateOrder(11)]);

        store.Persist(input);

        Assert.Equal(2, store.ReadAll().Count);
    }

    private static MarketObservationInput CreateInput(
        MarketResponseKind kind, DateTimeOffset capturedAt, int id)
        => new(
            kind switch
            {
                MarketResponseKind.Offers => nameof(AuctionGetOffersResponse),
                MarketResponseKind.Requests => nameof(AuctionGetRequestsResponse),
                _ => nameof(AuctionGetLoadoutOffersResponse)
            },
            kind, null, capturedAt, [CreateOrder(id)]);

    private static MarketOrderInput CreateOrder(int id)
        => new(id, "T4_MOCK", "MOCK_GROUP", 1001, 1, 0, 1234L, 2,
            "MOCK_AUCTION_TYPE", "2030-01-01T00:00:00Z", 0, "MOCK_LOCATION");

    private static string JsonString(string value) =>
        System.Text.Json.JsonSerializer.Serialize(value);

    private sealed class TemporaryDatabase : IDisposable
    {
        private TemporaryDatabase(string path) => Path = path;
        public string Path { get; }

        public static TemporaryDatabase Create() =>
            new(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"afm-reforge-step7-{Guid.NewGuid():N}.db"));

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
