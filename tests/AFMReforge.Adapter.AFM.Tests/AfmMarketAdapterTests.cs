using System.Text.Json;
using AFMReforge.Core;
using AFMReforge.Adapter.AFM;
using AlbionDataAvalonia.Network.Responses;

namespace AFMReforge.Adapter.AFM.Tests;

public sealed class AfmMarketAdapterTests
{
    [Fact]
    public void MapsOffersResponseToReforgeOwnedInput()
    {
        var capturedAt = DateTimeOffset.Parse("2026-10-02T12:00:00Z");
        var response = CreateOffersResponse(42, capturedAt);

        var input = new AfmMarketAdapter().Map(response);

        Assert.Equal(nameof(AuctionGetOffersResponse), input.ResponseType);
        Assert.Equal(MarketResponseKind.Offers, input.ResponseKind);
        Assert.Equal(capturedAt, input.CapturedAt);
        AssertMappedOrder(input, 42);
    }

    [Fact]
    public void MapsRequestsResponseToSameReforgePipeline()
    {
        var response = new AuctionGetRequestsResponse(new Dictionary<byte, object>
        {
            [0] = new[] { CreateOrderJson(43, "T4_MOCK_REQUEST") }
        });

        var input = new AfmMarketAdapter().Map(response);

        Assert.Equal(MarketResponseKind.Requests, input.ResponseKind);
        AssertMappedOrder(input, 43);
    }

    [Fact]
    public void MapsLoadoutResponseToSameReforgePipeline()
    {
        var response = new AuctionGetLoadoutOffersResponse(new Dictionary<byte, object>
        {
            [1] = new[]
            {
                new[]
                {
                    CreateOrderJson(44, "T4_MOCK_LOADOUT")
                }
            }
        });

        var input = new AfmMarketAdapter().Map(response);

        Assert.Equal(MarketResponseKind.LoadoutOffers, input.ResponseKind);
        AssertMappedOrder(input, 44);
    }

    [Fact]
    public void PreservesEmptyOffersCollection()
    {
        var response = new AuctionGetOffersResponse(new Dictionary<byte, object>
        {
            [0] = Array.Empty<string>()
        });

        var input = new AfmMarketAdapter().Map(response);

        Assert.Empty(input.Orders);
    }

    [Fact]
    public void AdapterToCorePipelinePreservesFields()
    {
        var capturedAt = DateTimeOffset.Parse("2026-10-02T12:00:00Z");
        var input = new AfmMarketAdapter().Map(CreateOffersResponse(45, capturedAt));
        var processing = new MarketProcessing();

        processing.Process(input);

        var stored = Assert.Single(processing.State.Inputs);
        Assert.Equal(input.ResponseKind, stored.ResponseKind);
        Assert.Equal(input.CapturedAt, stored.CapturedAt);
        Assert.Equal(input.Orders, stored.Orders);
        Assert.Equal("T4_MOCK", stored.Orders[0].ItemTypeId);
        Assert.Equal(1234L, stored.Orders[0].UnitPriceSilver);
        Assert.Equal("MOCK_LOCATION", stored.Orders[0].ResolvedLocation);
    }

    private static AuctionGetOffersResponse CreateOffersResponse(int id, DateTimeOffset capturedAt)
    {
        var response = new AuctionGetOffersResponse(new Dictionary<byte, object>
        {
            [0] = new[] { CreateOrderJson(id, "T4_MOCK") }
        });

        // BaseOperation.CapturedAt is read-only in the AFM DTO; this test verifies
        // the actual constructor path and does not invent a setter for runtime metadata.
        Assert.NotNull(response);
        return response;
    }

    private static void AssertMappedOrder(MarketObservationInput input, int expectedId)
    {
        var order = Assert.Single(input.Orders);
        Assert.Equal(expectedId, order.Id);
        Assert.NotNull(order.ItemTypeId);
        Assert.NotNull(order.UnitPriceSilver);
        Assert.NotNull(order.Amount);
        Assert.NotNull(order.ResolvedLocation);
    }

    private static string CreateOrderJson(int id, string itemTypeId)
        => JsonSerializer.Serialize(new
        {
            Id = id,
            ItemTypeId = itemTypeId,
            ItemGroupTypeId = "MOCK_GROUP",
            LocationId = 1001,
            QualityLevel = 1,
            EnchantmentLevel = 0,
            UnitPriceSilver = 1234L,
            Amount = 2,
            AuctionType = "MOCK_AUCTION_TYPE",
            Expires = "2030-01-01T00:00:00Z",
            DistanceFee = 0,
            Location = "MOCK_LOCATION"
        });
}
