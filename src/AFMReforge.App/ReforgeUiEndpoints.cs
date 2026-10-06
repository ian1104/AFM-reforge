using System.Globalization;
using AFMReforge.Core;
using AFMReforge.Infrastructure;

namespace AFMReforge.App;

public static class ReforgeUiEndpoints
{
    public static void Map(WebApplication app, SqliteMarketObservationStore store)
    {
        app.MapGet("/api/diagnostics", (RuntimeDiagnosticsState diagnostics) => Results.Ok(diagnostics.Snapshot()));

        app.MapGet("/api/overview", () =>
        {
            var observations = store.ReadObservations();
            var records = store.ReadAll();

            return Results.Ok(new
            {
                observations = observations.Count,
                records = records.Count,
                itemTypes = records.Where(x => x.ItemTypeId is not null).Select(x => x.ItemTypeId!).Distinct(StringComparer.Ordinal).Count(),
                locations = records.Where(x => x.LocationId is not null).Select(x => x.LocationId!).Distinct(StringComparer.Ordinal).Count(),
                latestCapturedAt = records
                    .Select(x => TryParseDateTimeOffset(x.CapturedAt))
                    .Where(x => x.HasValue)
                    .OrderByDescending(x => x)
                    .FirstOrDefault(),
                responseKinds = observations
                    .GroupBy(x => x.ResponseKind)
                    .ToDictionary(x => x.Key.ToString(), x => x.Count()),
                environment = string.Equals(Environment.GetEnvironmentVariable("AFM_REFORGE_DEMO"), "1", StringComparison.OrdinalIgnoreCase) ? "DEMO" : "STORED DATA"
            });
        });

        app.MapGet("/api/observations", (int? limit, int? offset) =>
        {
            var take = Math.Clamp(limit ?? 100, 1, 500);
            var skip = Math.Max(offset ?? 0, 0);
            var observations = store.ReadObservations()
                .Skip(skip)
                .Take(take)
                .Select(o =>
                {
                    var records = store.Query(new MarketRecordQuery(ObservationId: o.ObservationId, Limit: 5000));
                    var scope = TryCalculateScope(records);
                    return new
                    {
                        observationId = o.ObservationId,
                        capturedAt = o.CapturedAt,
                        responseKind = o.ResponseKind.ToString(),
                        recordCount = records.Count,
                        scope = scope
                    };
                });

            return Results.Ok(observations);
        });

        app.MapGet("/api/observations/{id:guid}", (Guid id) =>
        {
            var observation = store.ReadObservations().FirstOrDefault(x => x.ObservationId == id);
            if (observation is null)
                return Results.NotFound();

            var records = store.Query(new MarketRecordQuery(ObservationId: id, Limit: 5000));
            return Results.Ok(new
            {
                observationId = observation.ObservationId,
                capturedAt = observation.CapturedAt,
                responseKind = observation.ResponseKind.ToString(),
                recordCount = records.Count,
                scope = TryCalculateScope(records),
                records
            });
        });

        app.MapGet("/api/records", (
            string? itemTypeId,
            string? locationId,
            byte? qualityLevel,
            byte? enchantmentLevel,
            MarketResponseKind? responseKind,
            Guid? observationId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? limit,
            int? offset) =>
        {
            var query = new MarketRecordQuery(
                ItemTypeId: itemTypeId,
                LocationId: locationId,
                QualityLevel: qualityLevel,
                EnchantmentLevel: enchantmentLevel,
                ResponseKind: responseKind,
                From: from,
                To: to,
                Limit: Math.Clamp(limit ?? 100, 1, 1000),
                Offset: Math.Max(offset ?? 0, 0),
                ObservationId: observationId);

            return Results.Ok(store.Query(query));
        });

        app.MapGet("/api/groups", (int? limit, int? offset) =>
        {
            var rows = store.Query(new MarketRecordQuery(Limit: 5000));
            var records = new MarketRecord[rows.Count];
            for (var i = 0; i < rows.Count; i++)
                records[i] = ToDomainRecord(rows[i]);

            var orderedGroups = MarketRecordGrouping.GroupByKey(records)
                .OrderByDescending(x => x.Value.Count)
                .Skip(Math.Max(offset ?? 0, 0))
                .Take(Math.Clamp(limit ?? 100, 1, 500))
                .ToArray();

            var groupResults = new List<object>(orderedGroups.Length);
            var startIndex = Math.Max(offset ?? 0, 0);
            for (var i = 0; i < orderedGroups.Length; i++)
            {
                var group = orderedGroups[i];
                groupResults.Add(new
                {
                    index = startIndex + i + 1,
                    key = group.Key,
                    recordCount = group.Value.Count,
                    metrics = new ObservedOrderMetricsCalculator().Calculate(group.Value)
                });
            }

            return Results.Ok(groupResults);
        });

        app.MapGet("/api/groups/records", (
            string itemTypeId,
            string locationId,
            byte qualityLevel,
            byte enchantmentLevel,
            MarketOrderType auctionType) =>
        {
            var records = store.Query(new MarketRecordQuery(
                ItemTypeId: itemTypeId,
                LocationId: locationId,
                QualityLevel: qualityLevel,
                EnchantmentLevel: enchantmentLevel,
                Limit: 5000));

            var filtered = records
                .Where(x => string.Equals(x.AuctionType, auctionType.ToString(), StringComparison.Ordinal))
                .ToArray();

            return Results.Ok(filtered);
        });

        app.MapGet("/api/analysis", (
            string itemTypeId,
            string locationId,
            byte qualityLevel,
            byte enchantmentLevel,
            MarketOrderType auctionType) =>
        {
            var rows = store.Query(new MarketRecordQuery(
                ItemTypeId: itemTypeId,
                LocationId: locationId,
                QualityLevel: qualityLevel,
                EnchantmentLevel: enchantmentLevel,
                Limit: 5000));

            var records = new MarketRecord[rows.Count];
            var recordIndex = 0;
            foreach (var row in rows)
            {
                if (string.Equals(row.AuctionType, auctionType.ToString(), StringComparison.Ordinal))
                    records[recordIndex++] = ToDomainRecord(row);
            }

            if (recordIndex != records.Length)
                Array.Resize(ref records, recordIndex);

            var metrics = new ObservedOrderMetricsCalculator().Calculate(records);
            return Results.Ok(new
            {
                key = new MarketGroupingKey(itemTypeId, locationId, qualityLevel, enchantmentLevel, auctionType),
                metrics,
                points = rows
                    .Where(x => string.Equals(x.AuctionType, auctionType.ToString(), StringComparison.Ordinal))
                    .Select(x => new
                    {
                        x.StorageRecordId,
                        x.CapturedAt,
                        x.UnitPriceSilver,
                        x.Amount
                    })
            });
        });
    }

