using AFMReforge.Core;
using AFMReforge.Infrastructure;
using Microsoft.Data.Sqlite;

namespace AFMReforge.Infrastructure.Tests;

public sealed class SqliteMarketObservationRelationTests
{
    [Fact]
    public void PersistsObservationCandidateAndRecordsWithSameObservationId()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var observation = CreateObservation(Guid.NewGuid(), 10, 11);

        store.Persist(observation);

        var records = store.ReadAll();
        Assert.Equal(2, records.Count);
        Assert.All(records, record => Assert.Equal(observation.ObservationId, record.ObservationId));
        var associated = store.Query(new MarketRecordQuery(ObservationId: observation.ObservationId));
        Assert.Equal(2, associated.Count);
        Assert.All(associated, record => Assert.Equal(observation.ObservationId, record.ObservationId));
    }

    [Fact]
    public void MultipleRecordsCanBelongToOneObservation()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var observation = CreateObservation(Guid.NewGuid(), 1, 2, 3);

        store.Persist(observation);

        var records = store.Query(new MarketRecordQuery(ObservationId: observation.ObservationId, Limit: 10));

        Assert.Equal(3, records.Count);
        Assert.All(records, record => Assert.Equal(observation.ObservationId, record.ObservationId));
        Assert.Equal(["3", "2", "1"], records.Select(x => x.OrderId));
    }

    [Fact]
    public void DuplicateOrderIdsRemainSeparateRecordsWithinOneObservation()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var observation = CreateObservation(Guid.NewGuid(), 100, 100);

        store.Persist(observation);

        var records = store.Query(new MarketRecordQuery(ObservationId: observation.ObservationId, Limit: 10));

        Assert.Equal(2, records.Count);
        Assert.All(records, record => Assert.Equal("100", record.OrderId));
        Assert.All(records, record => Assert.Equal(observation.ObservationId, record.ObservationId));
        Assert.NotEqual(records[0].StorageRecordId, records[1].StorageRecordId);
    }

    [Fact]
    public void DifferentObservationsDoNotMergeEvenForSameItemAndLocation()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var first = CreateObservation(Guid.NewGuid(), 1);
        var second = CreateObservation(Guid.NewGuid(), 2);

        store.Persist(first);
        store.Persist(second);

        var firstRecords = store.Query(new MarketRecordQuery(ObservationId: first.ObservationId));
        var secondRecords = store.Query(new MarketRecordQuery(ObservationId: second.ObservationId));

        Assert.Single(firstRecords);
        Assert.Single(secondRecords);
        Assert.NotEqual(firstRecords[0].StorageRecordId, secondRecords[0].StorageRecordId);
        Assert.NotEqual(firstRecords[0].ObservationId, secondRecords[0].ObservationId);
    }

    [Fact]
    public void LegacyRecordPersistenceLeavesObservationIdNull()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var input = new MarketObservationInput(
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            DateTimeOffset.Parse("2026-10-02T12:00:00Z"),
            [CreateOrder(77)]);

        store.Persist(input);

        var record = Assert.Single(store.ReadAll());
        Assert.Null(record.ObservationId);
        Assert.Empty(store.Query(new MarketRecordQuery(ObservationId: Guid.NewGuid())));
    }

    [Fact]
    public void LegacyDatabaseWithoutObservationColumnIsMigratedWithoutChangingExistingRows()
    {
        using var database = TemporaryDatabase.Create();
        CreateLegacyDatabase(database.Path);

        var store = new SqliteMarketObservationStore(database.Path);
        var legacy = Assert.Single(store.ReadAll());

        Assert.Null(legacy.ObservationId);
        Assert.Equal("77", legacy.OrderId);

        var observation = CreateObservation(Guid.NewGuid(), 88);
        store.Persist(observation);

        Assert.Equal(2, store.ReadAll().Count);
        Assert.Single(store.Query(new MarketRecordQuery(ObservationId: observation.ObservationId)));
    }

    [Fact]
    public void FailedObservationPersistenceDoesNotLeavePartialSecondObservation()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteMarketObservationStore(database.Path);
        var observationId = Guid.NewGuid();

        store.Persist(CreateObservation(observationId, 1));

        Assert.Throws<SqliteException>(() => store.Persist(CreateObservation(observationId, 2, 3)));

        var records = store.ReadAll();
        Assert.Single(records);
        Assert.Equal("1", records[0].OrderId);
        Assert.Equal(observationId, records[0].ObservationId);
    }

    private static MarketObservation CreateObservation(Guid id, params int[] orderIds)
        => new(
            id,
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            DateTimeOffset.Parse("2026-10-02T12:00:00Z"),
            orderIds.Select(CreateOrder).ToArray());

    private static MarketRecord CreateOrder(int id)
        => new(
            (ulong)id,
            "MOCK_T6_SWORD",
            "MOCK_GROUP",
            "1001",
            1,
            0,
            1234,
            2,
            MarketOrderType.Unknown,
            "2030-01-01T00:00:00Z",
            0);

    private static void CreateLegacyDatabase(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path
        }.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE market_observation_records (
                StorageRecordId INTEGER PRIMARY KEY AUTOINCREMENT,
                ResponseType TEXT NOT NULL,
                ResponseKind TEXT NOT NULL,
                OperationCode TEXT NULL,
                CapturedAt TEXT NULL,
                OrderId TEXT NULL,
                ItemTypeId TEXT NULL,
                ItemGroupTypeId TEXT NULL,
                LocationId TEXT NULL,
                QualityLevel TEXT NULL,
                EnchantmentLevel TEXT NULL,
                UnitPriceSilver TEXT NULL,
                Amount TEXT NULL,
                AuctionType TEXT NULL,
                Expires TEXT NULL,
                DistanceFee TEXT NULL,
                ResolvedLocation TEXT NULL
            );

            INSERT INTO market_observation_records (
                ResponseType, ResponseKind, CapturedAt, OrderId, ItemTypeId, LocationId
            )
            VALUES (
                'AuctionGetOffersResponse', 'Offers', '2026-10-02T11:00:00.0000000+00:00',
                '77', 'MOCK_T6_SWORD', '1001'
            );
            """;
        command.ExecuteNonQuery();
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private TemporaryDatabase(string path) => Path = path;
        public string Path { get; }

        public static TemporaryDatabase Create()
            => new(System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"afm-reforge-step10-{Guid.NewGuid():N}.db"));

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
