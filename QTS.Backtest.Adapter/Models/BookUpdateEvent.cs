using QTS.Backtest.Adapter.Enums;

namespace QTS.Backtest.Adapter.Models;

/// <summary>
/// Represents a single order book update event parsed from TTD data.
/// </summary>
public class BookUpdateEvent
{
    /// <summary>
    /// Timestamp of the event. MUST be UTC!
    /// Validation enforced in parser.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Row index from source file for stable sorting when timestamps are equal.
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// Raw mdt value for diagnostics (66='B', 79='O', 65='A').
    /// </summary>
    public int Mdt { get; set; }

    /// <summary>
    /// Canonical side: Bid or Ask.
    /// Mapped from mdt: 66='B'→Bid, 79='O'→Ask, 65='A'→Ask
    /// </summary>
    public Side Side { get; set; }

    /// <summary>
    /// Depth level in the order book.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Operation type: Add, Update, or Delete.
    /// </summary>
    public Operation Operation { get; set; }

    /// <summary>
    /// Price level.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Size/quantity at this price level.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Optional level value. Nullable for missing level data.
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// RED LIST: Determines if this event should remove the level from the book.
    /// Rule: Delete OR Size≤0 → Level entfernen
    /// </summary>
    public bool ShouldRemoveLevel => Operation == Operation.Delete || Size <= 0;
}
