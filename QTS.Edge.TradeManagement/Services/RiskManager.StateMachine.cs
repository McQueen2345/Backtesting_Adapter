using QTS.Edge.Core.Interfaces;
using QTS.Edge.TradeManagement.Configuration;
using QTS.Edge.TradeManagement.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Risk Manager - verwaltet Risk State, Gates und Daily Loss Limits.
/// PARTIAL CLASS: StateMachine.cs definiert Felder und State-Logik.
/// </summary>
public sealed partial class RiskManager : IRiskManager
{
    // ============================================================
    // FELDER (verwendet in allen Partial Classes)
    // ============================================================

    private readonly TradeManagementConfig _config;

    // Risk State
    private RiskState _currentState = RiskState.Normal;
    private DateTime? _softPauseEndTime;

    // Equity Tracking (verwendet in RiskManager.Equity.cs)
    private decimal _dayStartEquity;
    private decimal _equityPeak;
    private decimal _currentEquity;

    // Trade Tracking (verwendet in RiskManager.DailyLoss.cs)
    private int _consecutiveLosses;
    private readonly List<TradeRecord> _todayTrades = new();

    // ============================================================
    // KONSTRUKTOR
    // ============================================================

    public RiskManager(TradeManagementConfig config, decimal dayStartEquity)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate(); // Fail-fast

        _dayStartEquity = dayStartEquity;
        _equityPeak = dayStartEquity;
        _currentEquity = dayStartEquity;
    }

    // ============================================================
    // STATE MACHINE PROPERTIES
    // ============================================================

    /// <inheritdoc />
    public RiskState CurrentState
    {
        get
        {
            // SoftPause Auto-Recovery prüfen
            if (_currentState == RiskState.SoftPaused && _softPauseEndTime.HasValue)
            {
                // HINWEIS: Wir prüfen gegen _softPauseEndTime, aber benötigen
                // aktuelle Zeit von außen. Daher wird dies in IsEntryAllowed geprüft.
            }
            return _currentState;
        }
    }

    /// <inheritdoc />
    public bool IsEntryAllowed
    {
        get
        {
            // HardDisabled: Nie erlaubt
            if (_currentState == RiskState.HardDisabled)
                return false;

            // Normal: Immer erlaubt (Window/Spread werden separat geprüft!)
            if (_currentState == RiskState.Normal)
                return true;

            // SoftPaused: Prüfen ob Timeout abgelaufen
            // HINWEIS: Ohne externe Zeit können wir hier nicht prüfen.
            // Der Aufrufer muss CheckSoftPauseExpiry() aufrufen.
            return false;
        }
    }

    // ============================================================
    // TRADING WINDOW
    // ============================================================

    /// <inheritdoc />
    public bool IsWithinTradingWindow(DateTime utcTime)
    {
        var timeOfDay = utcTime.TimeOfDay;
        return timeOfDay >= _config.TradingWindowStartUtc
            && timeOfDay < _config.TradingWindowEndUtc;
    }

    // ============================================================
    // SPREAD GATE
    // ============================================================

    /// <inheritdoc />
    public bool CheckSpreadGate(IDomSnapshot dom)
    {
        // WICHTIG: Alles in decimal für Präzision!
        // Spread = Ask - Bid (in Ticks)
        decimal spreadTicks = (dom.AskPrice - dom.BidPrice) / _config.TickSize;
        return spreadTicks <= _config.MaxSpreadTicks;
    }

    // ============================================================
    // BLOCK REASON
    // ============================================================

    /// <inheritdoc />
    public string GetBlockReason()
    {
        return _currentState switch
        {
            RiskState.HardDisabled => DecisionReasons.TRADING_DISABLED,
            RiskState.SoftPaused => DecisionReasons.SOFT_PAUSED,
            _ => DecisionReasons.NO_SIGNAL // Sollte nicht erreicht werden
        };
    }

    // ============================================================
    // TRADE RECORDING & STATE TRANSITIONS
    // ============================================================

    /// <inheritdoc />
    public void RecordTrade(TradeRecord trade)
    {
        _todayTrades.Add(trade);

        // Consecutive Losses tracken
        if (trade.IsWin)
        {
            _consecutiveLosses = 0;
        }
        else
        {
            _consecutiveLosses++;

            // SoftPause bei MaxConsecutiveLosses
            if (_consecutiveLosses >= _config.MaxConsecutiveLosses
                && _currentState == RiskState.Normal)
            {
                TriggerSoftPause(trade.ExitTime);
            }
        }
    }

    // ============================================================
    // STATE TRANSITION HELPERS
    // ============================================================

    /// <summary>
    /// Aktiviert SoftPause für konfigurierte Dauer.
    /// </summary>
    private void TriggerSoftPause(DateTime timestamp)
    {
        _currentState = RiskState.SoftPaused;
        _softPauseEndTime = timestamp.AddMinutes(_config.SoftPauseMinutes);
    }

    /// <summary>
    /// Aktiviert HardDisabled (Kill Switch).
    /// </summary>
    private void TriggerHardDisabled()
    {
        _currentState = RiskState.HardDisabled;
        _softPauseEndTime = null;
    }

    /// <summary>
    /// Prüft ob SoftPause abgelaufen ist und setzt State zurück.
    /// Muss mit aktuellem Timestamp aufgerufen werden.
    /// </summary>
    public void CheckSoftPauseExpiry(DateTime currentTime)
    {
        if (_currentState == RiskState.SoftPaused
            && _softPauseEndTime.HasValue
            && currentTime >= _softPauseEndTime.Value)
        {
            _currentState = RiskState.Normal;
            _softPauseEndTime = null;
            // ConsecutiveLosses wird NICHT zurückgesetzt!
        }
    }
}