    private static object TryCalculateScope(IReadOnlyList<MarketRecordView> rows)
    {
        var records = new MarketRecord[rows.Count];
        for (var i = 0; i < rows.Count; i++)
            records[i] = ToDomainRecord(rows[i]);

        var scope = new MarketObservationScopeCalculator().Calculate(records);

        return new
        {
            itemTypes = scope.ItemTypeIds,
            locations = scope.LocationIds,
            qualities = scope.QualityLevels,
            enchantments = scope.EnchantmentLevels,
            auctionTypes = scope.AuctionTypes.Select(x => x.ToString()).ToArray()
        };
    }

    private static MarketRecord ToDomainRecord(MarketRecordView row)
        => new(
            ParseUlong(row.OrderId, nameof(row.OrderId)),
            row.ItemTypeId ?? string.Empty,
            row.ItemGroupTypeId ?? string.Empty,
            row.LocationId ?? string.Empty,
            ParseByte(row.QualityLevel, nameof(row.QualityLevel)),
            ParseByte(row.EnchantmentLevel, nameof(row.EnchantmentLevel)),
            ParseUlong(row.UnitPriceSilver, nameof(row.UnitPriceSilver)),
            checked((uint)ParseUlong(row.Amount, nameof(row.Amount))),
            Enum.TryParse<MarketOrderType>(row.AuctionType, out var auctionType) ? auctionType : MarketOrderType.Unknown,
            row.Expires ?? string.Empty,
            ParseUlong(row.DistanceFee, nameof(row.DistanceFee)));

    private static ulong ParseUlong(string? value, string name)
        => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Stored {name} value is not a valid unsigned integer.");

    private static byte ParseByte(string? value, string name)
        => byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"Stored {name} value is not a valid byte.");

    private static DateTimeOffset? TryParseDateTimeOffset(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
}
