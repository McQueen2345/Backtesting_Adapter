using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Calculators;

/// <summary>
/// Berechnet die rohe strukturelle Imbalance aus Bid/Ask-Volumen.
/// </summary>
public sealed class StructImbCalculator : IStructImbCalculator
{
    /// <inheritdoc />
    public double Calculate(int bidSize, int askSize)
    {
        var safeBid = Math.Max(0, bidSize);
        var safeAsk = Math.Max(0, askSize);

        // FIX: Long-Cast verhindert Integer-Overflow bei großen Werten
        long total = (long)safeBid + (long)safeAsk;

        if (total == 0)
            return 0.0;

        return (double)(safeBid - safeAsk) / total;
    }
}
