using QTS.Backtest.Contracts.Enums;

namespace QTS.Backtest.Contracts.Models;

/// <summary>
/// Abgeschlossener Trade (Entry + Exit).
/// </summary>
public class TradeRecord
{
    public required int TradeId { get; init; }
    public required Direction Direction { get; init; }
    public required int Size { get; init; }
    
    // Entry
    public required DateTime EntryTime { get; init; }
    public required decimal EntryPrice { get; init; }
    public required long EntryFillSeq { get; init; }
    
    // Exit
    public required DateTime ExitTime { get; init; }
    public required decimal ExitPrice { get; init; }
    public required long ExitFillSeq { get; init; }
    
    // PnL (klare Einheiten!)
    /// <summary>
    /// Brutto-PnL in Ticks pro Contract.
    /// </summary>
    public required decimal GrossPnlTicksPerContract { get; init; }
    
    /// <summary>
    /// Brutto-PnL in Dollar (Total für alle Contracts).
    /// </summary>
    public required decimal GrossPnlDollarsTotal { get; init; }
    
    /// <summary>
    /// Commission (Round-Trip, nur bei Exit berechnet).
    /// </summary>
    public required decimal Commission { get; init; }
    
    /// <summary>
    /// Netto-PnL in Dollar (Total für alle Contracts).
    /// </summary>
    public required decimal NetPnlDollarsTotal { get; init; }
    
    /// <summary>
    /// Netto-PnL in Ticks pro Contract.
    /// </summary>
    public required decimal NetPnlTicksPerContract { get; init; }
    
    /// <summary>
    /// Trade-Dauer.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
