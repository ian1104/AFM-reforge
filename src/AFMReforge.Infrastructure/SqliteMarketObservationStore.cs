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
                reader.GetString(1),
                Enum.Parse<MarketResponseKind>(reader.GetString(2)),
                ReadNullableString(reader, 3),
                ReadNullableDateTimeOffset(reader, 4),
                UnserializeString(reader, 5),
                UnserializeString(reader, 6),
                UnserializeString(reader, 7),
                UnserializeString(reader, 8),
                UnserializeString(reader, 9),
                UnserializeString(reader, 10),
                UnserializeString(reader, 11),
                UnserializeString(reader, 12),
                UnserializeString(reader, 13),
                UnserializeString(reader, 14),
                UnserializeString(reader, 15),
                UnserializeString(reader, 16),
                ReadNullableGuid(reader, 17)));
        }

        return records;
    }

    public IReadOnlyList<StoredMarketObservation> ReadObservations()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ObservationId, CapturedAt, ResponseKind
            FROM market_observations
            ORDER BY COALESCE(CapturedAt, '' ) DESC, ObservationId DESC;
            """;

        using var reader = command.ExecuteReader();
        var observations = new List<StoredMarketObservation>();
        while (reader.Read())
        {
            observations.Add(new StoredMarketObservation(
                Guid.Parse(reader.GetString(0)),
                ReadNullableDateTimeOffset(reader, 1),
                Enum.Parse<MarketResponseKind>(reader.GetString(2))));
        }

        return observations;
    }

    public IReadOnlyList<StoredMarketObservationRecord> ReadAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                StorageRecordId,
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
            FROM market_observation_records
            ORDER BY StorageRecordId;
            """;

        using var reader = command.ExecuteReader();
        var records = new List<StoredMarketObservationRecord>();

        while (reader.Read())
        {
            records.Add(new StoredMarketObservationRecord(
                reader.GetInt64(0),
                ReadNullableGuid(reader, 1),
                reader.GetString(2),
                reader.GetString(3),
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
                ReadNullableString(reader, 17)));
        }

        return records;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

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
            """;
        command.ExecuteNonQuery();

        EnsureColumnExists(connection, "market_observation_records", "ObservationId", "TEXT NULL");
    }

    private static void EnsureColumnExists(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = check.ExecuteReader();

        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        reader.Close();

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alter.ExecuteNonQuery();
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal)
    {
        var value = ReadNullableString(reader, ordinal);
        return value is null ? null : Guid.Parse(value);
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqliteDataReader reader, int ordinal)
    {
        var value = ReadNullableString(reader, ordinal);
        return value is null ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static string? UnserializeString(SqliteDataReader reader, int ordinal)
    {
        var value = ReadNullableString(reader, ordinal);
        if (value is null || value == "null")
            return null;

        using var document = JsonDocument.Parse(value);
        return document.RootElement.ValueKind == JsonValueKind.String
            ? document.RootElement.GetString()
            : value;
    }

    internal static string SerializeValue(object? value)
        => value is null ? "null" : JsonSerializer.Serialize(value, value.GetType());
}

public sealed record StoredMarketObservationRecord(
    long StorageRecordId,
    Guid? ObservationId,
    string ResponseType,
    string ResponseKind,
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
    string? ResolvedLocation);

public sealed record StoredMarketObservation(
    Guid ObservationId,
    DateTimeOffset? CapturedAt,
    MarketResponseKind ResponseKind);
