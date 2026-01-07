using QTS.Backtest.Contracts.Enums;

namespace QTS.Backtest.Contracts.Models;

/// <summary>
/// Ausgeführter Fill.
/// WICHTIG: FillSeq ist ein long-Zähler, KEIN Guid!
/// </summary>
public class Fill
{
    /// <summary>
    /// Deterministische Sequenz-Nummer (1, 2, 3, ...).
    /// KEIN Guid.NewGuid()!
    /// </summary>
    public required long FillSeq { get; init; }
    
    /// <summary>
    /// Order-Typ der ausgeführten Order.
    /// </summary>
    public required OrderType OrderType { get; init; }
    
    /// <summary>
    /// Ausführungspreis (inkl. Slippage).
    /// </summary>
    public required decimal Price { get; init; }
    
    /// <summary>
    /// Anzahl Kontrakte.
    /// </summary>
    public required int Size { get; init; }
    
    /// <summary>
    /// UTC Timestamp der Ausführung.
    /// </summary>
    public required DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Angewendete Slippage in Ticks.
    /// </summary>
    public int SlippageTicks { get; init; }
    
    /// <summary>
    /// Ist dies ein Entry-Fill?
    /// </summary>
    public bool IsEntry { get; set; }
    
    /// <summary>
    /// Ist dies ein Exit-Fill?
    /// </summary>
    public bool IsExit { get; set; }
}
