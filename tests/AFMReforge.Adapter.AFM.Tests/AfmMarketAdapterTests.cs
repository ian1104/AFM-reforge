using System.Text.Json;
using AFMReforge.Core;
using AFMReforge.Adapter.AFM;
using AlbionDataAvalonia.Network.Responses;

namespace AFMReforge.Adapter.AFM.Tests;

public sealed class AfmMarketAdapterTests
{
    [Fact]
    public void MapsOffersResponseToCanonicalRecord()
    {
        var input = new AfmMarketAdapter().Map(CreateOffersResponse(42));

        Assert.Equal(nameof(AuctionGetOffersResponse), input.ResponseType);
        Assert.Equal(MarketResponseKind.Offers, input.ResponseKind);
        var record = Assert.Single(input.Records);
        Assert.Equal(42UL, record.OrderId);
        Assert.Equal("T4_MOCK", record.ItemTypeId);
        Assert.Equal("MOCK_GROUP", record.ItemGroupTypeId);
        Assert.Equal("1001", record.LocationId);
        Assert.Equal((byte)1, record.QualityLevel);
        Assert.Equal((byte)0, record.EnchantmentLevel);
        Assert.Equal(1234UL, record.UnitPriceSilver);
        Assert.Equal(2U, record.Amount);
        Assert.Equal(MarketOrderType.Unknown, record.AuctionType);
        Assert.Equal("2030-01-01T00:00:00Z", record.Expires);
        Assert.Equal(0UL, record.DistanceFee);
    }

    [Fact]
    public void PreservesCanonicalNumericTypesAndAuctionType()
    {
        var mapper = new MarketOrderMapper();
        var record = mapper.Map(new
        {
            Id = 42UL,
            ItemTypeId = "MOCK_ITEM",
            ItemGroupTypeId = "MOCK_GROUP",
            LocationId = "1001",
            QualityLevel = (byte)3,
            EnchantmentLevel = (byte)2,
            UnitPriceSilver = 987654UL,
            Amount = 7U,
            AuctionType = "offer",
            Expires = "2030-01-01T00:00:00Z",
            DistanceFee = 321UL
        });

        Assert.Equal(42UL, record.OrderId);
        Assert.Equal((byte)3, record.QualityLevel);
        Assert.Equal((byte)2, record.EnchantmentLevel);
        Assert.Equal(987654UL, record.UnitPriceSilver);
        Assert.Equal(7U, record.Amount);
        Assert.Equal(MarketOrderType.Offer, record.AuctionType);
        Assert.Equal(321UL, record.DistanceFee);
    }

    [Fact]
    public void RequiredCanonicalFieldsRejectNullValues()
    {
        var mapper = new MarketOrderMapper();
        var order = new
        {
            Id = 42UL,
            ItemTypeId = (string?)null,
            ItemGroupTypeId = "MOCK_GROUP",
            LocationId = "1001",
            QualityLevel = (byte)1,
            EnchantmentLevel = (byte)0,
            UnitPriceSilver = 1234UL,
            Amount = 2U,
            AuctionType = "request",
            Expires = "2030-01-01T00:00:00Z",
            DistanceFee = 0UL
        };

        Assert.Throws<InvalidOperationException>(() => mapper.Map(order));
    }

    [Fact]
    public void MapsRequestsResponseToSameReforgePipeline()
    {
        var input = new AfmMarketAdapter().Map(new AuctionGetRequestsResponse(new Dictionary<byte, object>
        {
            [0] = new[] { CreateOrderJson(43, "T4_MOCK_REQUEST") }
        }));

        Assert.Equal(MarketResponseKind.Requests, input.ResponseKind);
        Assert.Equal(43UL, Assert.Single(input.Records).OrderId);
    }

    [Fact]
    public void MapsLoadoutResponseToSameReforgePipeline()
    {
        var input = new AfmMarketAdapter().Map(new AuctionGetLoadoutOffersResponse(new Dictionary<byte, object>
        {
            [1] = new[] { new[] { CreateOrderJson(44, "T4_MOCK_LOADOUT") } }
        }));

        Assert.Equal(MarketResponseKind.LoadoutOffers, input.ResponseKind);
        Assert.Equal(44UL, Assert.Single(input.Records).OrderId);
    }

    [Fact]
    public void PreservesEmptyOffersCollection()
    {
        var input = new AfmMarketAdapter().Map(new AuctionGetOffersResponse(new Dictionary<byte, object>
        {
            [0] = Array.Empty<string>()
        }));

        Assert.Empty(input.Records);
    }

    [Fact]
    public void AdapterToCorePipelineExposesCanonicalRecordsOnly()
    {
        var input = new AfmMarketAdapter().Map(CreateOffersResponse(45));
        var processing = new MarketProcessing();

        processing.Process(input);

        var stored = Assert.Single(processing.State.Inputs);
        var record = Assert.Single(stored.Records);

        Assert.Equal(input.ResponseKind, stored.ResponseKind);
        Assert.Equal(input.CapturedAt, stored.CapturedAt);
        Assert.Equal(input.Records, stored.Records);
        Assert.Equal("T4_MOCK", record.ItemTypeId);
        Assert.Equal(1234UL, record.UnitPriceSilver);
        Assert.Equal("1001", record.LocationId);
    }

    private static AuctionGetOffersResponse CreateOffersResponse(int id)
        => new(new Dictionary<byte, object>
        {
            [0] = new[] { CreateOrderJson(id, "T4_MOCK") }
        });

    private static string CreateOrderJson(int id, string itemTypeId)
        => JsonSerializer.Serialize(new
        {
            Id = id,
            ItemTypeId = itemTypeId,
            ItemGroupTypeId = "MOCK_GROUP",
            LocationId = "1001",
            QualityLevel = 1,
            EnchantmentLevel = 0,
            UnitPriceSilver = 1234L,
            Amount = 2,
            AuctionType = "unknown",
            Expires = "2030-01-01T00:00:00Z",
            DistanceFee = 0,
            Location = "MOCK_LOCATION"
        });
}