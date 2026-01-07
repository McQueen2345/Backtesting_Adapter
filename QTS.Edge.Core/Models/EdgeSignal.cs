using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Models;

/// <summary>
/// Immutable Record für ein generiertes Trading-Signal.
/// </summary>
public sealed record EdgeSignal(
    DateTime Timestamp,
    double StructImbRaw,
    double StructImbZ,
    int Signal,
    bool IsContextWarm,
    bool IsDataStale,
    bool IsQualityGatePassed
) : IEdgeSignal;
