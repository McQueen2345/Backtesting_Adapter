namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Repräsentiert einen Depth-of-Market Snapshot für Level 1 Daten.
/// </summary>
public interface IDomSnapshot
{
    DateTime Timestamp { get; }
    decimal BidPrice { get; }
    int BidSize { get; }
    decimal AskPrice { get; }
    int AskSize { get; }
    bool IsDataStale { get; }
}
