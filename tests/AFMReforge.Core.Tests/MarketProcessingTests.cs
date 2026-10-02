using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class MarketProcessingTests
{
    [Fact]
    public void ProcessesSingleInputIntoInMemoryState()
    {
        var capturedAt = DateTimeOffset.Parse("2026-10-02T12:00:00Z");
        var input = CreateInput(MarketResponseKind.Offers, capturedAt, 42);

        var processing = new MarketProcessing();
        processing.Process(input);

        var stored = Assert.Single(processing.State.Inputs);
        Assert.Equal(input, stored);
        Assert.Equal(capturedAt, stored.CapturedAt);
        Assert.Equal(42, stored.Orders[0].Id);
    }

    [Fact]
    public void PreservesMultipleInputsInArrivalOrderWithoutDefiningDeduplication()
    {
        var first = CreateInput(MarketResponseKind.Offers, DateTimeOffset.Parse("2026-10-02T12:00:00Z"), 1);
        var second = CreateInput(MarketResponseKind.Requests, DateTimeOffset.Parse("2026-10-02T12:00:01Z"), 1);
        var third = CreateInput(MarketResponseKind.LoadoutOffers, DateTimeOffset.Parse("2026-10-02T12:00:02Z"), 2);

        var processing = new MarketProcessing();
        processing.Process(first);
        processing.Process(second);
        processing.Process(third);

        Assert.Equal(3, processing.State.InputCount);
        Assert.Equal([MarketResponseKind.Offers, MarketResponseKind.Requests, MarketResponseKind.LoadoutOffers],
            processing.State.Inputs.Select(x => x.ResponseKind));
    }

    [Fact]
    public void PreservesEmptyInput()
    {
        var input = new MarketObservationInput(
            "AuctionGetOffersResponse",
            MarketResponseKind.Offers,
            null,
            DateTimeOffset.Parse("2026-10-02T12:00:00Z"),
            []);

        var processing = new MarketProcessing();
        processing.Process(input);

        Assert.Empty(processing.State.Inputs[0].Orders);
    }

    private static MarketObservationInput CreateInput(
        MarketResponseKind kind,
        DateTimeOffset capturedAt,
        int id)
        => new(
            kind switch
            {
                MarketResponseKind.Offers => "AuctionGetOffersResponse",
                MarketResponseKind.Requests => "AuctionGetRequestsResponse",
                _ => "AuctionGetLoadoutOffersResponse"
            },
            kind,
            null,
            capturedAt,
            [new MarketOrderInput(id, "MOCK_ITEM", "MOCK_GROUP", 1001, 1, 0, 1234L, 2, "MOCK", null, 0, "MOCK_LOCATION")]);
}
