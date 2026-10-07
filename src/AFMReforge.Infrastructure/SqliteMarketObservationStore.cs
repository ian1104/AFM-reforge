using System.Globalization;
using System.Text.Json;
using AFMReforge.Core;
using Microsoft.Data.Sqlite;

namespace AFMReforge.Infrastructure;

/// <summary>
/// Runtime-independent local SQLite persistence for the current Reforge-owned input.
/// One row represents one input order plus the response-level context needed to
/// reconstruct what was stored. It is not the final MarketObservation model.
/// </summary>
public sealed class SqliteMarketObservationStore : IMarketObservationStore, IMarketObservationQuery
{
    private readonly string _connectionString;
    private readonly RuntimeDiagnosticsState? _diagnostics;

    public SqliteMarketObservationStore(string databasePath, RuntimeDiagnosticsState? diagnostics = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _diagnostics = diagnostics;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        Initialize();
    }

    public void Persist(MarketObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            using (var observationCommand = connection.CreateCommand())
            {
                observationCommand.Transaction = transaction;
                observationCommand.CommandText = """
                    INSERT INTO market_observations (
                        ObservationId,
                        CapturedAt,
                        ResponseKind
                    )
                    VALUES (
                        $observationId,
                        $capturedAt,
                        $responseKind
                    );
                    """;
                observationCommand.Parameters.AddWithValue("$observationId", observation.ObservationId.ToString("D"));
                observationCommand.Parameters.AddWithValue("$capturedAt",
                    observation.CapturedAt?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
                observationCommand.Parameters.AddWithValue("$responseKind", observation.ResponseKind.ToString());
                observationCommand.ExecuteNonQuery();
            }

            foreach (var order in observation.Records)
            {
                InsertOrder(connection, transaction, observation.ObservationId, observation.ResponseType,
                    observation.ResponseKind, observation.OperationCode, observation.CapturedAt, order);
            }

            transaction.Commit();
            _diagnostics?.MarkPersistence(observation.Records.Count);
        }
        catch (Exception exception)
        {
            _diagnostics?.MarkGateFailure(RuntimeGate.Persistence, exception);
            throw;
        }
    }

