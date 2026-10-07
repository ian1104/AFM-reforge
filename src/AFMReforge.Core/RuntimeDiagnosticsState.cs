namespace AFMReforge.Core;

public sealed class RuntimeDiagnosticsState
{
    private static readonly RuntimeGate[] GateOrder =
    [
        RuntimeGate.AppProcess,
        RuntimeGate.HttpUi,
        RuntimeGate.AfmCore,
        RuntimeGate.Receiver,
        RuntimeGate.PacketCapture,
        RuntimeGate.MarketResponse,
        RuntimeGate.Adapter,
        RuntimeGate.Observation,
        RuntimeGate.Persistence,
        RuntimeGate.UiReflection
    ];

    private readonly object _gate = new();
    private readonly Dictionary<RuntimeGate, GateState> _gates = GateOrder.ToDictionary(x => x, _ => new GateState());

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
                _lastError,
                _gates.Select(x => new RuntimeGateSnapshot(x.Key, x.Value.Status, x.Value.LastSuccessAt, x.Value.LastError)).ToArray(),
                GetRuntimeSummaryLocked(),
                GetFirstFailureBoundaryLocked());
        }
    }

    public void MarkAppProcessStarted() => MarkGateSuccess(RuntimeGate.AppProcess);

    public void MarkHttpUiStarted() => MarkGateSuccess(RuntimeGate.HttpUi);

    public void MarkAfmCoreInitialized()
    {
        lock (_gate) _afmCoreStatus = "VERIFIED";
        MarkGateSuccess(RuntimeGate.AfmCore);
    }

    public void MarkReceiverInitialized()
    {
        lock (_gate) _receiverStatus = "VERIFIED";
        MarkGateSuccess(RuntimeGate.Receiver);
    }

    public void MarkMarketResponse(MarketResponseKind responseKind, int recordCount, DateTimeOffset? capturedAt)
    {
        lock (_gate)
        {
            _lastResponseKind = responseKind.ToString();
            _lastResponseTime = capturedAt;
            _lastResponseRecordCount = recordCount;
        }

        MarkGateSuccess(RuntimeGate.MarketResponse);
    }

    public void MarkAdapterConversion(int recordCount)
    {
        lock (_gate) _lastAdapterRecordCount = recordCount;
        MarkGateSuccess(RuntimeGate.Adapter);
    }

    public void MarkObservation(Guid observationId, int recordCount)
    {
        lock (_gate)
        {
            _lastObservationId = observationId;
            _lastObservationRecordCount = recordCount;
        }

        MarkGateSuccess(RuntimeGate.Observation);
    }

    public void MarkPersistence(int recordCount)
    {
        lock (_gate) _lastPersistenceCount = recordCount;
        MarkGateSuccess(RuntimeGate.Persistence);
    }

    public void MarkUiReflection() => MarkGateSuccess(RuntimeGate.UiReflection);

    public void MarkGateSuccess(RuntimeGate gate)
    {
        lock (_gate)
        {
            var state = _gates[gate];
            state.Status = "PASS";
            state.LastSuccessAt = DateTimeOffset.UtcNow;
            state.LastError = null;
        }
    }

    public void MarkGateFailure(RuntimeGate gate, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        lock (_gate)
        {
            var state = _gates[gate];
            state.Status = "FAIL";
            state.LastError = exception.Message;
            _lastError = exception.Message;
        }
    }

    public void MarkError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        lock (_gate) _lastError = exception.Message;
    }

    private string GetRuntimeSummaryLocked()
    {
        if (_gates.Values.Any(x => x.Status == "FAIL"))
            return "FAIL";
        if (_gates.Values.All(x => x.Status == "PASS"))
            return "PASS";
        return "PENDING VALIDATION";
    }

    private string? GetFirstFailureBoundaryLocked()
    {
        var failure = GateOrder.FirstOrDefault(x => _gates[x].Status == "FAIL");
        return _gates[failure].Status == "FAIL" ? failure.ToString() : null;
    }

    private sealed class GateState
    {
        public string Status { get; set; } = "UNKNOWN";
        public DateTimeOffset? LastSuccessAt { get; set; }
        public string? LastError { get; set; }
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
    string? LastError,
    IReadOnlyList<RuntimeGateSnapshot> Gates,
    string RuntimeSummary,
    string? FirstFailureBoundary);
