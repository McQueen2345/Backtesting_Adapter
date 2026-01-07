using QTS.Edge.Core.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Trade Manager - ResetDay 2-Stage und ForceExit.
/// PARTIAL CLASS: ResetDay.cs implementiert Tag-Reset und erzwungenen Exit.
/// </summary>
public sealed partial class TradeManager
{
    // ============================================================
    // RESET DAY - 2-STAGE (RED LIST #4!)
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// 2-Stage Reset:
    /// - Stage 1: Position offen → PendingDayReset=true, return Exit(DAY_RESET)
    /// - Stage 2: Position FLAT → ExecuteReset(), return Hold(DAY_RESET_COMPLETE)
    /// </remarks>
    public TradeDecision ResetDay(decimal dayStartEquity, IDomSnapshot dom)
    {
        var timestamp = dom.Timestamp;
        var midPrice = CalculateMidPriceSafe(dom);
        var flags = new TradeDecisionFlags();

        // Stage 1: Wenn Position offen → nur Exit, kein Reset!
        if (_positionTracker.CurrentPosition.State != PositionState.Flat)
        {
            _pendingDayReset = true;
            _pendingDayStartEquity = dayStartEquity;

            // Exit durchführen
            return ExecuteExit(midPrice, timestamp, DecisionReasons.DAY_RESET, flags);
        }

        // Stage 2: Position FLAT → jetzt Reset ausführen
        ExecuteReset(dayStartEquity);

        return new TradeDecision
        {
            Action = TradeAction.Hold,
            Reason = DecisionReasons.DAY_RESET_COMPLETE,
            Timestamp = timestamp,
            MidPrice = midPrice,
            Direction = null,
            Quantity = 0,
            RiskState = _riskManager.CurrentState,
            Flags = flags
        };
    }

    // ============================================================
    // FORCE EXIT
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// Erzwingt Exit mit beliebigem Grund (z.B. ADAPTER_DISCONNECT).
    /// Gibt Hold zurück wenn keine Position offen.
    /// </remarks>
    public TradeDecision ForceExit(IDomSnapshot dom, string reason)
    {
        var timestamp = dom.Timestamp;
        var midPrice = CalculateMidPriceSafe(dom);
        var flags = new TradeDecisionFlags();

        // Keine Position offen?
        if (_positionTracker.CurrentPosition.State == PositionState.Flat)
        {
            return new TradeDecision
            {
                Action = TradeAction.Hold,
                Reason = reason,
                Timestamp = timestamp,
                MidPrice = midPrice,
                Direction = null,
                Quantity = 0,
                RiskState = _riskManager.CurrentState,
                Flags = flags
            };
        }

        // Exit durchführen
        return ExecuteExit(midPrice, timestamp, reason, flags);
    }

    // ============================================================
    // INTERNAL RESET HELPER
    // ============================================================

    /// <summary>
    /// Führt den eigentlichen Reset durch.
    /// Wird von ResetDay (Stage 2) und ProcessTick (Pending Reset) aufgerufen.
    /// </summary>
    private void ExecuteReset(decimal dayStartEquity)
    {
        _riskManager.ResetDay(dayStartEquity);
        _positionTracker.Reset();
        _tradeJournal.Clear();
        _pendingDayReset = false;
    }

    // ============================================================
    // SAFE MIDPRICE HELPER
    // ============================================================

    /// <summary>
    /// Berechnet MidPrice, auch bei ungültigem Spread (für ResetDay/ForceExit).
    /// Bei Bid > Ask wird 0 zurückgegeben.
    /// </summary>
    private static decimal CalculateMidPriceSafe(IDomSnapshot dom)
    {
        if (dom.BidPrice > dom.AskPrice)
            return 0m;

        return (dom.BidPrice + dom.AskPrice) / 2m;
    }
}
