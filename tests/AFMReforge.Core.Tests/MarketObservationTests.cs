using AFMReforge.Core;

namespace AFMReforge.Core.Tests;

public sealed class MarketObservationTests
{
    [Fact]
    public void SingleResponseProducesOneObservationCandidate()
    {
        var input = CreateInput(MarketResponseKind.Offers, 1);

        var observation = new MarketObservationFactory().Create(input);

        Assert.Equal(input.ResponseType, observation.ResponseType);
        Assert.Equal(input.ResponseKind, observation.ResponseKind);
        Assert.Single(observation.Orders);
        Assert.NotEqual(Guid.Empty, observation.ObservationId);
    }

    [Fact]
    public void MultipleOrdersRemainGroupedUnderTheSameInputBoundary()
    {
        var input = CreateInput(
            MarketResponseKind.Offers,
            1,
            2,
            3);

        var observation = new MarketObservationFactory().Create(input);

        Assert.Equal(3, observation.Orders.Count);
        Assert.Equal([1, 2, 3], observation.Orders.Select(x => x.Id));
    }

    [Fact]
    public void EachResponseKindIsPreservedWithoutMerging()
    {
        var factory = new MarketObservationFactory();

        var offers = factory.Create(CreateInput(MarketResponseKind.Offers, 1));
        var requests = factory.Create(CreateInput(MarketResponseKind.Requests, 2));
        var loadoutOffers = factory.Create(CreateInput(MarketResponseKind.LoadoutOffers, 3));

        Assert.Equal(MarketResponseKind.Offers, offers.ResponseKind);
        Assert.Equal(MarketResponseKind.Requests, requests.ResponseKind);
        Assert.Equal(MarketResponseKind.LoadoutOffers, loadoutOffers.ResponseKind);
        Assert.NotEqual(offers.ObservationId, requests.ObservationId);
        Assert.NotEqual(requests.ObservationId, loadoutOffers.ObservationId);
    }

    [Fact]
    public void CapturedAtIsPreservedAsCapturedAt()
    {
        var capturedAt = DateTimeOffset.Parse("2026-10-02T12:34:56.789Z");
        var input = CreateInput(MarketResponseKind.Offers, 1, capturedAt: capturedAt);

        var observation = new MarketObservationFactory().Create(input);

        Assert.Equal(capturedAt, observation.CapturedAt);
    }

    [Fact]
    public void DuplicateOrderIdsDoNotBecomeObservationIdentity()
    {
        var factory = new MarketObservationFactory();
        var first = factory.Create(CreateInput(MarketResponseKind.Offers, 42));
        var second = factory.Create(CreateInput(MarketResponseKind.Offers, 42));

        Assert.Equal(42, first.Orders.Single().Id);
        Assert.Equal(42, second.Orders.Single().Id);
        Assert.NotEqual(first.ObservationId, second.ObservationId);
    }

    [Fact]
    public void DifferentResponsesAreNotAutomaticallyMerged()
    {
        var factory = new MarketObservationFactory();

        var first = factory.Create(CreateInput(MarketResponseKind.Offers, 1, itemTypeId: "MOCK_T6_SWORD"));
        var second = factory.Create(CreateInput(MarketResponseKind.Offers, 2, itemTypeId: "MOCK_T6_SWORD"));

        Assert.NotEqual(first.ObservationId, second.ObservationId);
        Assert.Equal("MOCK_T6_SWORD", first.Orders.Single().ItemTypeId);
        Assert.Equal("MOCK_T6_SWORD", second.Orders.Single().ItemTypeId);
    }

    private static MarketObservationInput CreateInput(
        MarketResponseKind kind,
        params int[] orderIds)
        => CreateInput(kind, orderIds, DateTimeOffset.Parse("2026-10-02T12:00:00Z"), "MOCK_T6_SWORD");

    private static MarketObservationInput CreateInput(
        MarketResponseKind kind,
        int orderId,
        DateTimeOffset capturedAt)
        => CreateInput(kind, [orderId], capturedAt, "MOCK_T6_SWORD");

    private static MarketObservationInput CreateInput(
        MarketResponseKind kind,
        int[] orderIds,
        DateTimeOffset capturedAt,
        string itemTypeId)
        => new(
            kind switch
            {
                MarketResponseKind.Offers => "AuctionGetOffersResponse",
                MarketResponseKind.Requests => "AuctionGetRequestsResponse",
                _ => "AuctionGetLoadoutOffersResponse"
            },
            kind,
            42,
            capturedAt,
            orderIds.Select(id => new MarketOrderInput(
                id,
                itemTypeId,
                "MOCK_GROUP",
                1001,
                1,
                0,
                1234L,
                2,
                "MOCK_AUCTION",
                null,
                0,
                "MOCK_CAERLEON")).ToArray());
}
