using QTS.Edge.Core.Interfaces;
using QTS.Backtest.Contracts.Models;

namespace QTS.Backtest.Contracts.Interfaces;

/// <summary>
/// Interface das ein Algo implementieren muss um backtest-fähig zu sein.
/// Der Production-Algo (StructImbL1Edge + TradeManagement) wird über
/// einen Wrapper (Harness) angebunden.
/// </summary>
public interface IBacktestableAlgo
{
    /// <summary>
    /// Initialisiert den Algo zu Beginn des Backtests.
    /// </summary>
    void Initialize(AlgoConfig config);
    
    /// <summary>
    /// Wird beim Session-Start (Market Open) aufgerufen.
    /// </summary>
    /// <param name="timestamp">UTC Timestamp des Market Open.</param>
    void OnMarketOpen(DateTime timestamp);
    
    /// <summary>
    /// Verarbeitet einen DOM-Snapshot und gibt optional einen Order-Request zurück.
    /// </summary>
    /// <param name="snapshot">DOM-Snapshot vom Adapter (UTC Timestamp!).</param>
    /// <returns>OrderRequest oder null für "Hold".</returns>
    OrderRequest? OnSnapshot(IDomSnapshot snapshot);
    
    /// <summary>
    /// Wird nach Fill-Execution aufgerufen.
    /// </summary>
    /// <param name="fill">Ausgeführter Fill.</param>
    void OnFill(Fill fill);
    
    /// <summary>
    /// Wird beim Session-Ende (Market Close) aufgerufen.
    /// </summary>
    /// <param name="timestamp">UTC Timestamp des LETZTEN In-Session Snapshots!</param>
    void OnMarketClose(DateTime timestamp);
    
    /// <summary>
    /// Cleanup am Ende des Backtests.
    /// </summary>
    void Cleanup();
}

/// <summary>
/// Konfiguration für den Algo im Backtest.
/// </summary>
public class AlgoConfig
{
    // Placeholder - kann erweitert werden für algo-spezifische Settings
}
