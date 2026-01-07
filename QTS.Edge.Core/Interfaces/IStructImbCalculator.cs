namespace QTS.Edge.Core.Interfaces;

/// <summary>
/// Berechnet die rohe strukturelle Imbalance aus Bid/Ask-Volumen.
/// </summary>
public interface IStructImbCalculator
{
    /// <summary>
    /// Berechnet die Imbalance.
    /// </summary>
    /// <param name="bidSize">Bid-Volumen</param>
    /// <param name="askSize">Ask-Volumen</param>
    /// <returns>Wert zwischen -1.0 und +1.0</returns>
    double Calculate(int bidSize, int askSize);
}
