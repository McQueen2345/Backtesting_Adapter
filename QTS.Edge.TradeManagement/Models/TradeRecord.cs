namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Record eines abgeschlossenen Trades (Round-Turn).
/// </summary>
public sealed record TradeRecord
{
    /// <summary>
    /// Zeitpunkt des Einstiegs.
    /// </summary>
    public required DateTime EntryTime { get; init; }

    /// <summary>
    /// Zeitpunkt des Ausstiegs.
    /// </summary>
    public required DateTime ExitTime { get; init; }

    /// <summary>
    /// Entry-Preis (MidPrice).
    /// </summary>
    public required decimal EntryPrice { get; init; }

    /// <summary>
    /// Exit-Preis (MidPrice).
    /// </summary>
    public required decimal ExitPrice { get; init; }

    /// <summary>
    /// Richtung: +1 (Long), -1 (Short).
    /// </summary>
    public required int Direction { get; init; }

    /// <summary>
    /// Anzahl Kontrakte.
    /// </summary>
    public required int Quantity { get; init; }

    /// <summary>
    /// PnL in Ticks (pro Kontrakt).
    /// </summary>
    public required decimal PnLTicks { get; init; }

    /// <summary>
    /// Brutto-PnL in Dollar (vor Fees).
    /// </summary>
    public required decimal GrossPnLDollars { get; init; }

    /// <summary>
    /// Fees/Commission in Dollar.
    /// </summary>
    public required decimal FeesDollars { get; init; }

    /// <summary>
    /// Netto-PnL in Dollar (nach Fees) - RISK BASIS!
    /// </summary>
    public required decimal NetPnLDollars { get; init; }

    /// <summary>
    /// Grund für den Exit (aus DecisionReasons).
    /// </summary>
    public required string ExitReason { get; init; }

    /// <summary>
    /// Dauer der Position.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// True wenn NetPnLDollars > 0.
    /// </summary>
    public bool IsWin => NetPnLDollars > 0;
}
