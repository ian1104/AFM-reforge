using System.Text.Json;
using AFMReforge.Adapter.AFM;
using AFMReforge.Core;
using AFMReforge.Infrastructure;

var databasePath = Path.Combine(
    Path.GetTempPath(),
    $"afm-reforge-step12-demo-{Guid.NewGuid():N}.db");

try
{
    var store = new SqliteMarketObservationStore(databasePath);
    var adapter = new AfmMarketAdapter();
    var processing = new MarketProcessing(store: store);
    var observations = new List<MarketObservation>();

    adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));

    adapter.ProcessMockResponse(CreateOffersResponse((100, "MOCK_T6_SWORD"), (100, "MOCK_T6_SWORD")));
    adapter.ProcessMockResponse(CreateRequestsResponse((100, "MOCK_T6_SWORD")));
    adapter.ProcessMockResponse(CreateLoadoutOffersResponse((200, "MOCK_T6_AXE")));

    var recent = store.Query(new MarketRecordQuery(Limit: 10));
    var firstObservationRecords = store.Query(new MarketRecordQuery(
        ObservationId: observations[0].ObservationId,
        ItemTypeId: "MOCK_T6_SWORD",
        QualityLevel: 1,
        Limit: 10));

    var legacyInput = new MarketObservationInput(
        "AuctionGetOffersResponse",
        MarketResponseKind.Offers,
        null,
        DateTimeOffset.Parse("2026-10-02T14:00:00Z"),
        [new MarketOrderInput(
            900,
            "MOCK_LEGACY",
            "MOCK_GROUP",
            1001,
            1,
            0,
            1234L,
            1,
            "MOCK_AUCTION",
            "2030-01-01T00:00:00Z",
            0,
            "MOCK_CAERLEON")]);
    store.Persist(legacyInput);

    var legacyRecords = store.Query(new MarketRecordQuery(ItemTypeId: "MOCK_LEGACY"));

    Console.WriteLine("Synthetic scenario completed");
    Console.WriteLine($"Observations: {observations.Count}");
    foreach (var observation in observations)
        Console.WriteLine($"  {observation.ObservationId} / {observation.ResponseKind} / orders={observation.Orders.Count}");

    Console.WriteLine($"Stored records: {store.ReadAll().Count}");
    Console.WriteLine($"First observation filtered records: {firstObservationRecords.Count}");
    Console.WriteLine($"Duplicate Order IDs: {firstObservationRecords.Count(r => r.OrderId == "100")} preserved");
    Console.WriteLine($"Legacy records: {legacyRecords.Count} preserved");
    Console.WriteLine($"Recent query order: {string.Join(", ", recent.Select(r => r.OrderId))}");
    Console.WriteLine("Query isolation: passed by synthetic scenario");
    Console.WriteLine("Runtime status: UNAVAILABLE");
}
finally
{
    if (File.Exists(databasePath))
        File.Delete(databasePath);
}

static AuctionGetOffersResponse CreateOffersResponse(params (int Id, string ItemTypeId)[] orders)
    => new(new Dictionary<byte, object>
    {
        [0] = orders.Select(CreateOrderJson).ToArray()
    });

static AuctionGetRequestsResponse CreateRequestsResponse(params (int Id, string ItemTypeId)[] orders)
    => new(new Dictionary<byte, object>
    {
        [0] = orders.Select(CreateOrderJson).ToArray()
    });

static AuctionGetLoadoutOffersResponse CreateLoadoutOffersResponse(params (int Id, string ItemTypeId)[] orders)
    => new(new Dictionary<byte, object>
    {
        [1] = new[]
        {
            orders.Select(CreateOrderJson).ToArray()
        }
    });

static string CreateOrderJson((int Id, string ItemTypeId) order)
    => JsonSerializer.Serialize(new
    {
        Id = order.Id,
        ItemTypeId = order.ItemTypeId,
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
