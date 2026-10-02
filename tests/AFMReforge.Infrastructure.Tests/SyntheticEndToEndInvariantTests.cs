using AFMReforge.Adapter.AFM;
using AFMReforge.Core;
using AlbionDataAvalonia.Network.Responses;
using Microsoft.Data.Sqlite;

namespace AFMReforge.Infrastructure.Tests;

public sealed class SyntheticEndToEndInvariantTests
{
    [Fact]
    public void ProcessesSyntheticResponsesThroughAdapterProcessingPersistenceAndQuery()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var adapter = new AfmMarketAdapter();
        var processing = new MarketProcessing(store: store);
        var observations = new List<MarketObservation>();

        adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));

        adapter.ProcessMockResponse(CreateOffersResponse(
            (100, "MOCK_T6_SWORD"),
            (100, "MOCK_T6_SWORD"),
            (101, "MOCK_T6_AXE")));

        Assert.Single(observations);
        var observation = observations.Single();

        var records = store.Query(new MarketRecordQuery(
            ObservationId: observation.ObservationId,
            ItemTypeId: "MOCK_T6_SWORD",
            QualityLevel: 1,
            Limit: 10));

        Assert.Equal(2, records.Count);
        Assert.All(records, record => Assert.Equal(observation.ObservationId, record.ObservationId));
        Assert.All(records, record => Assert.Equal("MOCK_T6_SWORD", record.ItemTypeId));
        Assert.Equal(["100", "100"], records.Select(record => record.OrderId));
        Assert.Equal(2, records.Select(record => record.StorageRecordId).Distinct().Count());
    }

    [Fact]
    public void PreservesSameOrderIdAcrossDifferentObservations()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var adapter = new AfmMarketAdapter();
        var processing = new MarketProcessing(store: store);
        var observations = new List<MarketObservation>();

        adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));

        adapter.ProcessMockResponse(CreateOffersResponse((100, "MOCK_T6_SWORD")));
        adapter.ProcessMockResponse(CreateOffersResponse((100, "MOCK_T6_SWORD")));

        Assert.Equal(2, observations.Count);
        Assert.NotEqual(observations[0].ObservationId, observations[1].ObservationId);

        var first = store.Query(new MarketRecordQuery(ObservationId: observations[0].ObservationId));
        var second = store.Query(new MarketRecordQuery(ObservationId: observations[1].ObservationId));

        Assert.Equal("100", Assert.Single(first).OrderId);
        Assert.Equal("100", Assert.Single(second).OrderId);
        Assert.NotEqual(first[0].StorageRecordId, second[0].StorageRecordId);
    }

    [Fact]
    public void KeepsResponseKindsAndObservationAssociationsIsolated()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var adapter = new AfmMarketAdapter();
        var processing = new MarketProcessing(store: store);
        var observations = new List<MarketObservation>();

        adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));

        adapter.ProcessMockResponse(CreateOffersResponse((1, "MOCK_OFFERS")));
        adapter.ProcessMockResponse(CreateRequestsResponse((2, "MOCK_REQUESTS")));
        adapter.ProcessMockResponse(CreateLoadoutOffersResponse((3, "MOCK_LOADOUT")));

        Assert.Equal(3, observations.Count);
        Assert.Equal(
            [MarketResponseKind.Offers, MarketResponseKind.Requests, MarketResponseKind.LoadoutOffers],
            observations.Select(x => x.ResponseKind));

        for (var i = 0; i < observations.Count; i++)
        {
            var records = store.Query(new MarketRecordQuery(ObservationId: observations[i].ObservationId));
            var record = Assert.Single(records);
            Assert.Equal(observations[i].ObservationId, record.ObservationId);
            Assert.Equal(observations[i].ResponseKind, record.ResponseKind);
        }

        Assert.Empty(store.Query(new MarketRecordQuery(
            ObservationId: observations[0].ObservationId,
            ResponseKind: MarketResponseKind.Requests)));
    }

    [Fact]
    public void LegacyRecordCanCoexistWithoutBeingIncludedInObservationQuery()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);

        store.Persist(new MarketObservationInput(
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            DateTimeOffset.Parse("2026-10-02T10:00:00Z"),
            [CreateOrder(900, "MOCK_LEGACY")]));

        var adapter = new AfmMarketAdapter();
        var processing = new MarketProcessing(store: store);
        MarketObservation? observation = null;
        adapter.MarketResponseObserved += input => observation = processing.Process(input);

        adapter.ProcessMockResponse(CreateOffersResponse((901, "MOCK_NEW")));

        Assert.NotNull(observation);
        var all = store.ReadAll();
        Assert.Equal(2, all.Count);
        Assert.Null(all[0].ObservationId);

        var associated = store.Query(new MarketRecordQuery(ObservationId: observation!.ObservationId));
        Assert.Single(associated);
        Assert.Equal("901", associated[0].OrderId);
    }

    [Fact]
    public void FailedPersistenceRollsBackOnlyCurrentObservationOperation()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var stable = new MarketObservation(
            Guid.NewGuid(),
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            DateTimeOffset.Parse("2026-10-02T10:00:00Z"),
            [CreateOrder(1, "MOCK_STABLE")]);
        store.Persist(stable);

        var duplicateObservationId = stable.ObservationId;
        var failing = new MarketObservation(
            duplicateObservationId,
            "AuctionGetRequestsResponse",
            MarketResponseKind.Requests,
            null,
            DateTimeOffset.Parse("2026-10-02T10:01:00Z"),
            [CreateOrder(2, "MOCK_SHOULD_ROLLBACK"), CreateOrder(3, "MOCK_SHOULD_ROLLBACK")]);

        Assert.Throws<SqliteException>(() => store.Persist(failing));

        var records = store.ReadAll();
        Assert.Single(records);
        Assert.Equal("1", records[0].OrderId);
        Assert.Equal(stable.ObservationId, records[0].ObservationId);
    }

    [Fact]
    public void SequentialObservationsPreserveRecentStorageOrder()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var adapter = new AfmMarketAdapter();
        var processing = new MarketProcessing(store: store);
        var observations = new List<MarketObservation>();

        adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));

        adapter.ProcessMockResponse(CreateOffersResponse((10, "MOCK_A"), (11, "MOCK_A")));
        adapter.ProcessMockResponse(CreateRequestsResponse((20, "MOCK_B"), (21, "MOCK_B")));
        adapter.ProcessMockResponse(CreateLoadoutOffersResponse((30, "MOCK_C"), (31, "MOCK_C")));
        adapter.ProcessMockResponse(CreateOffersResponse((40, "MOCK_D"), (41, "MOCK_D")));

        Assert.Equal(4, observations.Count);

        var recent = store.Query(new MarketRecordQuery(Limit: 8));

        Assert.Equal(
            ["41", "40", "31", "30", "21", "20", "11", "10"],
            recent.Select(record => record.OrderId));
        Assert.All(
            recent.Take(2),
            record => Assert.Equal(observations[3].ObservationId, record.ObservationId));
        Assert.All(
            recent.Skip(2).Take(2),
            record => Assert.Equal(observations[2].ObservationId, record.ObservationId));
    }

    [Fact]
    public void EmptySyntheticResponseCreatesObservationWithZeroRecords()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var processing = new MarketProcessing(store: store);
        var input = new MarketObservationInput(
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            DateTimeOffset.Parse("2026-10-02T13:00:00Z"),
            []);

        var observation = processing.Process(input);

        Assert.NotEqual(Guid.Empty, observation.ObservationId);
        Assert.Empty(observation.Orders);
        Assert.Empty(store.ReadAll());
        Assert.Empty(store.Query(new MarketRecordQuery(ObservationId: observation.ObservationId)));
    }

    private static AuctionGetOffersResponse CreateOffersResponse(params (int Id, string ItemTypeId)[] orders)
        => new(new Dictionary<byte, object>
        {
            [0] = orders.Select(order => CreateOrderJson(order.Id, order.ItemTypeId)).ToArray()
        });

    private static AuctionGetRequestsResponse CreateRequestsResponse(params (int Id, string ItemTypeId)[] orders)
        => new(new Dictionary<byte, object>
        {
            [0] = orders.Select(order => CreateOrderJson(order.Id, order.ItemTypeId)).ToArray()
        });

    private static AuctionGetLoadoutOffersResponse CreateLoadoutOffersResponse(params (int Id, string ItemTypeId)[] orders)
        => new(new Dictionary<byte, object>
        {
            [1] = new[]
            {
                orders.Select(order => CreateOrderJson(order.Id, order.ItemTypeId)).ToArray()
            }
        });

    private static string CreateOrderJson(int id, string itemTypeId)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            Id = id,
            ItemTypeId = itemTypeId,
            ItemGroupTypeId = "MOCK_GROUP",
            LocationId = 1001,
            QualityLevel = 1,
            EnchantmentLevel = 0,
            UnitPriceSilver = 1234L,
            Amount = 2,
            AuctionType = "MOCK_AUCTION",
            Expires = "2030-01-01T00:00:00Z",
            DistanceFee = 0,
            Location = "MOCK_CAERLEON"
        });

    private static MarketOrderInput CreateOrder(int id, string itemTypeId)
        => new(
            id,
            itemTypeId,
            "MOCK_GROUP",
            1001,
            1,
            0,
            1234L,
            2,
            "MOCK_AUCTION",
            "2030-01-01T00:00:00Z",
            0,
            "MOCK_CAERLEON");

    private sealed class TemporaryDatabase : IDisposable
    {
        private TemporaryDatabase(string path) => Path = path;
        public string Path { get; }

        public static TemporaryDatabase Create()
            => new(System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"afm-reforge-step12-{Guid.NewGuid():N}.db"));

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
