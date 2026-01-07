namespace QTS.Backtest.Adapter.Enums;

/// <summary>
/// Represents the side of the order book (Bid or Ask).
/// Mapped from TTD mdt field: 66='B'→Bid, 79='O'→Ask, 65='A'→Ask
/// </summary>
public enum Side
{
    Bid,
    Ask
}

/// <summary>
/// Represents the operation type for order book updates.
/// Mapped from TTD operation field.
/// RED LIST: Delete OR Size≤0 → Level entfernen
/// </summary>
public enum Operation
{
    Add = 0,
    Update = 1,
    Delete = 2
}
