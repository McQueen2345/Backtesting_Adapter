namespace QTS.Backtest.Contracts.Models;

/// <summary>
/// Session-Transition Ergebnis.
/// Immutable Record für Thread-Safety.
/// </summary>
public record SessionTransition
{
    /// <summary>
    /// Ist der aktuelle Snapshot innerhalb der Session?
    /// </summary>
    public required bool IsInSession { get; init; }
    
    /// <summary>
    /// Hat die Session gerade begonnen? (Out → In Transition)
    /// </summary>
    public required bool SessionStarted { get; init; }
    
    /// <summary>
    /// Hat die Session gerade geendet? (In → Out Transition)
    /// </summary>
    public required bool SessionEnded { get; init; }
    
    /// <summary>
    /// Timestamp des letzten In-Session Snapshots (für OnMarketClose).
    /// </summary>
    public DateTime? LastInSessionTimestamp { get; init; }
}
