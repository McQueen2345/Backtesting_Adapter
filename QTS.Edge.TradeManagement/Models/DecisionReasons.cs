namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Konstanten für alle Decision Reasons.
/// Verwendet für Telemetrie und KPIs - exakte String-Werte sind wichtig!
/// </summary>
public static class DecisionReasons
{
    // ============================================================
    // EXIT REASONS - Position wird geschlossen
    // ============================================================

    /// <summary>
    /// HardDisabled State aktiv - Kill Switch.
    /// </summary>
    public const string RISK_KILL_SWITCH = "RISK_KILL_SWITCH";

    /// <summary>
    /// PnL ≤ -10 Ticks pro Kontrakt.
    /// </summary>
    public const string EMERGENCY_STOP = "EMERGENCY_STOP";

    /// <summary>
    /// Position > MaxPositionSeconds (Default: 300s).
    /// </summary>
    public const string TIME_EXIT = "TIME_EXIT";

    /// <summary>
    /// Trading-Fenster endet (21:00 UTC).
    /// </summary>
    public const string WINDOW_CLOSE = "WINDOW_CLOSE";

    /// <summary>
    /// ResetDay() aufgerufen während Position offen.
    /// </summary>
    public const string DAY_RESET = "DAY_RESET";

    /// <summary>
    /// Adapter meldet Disconnect.
    /// </summary>
    public const string ADAPTER_DISCONNECT = "ADAPTER_DISCONNECT";

    // ============================================================
    // HOLD REASONS - Entry blockiert
    // ============================================================

    /// <summary>
    /// RiskState == HardDisabled, aber keine Position offen.
    /// </summary>
    public const string TRADING_DISABLED = "TRADING_DISABLED";

    /// <summary>
    /// Außerhalb Trading-Fenster (vor 14:30 oder nach 21:00 UTC).
    /// </summary>
    public const string OUTSIDE_WINDOW = "OUTSIDE_WINDOW";

    /// <summary>
    /// Bereits in Position - warten auf Exit.
    /// </summary>
    public const string IN_POSITION = "IN_POSITION";

    /// <summary>
    /// RiskState == SoftPaused (5 Consecutive Losses).
    /// </summary>
    public const string SOFT_PAUSED = "SOFT_PAUSED";

    /// <summary>
    /// Spread > MaxSpreadTicks.
    /// </summary>
    public const string SPREAD_GATE = "SPREAD_GATE";

    /// <summary>
    /// Signal == 0, kein Entry-Signal vorhanden.
    /// </summary>
    public const string NO_SIGNAL = "NO_SIGNAL";

    /// <summary>
    /// Feed-Daten sind veraltet (signal.IsDataStale == true).
    /// </summary>
    public const string DATA_STALE = "DATA_STALE";

    /// <summary>
    /// Feed-Error: Bid > Ask (negativer Spread).
    /// </summary>
    public const string FEED_ERROR = "FEED_ERROR";

    /// <summary>
    /// Cooldown nach letztem Exit noch aktiv.
    /// </summary>
    public const string COOLDOWN = "COOLDOWN";

    // ============================================================
    // SPECIAL REASONS
    // ============================================================

    /// <summary>
    /// ResetDay Stage-2 abgeschlossen (Position war bereits FLAT).
    /// </summary>
    public const string DAY_RESET_COMPLETE = "DAY_RESET_COMPLETE";
}