    public void Persist(MarketObservationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Records.Count == 0)
            return;

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var order in input.Records)
        {
            InsertOrder(connection, transaction, null, input.ResponseType, input.ResponseKind,
                input.OperationCode, input.CapturedAt, order);
        }

        transaction.Commit();
    }

    private static void InsertOrder(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid? observationId,
        string responseType,
        MarketResponseKind responseKind,
        object? operationCode,
        DateTimeOffset? capturedAt,
        MarketRecord order)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO market_observation_records (
                ObservationId,
                ResponseType,
                ResponseKind,
                OperationCode,
                CapturedAt,
                OrderId,
                ItemTypeId,
                ItemGroupTypeId,
                LocationId,
                QualityLevel,
                EnchantmentLevel,
                UnitPriceSilver,
                Amount,
                AuctionType,
                Expires,
                DistanceFee,
                ResolvedLocation
            )
            VALUES (
                $observationId,
                $responseType,
                $responseKind,
                $operationCode,
                $capturedAt,
                $orderId,
                $itemTypeId,
                $itemGroupTypeId,
                $locationId,
                $qualityLevel,
                $enchantmentLevel,
                $unitPriceSilver,
                $amount,
                $auctionType,
                $expires,
                $distanceFee,
                $resolvedLocation
            );
            """;

        command.Parameters.AddWithValue("$observationId",
            observationId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$responseType", responseType);
        command.Parameters.AddWithValue("$responseKind", responseKind.ToString());
        command.Parameters.AddWithValue("$operationCode", SerializeValue(operationCode));
        command.Parameters.AddWithValue("$capturedAt",
            capturedAt?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$orderId", SerializeValue(order.OrderId));
        command.Parameters.AddWithValue("$itemTypeId", SerializeValue(order.ItemTypeId));
        command.Parameters.AddWithValue("$itemGroupTypeId", SerializeValue(order.ItemGroupTypeId));
        command.Parameters.AddWithValue("$locationId", SerializeValue(order.LocationId));
        command.Parameters.AddWithValue("$qualityLevel", SerializeValue(order.QualityLevel));
        command.Parameters.AddWithValue("$enchantmentLevel", SerializeValue(order.EnchantmentLevel));
        command.Parameters.AddWithValue("$unitPriceSilver", SerializeValue(order.UnitPriceSilver));
        command.Parameters.AddWithValue("$amount", SerializeValue(order.Amount));
        command.Parameters.AddWithValue("$auctionType", SerializeValue(order.AuctionType.ToString()));
        command.Parameters.AddWithValue("$expires", SerializeValue(order.Expires));
        command.Parameters.AddWithValue("$distanceFee", SerializeValue(order.DistanceFee));
        command.Parameters.AddWithValue("$resolvedLocation", (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<MarketRecordView> Query(MarketRecordQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(query.Limit), "Limit must be greater than zero.");
        if (query.Offset < 0)
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Offset cannot be negative.");
        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            throw new ArgumentException("From must be earlier than or equal to To.");

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();

        var predicates = new List<string>();

        if (query.ObservationId.HasValue)
        {
            predicates.Add("ObservationId = $observationId");
            command.Parameters.AddWithValue("$observationId", query.ObservationId.Value.ToString("D"));
        }

        if (query.ItemTypeId is not null)
        {
            predicates.Add("ItemTypeId = $itemTypeId");
            command.Parameters.AddWithValue("$itemTypeId", SerializeValue(query.ItemTypeId));
        }

        if (query.LocationId is not null)
        {
            predicates.Add("LocationId = $locationId");
            command.Parameters.AddWithValue("$locationId", SerializeValue(query.LocationId));
        }

        if (query.QualityLevel is not null)
        {
            predicates.Add("QualityLevel = $qualityLevel");
            command.Parameters.AddWithValue("$qualityLevel", SerializeValue(query.QualityLevel));
        }

        if (query.EnchantmentLevel is not null)
        {
            predicates.Add("EnchantmentLevel = $enchantmentLevel");
            command.Parameters.AddWithValue("$enchantmentLevel", SerializeValue(query.EnchantmentLevel));
        }

        if (query.ResponseKind.HasValue)
        {
            predicates.Add("ResponseKind = $responseKind");
            command.Parameters.AddWithValue("$responseKind", query.ResponseKind.Value.ToString());
        }

        if (query.From.HasValue)
        {
            predicates.Add("CapturedAt >= $from");
            command.Parameters.AddWithValue("$from", query.From.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        if (query.To.HasValue)
        {
            predicates.Add("CapturedAt <= $to");
            command.Parameters.AddWithValue("$to", query.To.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        command.CommandText = $"""
            SELECT
                StorageRecordId,
                ResponseType,
                ResponseKind,
                OperationCode,
                CapturedAt,
                OrderId,
                ItemTypeId,
                ItemGroupTypeId,
                LocationId,
                QualityLevel,
                EnchantmentLevel,
                UnitPriceSilver,
                Amount,
                AuctionType,
                Expires,
                DistanceFee,
                ResolvedLocation,
                ObservationId
            FROM market_observation_records
            {(predicates.Count == 0 ? "" : "WHERE " + string.Join(" AND ", predicates))}
            ORDER BY StorageRecordId DESC
            LIMIT $limit OFFSET $offset;
            """;

        command.Parameters.AddWithValue("$limit", query.Limit);
        command.Parameters.AddWithValue("$offset", query.Offset);

        using var reader = command.ExecuteReader();
        var records = new List<MarketRecordView>();

        while (reader.Read())
        {
            records.Add(new MarketRecordView(
                reader.GetInt64(0),
                ReadNullableString(reader, 1),
                ReadNullableString(reader, 2),
                ReadNullableString(reader, 3),
                ReadNullableString(reader, 4),
                ReadNullableString(reader, 5),
                ReadNullableString(reader, 6),
                ReadNullableString(reader, 7),
                ReadNullableString(reader, 8),
                ReadNullableString(reader, 9),
                ReadNullableString(reader, 10),
                ReadNullableString(reader, 11),
                ReadNullableString(reader, 12),
                ReadNullableString(reader, 13),
                ReadNullableString(reader, 14),
                ReadNullableString(reader, 15),
                ReadNullableString(reader, 16),
                ReadNullableGuid(reader, 17)));
        }

        return records;
    }

    public IReadOnlyList<MarketObservationRecordView> ReadObservations()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ObservationId, CapturedAt, ResponseKind
            FROM market_observations
            ORDER BY CapturedAt DESC, rowid DESC;
            """;

        using var reader = command.ExecuteReader();
        var rows = new List<MarketObservationRecordView>();
        while (reader.Read())
        {
            rows.Add(new MarketObservationRecordView(
                Guid.Parse(reader.GetString(0)),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                Enum.TryParse<MarketResponseKind>(reader.GetString(2), out var kind) ? kind : MarketResponseKind.Unknown));
        }

        return rows;
    }

    public IReadOnlyList<MarketRecordView> ReadAll() => Query(new MarketRecordQuery(Limit: 5000));

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS market_observations (
                ObservationId TEXT PRIMARY KEY,
                CapturedAt TEXT NULL,
                ResponseKind TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS market_observation_records (
                StorageRecordId INTEGER PRIMARY KEY AUTOINCREMENT,
                ObservationId TEXT NULL,
                ResponseType TEXT NOT NULL,
                ResponseKind TEXT NOT NULL,
                OperationCode TEXT NULL,
                CapturedAt TEXT NULL,
                OrderId TEXT NOT NULL,
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
            """;
        command.ExecuteNonQuery();

        EnsureObservationIdColumn(connection);
    }

    private static void EnsureObservationIdColumn(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(market_observation_records);";
        using var reader = command.ExecuteReader();
        var hasObservationId = false;
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "ObservationId", StringComparison.OrdinalIgnoreCase))
            {
                hasObservationId = true;
                break;
            }
        }

        if (hasObservationId)
            return;

        using var alter = connection.CreateCommand();
        alter.CommandText = "ALTER TABLE market_observation_records ADD COLUMN ObservationId TEXT NULL;";
        alter.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string? SerializeValue(object? value)
        => value switch
        {
            null => null,
            string text => text,
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString();

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Guid.TryParse(reader.GetString(ordinal), out var id) ? id : null;
}

public sealed record MarketObservationRecordView(
    Guid ObservationId,
    string? CapturedAt,
    MarketResponseKind ResponseKind);

public sealed record MarketRecordView(
    long StorageRecordId,
    string? ResponseType,
    string? ResponseKind,
    string? OperationCode,
    string? CapturedAt,
    string? OrderId,
    string? ItemTypeId,
    string? ItemGroupTypeId,
    string? LocationId,
    string? QualityLevel,
    string? EnchantmentLevel,
    string? UnitPriceSilver,
    string? Amount,
    string? AuctionType,
    string? Expires,
    string? DistanceFee,
    string? ResolvedLocation,
    Guid? ObservationId);
