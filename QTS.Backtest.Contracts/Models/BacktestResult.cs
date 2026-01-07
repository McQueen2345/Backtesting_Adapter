namespace QTS.Backtest.Contracts.Models;

/// <summary>
/// Ergebnis eines Backtests.
/// </summary>
public class BacktestResult
{
    // Metadata
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public required string ConfigHash { get; init; }
    
    // Trades
    public required List<TradeRecord> Trades { get; set; }
    
    // Summary Statistics
    public int TotalTrades => Trades.Count;
    public int WinningTrades => Trades.Count(t => t.NetPnlDollarsTotal > 0);
    public int LosingTrades => Trades.Count(t => t.NetPnlDollarsTotal < 0);
    public decimal WinRate => TotalTrades > 0 ? (decimal)WinningTrades / TotalTrades : 0m;
    
    public decimal GrossPnlTotal => Trades.Sum(t => t.GrossPnlDollarsTotal);
    public decimal TotalCommission => Trades.Sum(t => t.Commission);
    public decimal NetPnlTotal => Trades.Sum(t => t.NetPnlDollarsTotal);
    
    public decimal AverageWin => WinningTrades > 0 
        ? Trades.Where(t => t.NetPnlDollarsTotal > 0).Average(t => t.NetPnlDollarsTotal) 
        : 0m;
    public decimal AverageLoss => LosingTrades > 0 
        ? Trades.Where(t => t.NetPnlDollarsTotal < 0).Average(t => t.NetPnlDollarsTotal) 
        : 0m;
    
    public decimal ProfitFactor => AverageLoss != 0 
        ? Math.Abs(AverageWin * WinningTrades / (AverageLoss * LosingTrades)) 
        : 0m;
    
    // Fills (für Audit)
    public int TotalFills { get; init; }
    public long LastFillSeq { get; init; }
    
    // Quality
    public required Interfaces.QualityMetrics DataQuality { get; init; }
}
