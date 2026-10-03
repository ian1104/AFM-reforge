namespace AFMReforge.Core;

public sealed class RuntimeDiagnosticsState
{
    private readonly object _gate = new();

    private string _afmCoreStatus = "UNKNOWN";
    private string _receiverStatus = "UNKNOWN";
    private string _lastResponseKind = "UNKNOWN";
    private DateTimeOffset? _lastResponseTime;
    private int? _lastResponseRecordCount;
    private int? _lastAdapterRecordCount;
    private Guid? _lastObservationId;
    private int? _lastObservationRecordCount;
    private int? _lastPersistenceCount;
    private string? _lastError;

    public RuntimeDiagnosticsSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new RuntimeDiagnosticsSnapshot(
                _afmCoreStatus,
                _receiverStatus,
                _lastResponseKind,
                _lastResponseTime,
                _lastResponseRecordCount,
                _lastAdapterRecordCount,
                _lastObservationId,
                _lastObservationRecordCount,
                _lastPersistenceCount,
                _lastError);
        }
    }

    public void MarkAfmCoreInitialized()
    {
        lock (_gate) _afmCoreStatus = "VERIFIED";
    }

    public void MarkReceiverInitialized()
    {
        lock (_gate) _receiverStatus = "VERIFIED";
    }

    public void MarkMarketResponse(MarketResponseKind responseKind, int recordCount, DateTimeOffset? capturedAt)
    {
        lock (_gate)
        {
            _lastResponseKind = responseKind.ToString();
            _lastResponseTime = capturedAt;
            _lastResponseRecordCount = recordCount;
        }
    }

    public void MarkAdapterConversion(int recordCount)
    {
        lock (_gate) _lastAdapterRecordCount = recordCount;
    }

    public void MarkObservation(Guid observationId, int recordCount)
    {
        lock (_gate)
        {
            _lastObservationId = observationId;
            _lastObservationRecordCount = recordCount;
        }
    }

    public void MarkPersistence(int recordCount)
    {
        lock (_gate) _lastPersistenceCount = recordCount;
    }

    public void MarkError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        lock (_gate) _lastError = exception.Message;
    }
}

public sealed record RuntimeDiagnosticsSnapshot(
    string AfmCoreStatus,
    string ReceiverStatus,
    string LastResponseKind,
    DateTimeOffset? LastResponseTime,
    int? LastResponseRecordCount,
    int? LastAdapterRecordCount,
    Guid? LastObservationId,
    int? LastObservationRecordCount,
    int? LastPersistenceCount,
    string? LastError);
