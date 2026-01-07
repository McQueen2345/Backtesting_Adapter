using QTS.Backtest.Contracts.Interfaces;
using QTS.Backtest.Contracts.Models;
using QTS.Edge.Core;
using QTS.Edge.Core.Interfaces;
using QTS.Edge.TradeManagement.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Backtest.Harness;

/// <summary>
/// Verbindet den Production-Algo (StructImbL1Edge + TradeManagement) 
/// mit der Backtest-Engine.
/// </summary>
public class StructImbBacktestHarness : IBacktestableAlgo
{
    private readonly StructImbL1Edge _edge;
    private readonly ITradeManager _tradeManager;
    private decimal _currentEquity;
    
    public StructImbBacktestHarness(StructImbL1Edge edge, ITradeManager tradeManager, decimal startEquity)
    {
        _edge = edge;
        _tradeManager = tradeManager;
        _currentEquity = startEquity;
    }
    
    public void Initialize(AlgoConfig config)
    {
        // Reset Edge
        _edge.Reset();
    }
    
    public void OnMarketOpen(DateTime timestamp)
    {
        // TODO: TradeManager.ResetDay mit neuem Tag
    }
    
    public OrderRequest? OnSnapshot(IDomSnapshot snapshot)
    {
        // 1. Edge verarbeitet Snapshot → Signal
        var signal = _edge.ProcessSnapshot(snapshot);
        
        // 2. TradeManager verarbeitet Signal → Decision
        var decision = _tradeManager.ProcessTick(signal, snapshot, _currentEquity);
        
        // 3. Decision → OrderRequest für Engine
        return decision.Action switch
        {
            TradeAction.EnterLong => new OrderRequest 
            { 
                Type = Contracts.Enums.OrderType.Buy, 
                Size = decision.Quantity,
                Reason = decision.Reason
            },
            TradeAction.EnterShort => new OrderRequest 
            { 
                Type = Contracts.Enums.OrderType.Sell, 
                Size = decision.Quantity,
                Reason = decision.Reason
            },
            TradeAction.Exit => new OrderRequest 
            { 
                Type = Contracts.Enums.OrderType.Flat,
                Reason = decision.Reason
            },
            TradeAction.Hold => null,
            _ => null
        };
    }
    
    public void OnFill(Fill fill)
    {
        // TODO: Equity-Update, TradeManager-Notification
    }
    
    public void OnMarketClose(DateTime timestamp)
    {
        // TODO: ForceExit für offene Positionen
    }
    
    public void Cleanup()
    {
        _edge.Reset();
    }
}
