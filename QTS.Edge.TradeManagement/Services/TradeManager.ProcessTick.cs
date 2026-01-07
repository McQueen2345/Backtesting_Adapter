using QTS.Edge.Core.Interfaces;
using QTS.Edge.TradeManagement.Configuration;
using QTS.Edge.TradeManagement.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Trade Manager - Orchestriert Position Tracking, Risk Management und Trade Execution.
/// PARTIAL CLASS: ProcessTick.cs implementiert die Hauptlogik.
/// </summary>
public sealed partial class TradeManager : ITradeManager
{
    // ============================================================
    // FELDER
    // ============================================================

    private readonly TradeManagementConfig _config;
    private readonly RiskManager _riskManager;
    private readonly PositionTracker _positionTracker;
    private readonly ITradeJournal _tradeJournal;

    // ResetDay 2-Stage State (verwendet in TradeManager.ResetDay.cs)
    private bool _pendingDayReset;
    private decimal _pendingDayStartEquity;

    // ============================================================
    // KONSTRUKTOR
    // ============================================================

    public TradeManager(
        TradeManagementConfig config,
        RiskManager riskManager,
        PositionTracker positionTracker,
        ITradeJournal tradeJournal)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
        _positionTracker = positionTracker ?? throw new ArgumentNullException(nameof(positionTracker));
        _tradeJournal = tradeJournal ?? throw new ArgumentNullException(nameof(tradeJournal));
    }

    // ============================================================
    // PROPERTIES
    // ============================================================

    /// <inheritdoc />
    public Position CurrentPosition => _positionTracker.CurrentPosition;

    /// <inheritdoc />
    public RiskState CurrentRiskState => _riskManager.CurrentState;

    /// <inheritdoc />
    public DailyStatistics TodayStats => _riskManager.GetDailyStats();

    /// <inheritdoc />
    public IReadOnlyList<TradeRecord> TodayTrades => _tradeJournal.GetTodayTrades();

    // ============================================================
    // PROCESS TICK - HAUPTLOGIK
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// RED LIST #1: Reihenfolge EXAKT wie spezifiziert!
    /// 0a. Feed-Error → 0b. Stale → 1. midPrice → 2. Equity → 3. Kill →
    /// 4. Exit → 5. Window → 6. Hold if in Position → 7. Entry Checks → 8. Entry
    /// </remarks>
    public TradeDecision ProcessTick(IEdgeSignal signal, IDomSnapshot dom, decimal currentEquity)
    {
        var timestamp = dom.Timestamp;  // DOM ist Zeitquelle!
        var flags = new TradeDecisionFlags();

        // Pending ResetDay finalisieren wenn Position FLAT
        if (_pendingDayReset && _positionTracker.CurrentPosition.State == PositionState.Flat)
        {
            ExecuteReset(_pendingDayStartEquity);
            _pendingDayReset = false;
        }

        // SoftPause Expiry prüfen
        _riskManager.CheckSoftPauseExpiry(timestamp);

        // ============================================================
        // 0a. FEED-ERROR GUARD (Bid > Ask)
        // ============================================================
        if (dom.BidPrice > dom.AskPrice)
        {
            return Hold(timestamp, 0m, DecisionReasons.FEED_ERROR, flags);
        }

        // ============================================================
        // 0b. STALE GUARD (RED LIST #7: signal.IsDataStale!)
        // ============================================================
        if (signal.IsDataStale)
        {
            flags = flags with { DataStale = true };
            return Hold(timestamp, 0m, DecisionReasons.DATA_STALE, flags);
        }

        // ============================================================
        // 1. MIDPRICE BERECHNUNG (nur bei validem Feed!)
        // ============================================================
        var midPrice = (dom.BidPrice + dom.AskPrice) / 2m;

        // ============================================================
        // 2. EQUITY UPDATE
        // ============================================================
        _riskManager.UpdateEquity(currentEquity);

        // ============================================================
        // 3. KILL SWITCH CHECK
        // ============================================================
        if (_riskManager.CurrentState == RiskState.HardDisabled)
        {
            if (_positionTracker.CurrentPosition.State != PositionState.Flat)
            {
                return ExecuteExit(midPrice, timestamp, DecisionReasons.RISK_KILL_SWITCH, flags);
            }
            return Hold(timestamp, midPrice, DecisionReasons.TRADING_DISABLED, flags);
        }

        // ============================================================
        // 4. EXIT CHECK (wenn in Position)
        // ============================================================
        bool isInPosition = _positionTracker.CurrentPosition.State != PositionState.Flat;

        if (isInPosition)
        {
            var exitResult = _positionTracker.CheckExit(midPrice, timestamp);
            if (exitResult.ShouldExit)
            {
                return ExecuteExit(midPrice, timestamp, exitResult.Reason!, flags);
            }
        }

        // ============================================================
        // 5. WINDOW CHECK
        // ============================================================
        if (!_riskManager.IsWithinTradingWindow(timestamp))
        {
            if (isInPosition)
            {
                return ExecuteExit(midPrice, timestamp, DecisionReasons.WINDOW_CLOSE, flags);
            }
            return Hold(timestamp, midPrice, DecisionReasons.OUTSIDE_WINDOW, flags);
        }

        // ============================================================
        // 6. HOLD WENN IN POSITION
        // ============================================================
        if (isInPosition)
        {
            return Hold(timestamp, midPrice, DecisionReasons.IN_POSITION, flags);
        }

        // ============================================================
        // 7. ENTRY CHECKS (Position ist FLAT)
        // ============================================================

        // 7a. Risk State Check
        if (!_riskManager.IsEntryAllowed)
        {
            return Hold(timestamp, midPrice, _riskManager.GetBlockReason(), flags);
        }

        // 7b. Spread Gate
        bool spreadOk = _riskManager.CheckSpreadGate(dom);
        if (!spreadOk)
        {
            flags = flags with { SpreadGateActive = true };
            return Hold(timestamp, midPrice, DecisionReasons.SPREAD_GATE, flags);
        }

        // 7c. Signal Check
        if (signal.Signal == 0)
        {
            // Setze Flags für Telemetrie (auch wenn NO_SIGNAL primary reason ist)
            bool cooldownActive = _positionTracker.IsInCooldown(timestamp);
            if (cooldownActive)
            {
                flags = flags with { CooldownActive = true };
            }
            return Hold(timestamp, midPrice, DecisionReasons.NO_SIGNAL, flags);
        }

        // 7d. Cooldown Check
        if (_positionTracker.IsInCooldown(timestamp))
        {
            flags = flags with { CooldownActive = true };
            return Hold(timestamp, midPrice, DecisionReasons.COOLDOWN, flags);
        }

        // ============================================================
        // 8. ENTRY!
        // ============================================================
        return ExecuteEntry(signal.Signal, midPrice, timestamp, flags);
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    private TradeDecision ExecuteEntry(int signalDirection, decimal midPrice, DateTime timestamp, TradeDecisionFlags flags)
    {
        bool success = _positionTracker.TryEntry(signalDirection, midPrice, timestamp, _config.ContractSize);

        if (!success)
        {
            // Sollte nicht passieren wenn alle Checks korrekt
            return Hold(timestamp, midPrice, DecisionReasons.NO_SIGNAL, flags);
        }

        var action = signalDirection == 1 ? TradeAction.EnterLong : TradeAction.EnterShort;

        return new TradeDecision
        {
            Action = action,
            Reason = "ENTRY",
            Timestamp = timestamp,
            MidPrice = midPrice,
            Direction = signalDirection,
            Quantity = _config.ContractSize,
            RiskState = _riskManager.CurrentState,
            Flags = flags
        };
    }

    private TradeDecision ExecuteExit(decimal midPrice, DateTime timestamp, string reason, TradeDecisionFlags flags)
    {
        // Trade Record erstellen
        var tradeRecord = _positionTracker.ExecuteExit(midPrice, timestamp, reason);

        // Im Journal aufzeichnen
        _tradeJournal.RecordTrade(tradeRecord);

        // Risk Manager informieren
        _riskManager.RecordTrade(tradeRecord);

        // Daily Loss Check nach Trade
        _riskManager.CheckDailyLossLimit();

        // Cooldown setzen
        bool isEmergency = reason == DecisionReasons.EMERGENCY_STOP;
        _positionTracker.SetCooldown(timestamp, isEmergency);

        return new TradeDecision
        {
            Action = TradeAction.Exit,
            Reason = reason,
            Timestamp = timestamp,
            MidPrice = midPrice,
            Direction = null,
            Quantity = tradeRecord.Quantity,
            RiskState = _riskManager.CurrentState,
            Flags = flags
        };
    }

    private TradeDecision Hold(DateTime timestamp, decimal midPrice, string reason, TradeDecisionFlags flags)
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
}
