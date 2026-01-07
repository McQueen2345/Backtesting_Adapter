namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Sekundäre Telemetrie-Flags für TradeDecision.
/// PrimaryReason bleibt eindeutig, Flags erfassen zusätzliche Zustände.
/// </summary>
public sealed record TradeDecisionFlags
{
    /// <summary>
    /// True wenn Cooldown zum Zeitpunkt der Decision aktiv war.
    /// </summary>
    public bool CooldownActive { get; init; }

    /// <summary>
    /// True wenn SpreadGate zum Zeitpunkt der Decision nicht bestanden hätte.
    /// </summary>
    public bool SpreadGateActive { get; init; }

    /// <summary>
    /// True wenn signal.IsDataStale == true war.
    /// </summary>
    public bool DataStale { get; init; }
}
