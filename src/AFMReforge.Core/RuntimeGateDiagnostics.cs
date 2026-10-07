namespace AFMReforge.Core;

public enum RuntimeGate
{
    AppProcess,
    HttpUi,
    AfmCore,
    Receiver,
    PacketCapture,
    MarketResponse,
    Adapter,
    Observation,
    Persistence,
    UiReflection
}

public sealed record RuntimeGateSnapshot(
    RuntimeGate Gate,
    string Status,
    DateTimeOffset? LastSuccessAt,
    string? LastError);
