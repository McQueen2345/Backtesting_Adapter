using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Models;

/// <summary>
/// Immutable Record für einen DOM-Snapshot.
/// </summary>
public sealed record DomSnapshot(
    DateTime Timestamp,
    decimal BidPrice,
    int BidSize,
    decimal AskPrice,
    int AskSize,
    bool IsDataStale = false
) : IDomSnapshot;
