namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Repräsentiert ein generiertes Trading-Signal mit allen relevanten Metriken.
/// </summary>
public interface IEdgeSignal
{
    DateTime Timestamp { get; }
    double StructImbRaw { get; }
    double StructImbZ { get; }
    int Signal { get; }
    bool IsContextWarm { get; }
    bool IsDataStale { get; }
    bool IsQualityGatePassed { get; }
}
