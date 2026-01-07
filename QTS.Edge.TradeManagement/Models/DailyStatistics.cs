namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Tages-Statistiken für Risk Management.
/// </summary>
public sealed record DailyStatistics
{
    /// <summary>
    /// Anzahl abgeschlossener Trades (Round-Turns).
    /// </summary>
    public required int TradeCount { get; init; }

    /// <summary>
    /// Anzahl Gewinntrades (NetPnL > 0).
    /// </summary>
    public required int WinCount { get; init; }

    /// <summary>
    /// Anzahl Verlusttrades (NetPnL <= 0).
    /// </summary>
    public required int LossCount { get; init; }

    /// <summary>
    /// Aktuelle Anzahl aufeinanderfolgender Verluste.
    /// </summary>
    public required int ConsecutiveLosses { get; init; }

    /// <summary>
    /// Brutto-PnL in Dollar (vor Fees).
    /// </summary>
    public required decimal GrossPnLDollars { get; init; }

    /// <summary>
    /// Summe aller Fees in Dollar.
    /// </summary>
    public required decimal TotalFeesDollars { get; init; }

    /// <summary>
    /// Netto-PnL in Dollar (nach Fees) - RISK BASIS!
    /// </summary>
    public required decimal NetPnLDollars { get; init; }

    /// <summary>
    /// Höchster Equity-Stand heute (für Drawdown-Berechnung).
    /// </summary>
    public required decimal EquityPeakToday { get; init; }

    /// <summary>
    /// Aktueller Drawdown (Peak - Current Equity).
    /// </summary>
    public required decimal CurrentDrawdown { get; init; }

    /// <summary>
    /// Win Rate (0.0 - 1.0). Gibt 0 zurück wenn TradeCount == 0.
    /// </summary>
    public double WinRate => TradeCount > 0 ? (double)WinCount / TradeCount : 0.0;

    /// <summary>
    /// Factory für leere Tages-Statistiken.
    /// </summary>
    public static DailyStatistics Empty(decimal dayStartEquity) => new()
    {
        TradeCount = 0,
        WinCount = 0,
        LossCount = 0,
        ConsecutiveLosses = 0,
        GrossPnLDollars = 0m,
        TotalFeesDollars = 0m,
        NetPnLDollars = 0m,
        EquityPeakToday = dayStartEquity,
        CurrentDrawdown = 0m
    };
}
