using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Calculators;

/// <summary>
/// Berechnet den Z-Score basierend auf Median und MAD.
/// Formel: Z = (value - median) / (MadScale * MAD), clipped to [-ZClip, +ZClip]
/// </summary>
public sealed class ZScoreCalculator : IZScoreCalculator
{
    private readonly double _madScale;
    private readonly double _zClip;
    private readonly double _epsilon;

    public ZScoreCalculator(EdgeConfiguration config)
    {
        _madScale = config.MadScale;
        _zClip = config.ZClip;
        _epsilon = config.Epsilon;
    }

    /// <inheritdoc />
    public double Calculate(double value, double median, double mad)
    {
        // Edge Case: MAD zu klein → keine sinnvolle Berechnung möglich
        if (mad < _epsilon)
        {
            return 0.0;
        }

        // Z-Score Formel
        var z = (value - median) / (_madScale * mad);

        // Clipping auf [-ZClip, +ZClip]
        return Math.Clamp(z, -_zClip, _zClip);
    }
}
