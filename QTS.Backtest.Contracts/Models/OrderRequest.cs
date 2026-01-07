using QTS.Backtest.Contracts.Enums;

namespace QTS.Backtest.Contracts.Models;

/// <summary>
/// Order-Anfrage vom Algo an die Engine.
/// </summary>
public class OrderRequest
{
    /// <summary>
    /// Order-Typ: Buy, Sell, oder Flat.
    /// </summary>
    public required OrderType Type { get; init; }
    
    /// <summary>
    /// Anzahl Kontrakte (>= 1, bei Flat ignoriert).
    /// </summary>
    public int Size { get; init; } = 1;
    
    /// <summary>
    /// Timestamp - wird von der ENGINE gesetzt, nicht vom Algo!
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Optionaler Grund für die Order (für Logging/Debugging).
    /// </summary>
    public string? Reason { get; init; }
}
