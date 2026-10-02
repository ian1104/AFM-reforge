using System.Text.Json;
using AFMReforge.Adapter.AFM;
using AFMReforge.Core;
using AFMReforge.Infrastructure;

var databasePath = Path.Combine(Path.GetTempPath(), $"afm-reforge-step13-demo-{Guid.NewGuid():N}.db");

try
{
    var store = new SqliteMarketObservationStore(databasePath);
    var adapter = new AfmMarketAdapter();
    var processing = new MarketProcessing(store: store);
    var observations = new List<MarketObservation>();

    adapter.MarketResponseObserved += input => observations.Add(processing.Process(input));

    adapter.ProcessMockResponse(CreateOffersResponse((100, "MOCK_T6_SWORD"), (100, "MOCK_T6_SWORD")));
    adapter.ProcessMockResponse(CreateRequestsResponse((200, "MOCK_T6_SWORD")));
    adapter.ProcessMockResponse(CreateLoadoutOffersResponse((300, "MOCK_T6_AXE")));

    var recent = store.Query(new MarketRecordQuery(Limit: 10));
    var firstObservationRecords = store.Query(new MarketRecordQuery(
        ObservationId: observations[0].ObservationId,
        ItemTypeId: "MOCK_T6_SWORD",
        QualityLevel: 1,
        Limit: 10));

    Console.WriteLine("Step 13 canonical pipeline completed");
    Console.WriteLine($"Observations: {observations.Count}");
    Console.WriteLine($"Canonical records in first observation: {firstObservationRecords.Count}");
    Console.WriteLine($"Duplicate Order IDs preserved: {firstObservationRecords.Count(r => r.OrderId == "100")}");
    Console.WriteLine($"Recent query records: {recent.Count}");
    Console.WriteLine("AFM DTO exposure after adapter boundary: none");
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
        [1] = new[] { orders.Select(CreateOrderJson).ToArray() }
    });

static string CreateOrderJson((int Id, string ItemTypeId) order)
    => JsonSerializer.Serialize(new
    {
        order.Id,
        order.ItemTypeId,
        ItemGroupTypeId = "MOCK_GROUP",
        LocationId = "1001",
        QualityLevel = 1,
        EnchantmentLevel = 0,
        UnitPriceSilver = 1234L,
        Amount = 2,
        AuctionType = "MOCK_AUCTION",
        Expires = "2030-01-01T00:00:00Z",
        DistanceFee = 0,
        Location = "MOCK_CAERLEON"
    });
