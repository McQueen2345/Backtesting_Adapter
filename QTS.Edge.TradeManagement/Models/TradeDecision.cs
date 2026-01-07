namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Ergebnis von ProcessTick - beschreibt die Trading-Entscheidung.
/// </summary>
public sealed record TradeDecision
{
    /// <summary>
    /// Die zu ausführende Aktion.
    /// </summary>
    public required TradeAction Action { get; init; }

    /// <summary>
    /// Grund für die Entscheidung (aus DecisionReasons).
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Zeitstempel der Entscheidung (aus dom.Timestamp).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// MidPrice zum Zeitpunkt der Entscheidung.
    /// </summary>
    public required decimal MidPrice { get; init; }

    /// <summary>
    /// Richtung: +1 (Long), -1 (Short), null (Hold/Exit ohne Richtung).
    /// </summary>
    public int? Direction { get; init; }

    /// <summary>
    /// Anzahl Kontrakte.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Aktueller Risk State zum Zeitpunkt der Entscheidung.
    /// </summary>
    public RiskState RiskState { get; init; }

    /// <summary>
    /// Sekundäre Telemetrie-Flags.
    /// </summary>
    public TradeDecisionFlags Flags { get; init; } = new();
}
