namespace QTS.Edge.TradeManagement.Models;

/// <summary>
/// Ergebnis von IPositionTracker.CheckExit().
/// </summary>
public sealed record ExitResult
{
    /// <summary>
    /// True wenn Exit getriggert werden soll.
    /// </summary>
    public required bool ShouldExit { get; init; }

    /// <summary>
    /// Grund für den Exit (aus DecisionReasons). Null wenn ShouldExit == false.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Factory für "kein Exit".
    /// </summary>
    public static ExitResult NoExit => new() { ShouldExit = false, Reason = null };

    /// <summary>
    /// Factory für Exit mit Grund.
    /// </summary>
    public static ExitResult Exit(string reason) => new() { ShouldExit = true, Reason = reason };
}
