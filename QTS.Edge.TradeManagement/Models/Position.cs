namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Repräsentiert die aktuelle Position.
/// </summary>
public sealed record Position
{
    /// <summary>
    /// Aktueller Zustand der Position.
    /// </summary>
    public required PositionState State { get; init; }

    /// <summary>
    /// Richtung: +1 (Long), -1 (Short), 0 (Flat).
    /// </summary>
    public int Direction { get; init; }

    /// <summary>
    /// Anzahl Kontrakte in der Position.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Entry-Preis (MidPrice bei Einstieg).
    /// </summary>
    public decimal EntryPrice { get; init; }

    /// <summary>
    /// Zeitpunkt des Einstiegs.
    /// </summary>
    public DateTime? EntryTime { get; init; }

    /// <summary>
    /// Unrealisierter PnL in Ticks (pro Kontrakt).
    /// </summary>
    public decimal UnrealizedPnLTicks { get; init; }

    /// <summary>
    /// Unrealisierter PnL in Dollar (gesamt).
    /// </summary>
    public decimal UnrealizedPnLDollars { get; init; }

    /// <summary>
    /// Factory für eine flache Position.
    /// </summary>
    public static Position Flat => new()
    {
        State = PositionState.Flat,
        Direction = 0,
        Quantity = 0,
        EntryPrice = 0m,
        EntryTime = null,
        UnrealizedPnLTicks = 0m,
        UnrealizedPnLDollars = 0m
    };
}
