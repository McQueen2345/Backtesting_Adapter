namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Berechnet den Z-Score basierend auf Median und MAD.
/// Formel: Z = (value - median) / (1.4826 * MAD), clipped to [-ZClip, +ZClip]
/// </summary>
public interface IZScoreCalculator
{
    double Calculate(double value, double median, double mad);
}
