namespace QTS.Edge.Core.Configuration;

/// <summary>
/// Konfiguration für den StructImb_L1_Strong Edge.
/// Alle Werte sind validiert und dürfen nicht geändert werden (Spec-Freeze).
/// </summary>
public sealed class EdgeConfiguration
{
    // Timing
    public int SnapshotIntervalMs { get; init; } = 100;
    public int WindowSize { get; init; } = 36_000;
    public int MinWarmupSamples { get; init; } = 200;

    // Z-Score
    public double ZThreshold { get; init; } = 1.5;
    public double ZClip { get; init; } = 5.0;
    public double MadScale { get; init; } = 1.4826;
    public double Epsilon { get; init; } = 0.001;

    // Signal Stability
    public int SignalCooldownMs { get; init; } = 1000;
    public double HysteresisFactor { get; init; } = 0.5;

    // Quality Gates
    public bool EnableQualityGates { get; init; } = true;
    public int MaxSpreadTicks { get; init; } = 4;
    public int MinDepthL1 { get; init; } = 1;

    // ES Futures
    public decimal TickSize { get; init; } = 0.25m;
    public decimal TickValue { get; init; } = 12.50m;

    public static EdgeConfiguration Default => new();
}
