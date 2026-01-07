namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Position State Machine Zustände.
/// </summary>
public enum PositionState
{
    Flat,
    Long,
    Short
}

/// <summary>
/// Mögliche Trading-Aktionen als Ergebnis von ProcessTick.
/// </summary>
public enum TradeAction
{
    Hold,
    EnterLong,
    EnterShort,
    Exit
}

/// <summary>
/// Risk Management Zustände.
/// </summary>
public enum RiskState
{
    /// <summary>
    /// Trading erlaubt.
    /// </summary>
    Normal,

    /// <summary>
    /// Entries blockiert, Exits erlaubt, Auto-Resume nach Timeout.
    /// </summary>
    SoftPaused,

    /// <summary>
    /// Kill Switch: ForceExit + keine Trades bis ResetDay.
    /// </summary>
    HardDisabled
}
