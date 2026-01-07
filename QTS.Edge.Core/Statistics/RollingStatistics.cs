using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Statistics;

/// <summary>
/// Rollendes Fenster für Median/MAD-Berechnung.
/// WICHTIG: Bei IsDataStale=true darf Add() NICHT aufgerufen werden!
/// </summary>
public sealed class RollingStatistics : IRollingStatistics
{
    private readonly int _windowSize;
    private readonly int _minWarmupSamples;
    private readonly Queue<double> _values;

    // Cache für Determinismus: Median und MAD aus demselben Sort
    private (double Median, double Mad)? _cachedStats;

    public RollingStatistics(int windowSize, int minWarmupSamples)
    {
        _windowSize = windowSize;
        _minWarmupSamples = minWarmupSamples;
        _values = new Queue<double>(windowSize);
        _cachedStats = null;
    }

    public int Count => _values.Count;

    public bool IsWarm => Count >= _minWarmupSamples;

    public void Add(double value)
    {
        // Wenn Fenster voll, ältesten Wert entfernen
        if (_values.Count >= _windowSize)
        {
            _values.Dequeue();
        }

        _values.Enqueue(value);
        _cachedStats = null; // Cache invalidieren
    }

    public double GetMedian()
    {
        if (Count == 0)
        {
            return 0.0;
        }

        EnsureCache();
        return _cachedStats!.Value.Median;
    }

    public double GetMad()
    {
        if (Count == 0)
        {
            return 0.0;
        }

        EnsureCache();
        return _cachedStats!.Value.Mad;
    }

    public void Reset()
    {
        _values.Clear();
        _cachedStats = null;
    }

    private void EnsureCache()
    {
        if (_cachedStats.HasValue)
        {
            return;
        }

        // Sortierter Vektor für beide Berechnungen
        var sorted = _values.OrderBy(v => v).ToArray();

        // Median berechnen (Lower Median bei gerader Anzahl!)
        var median = CalculateLowerMedian(sorted);

        // MAD = Median der absoluten Abweichungen vom Median
        var absoluteDeviations = sorted
            .Select(v => Math.Abs(v - median))
            .OrderBy(v => v)
            .ToArray();

        var mad = CalculateLowerMedian(absoluteDeviations);

        _cachedStats = (median, mad);
    }

    private static double CalculateLowerMedian(double[] sorted)
    {
        if (sorted.Length == 0)
        {
            return 0.0;
        }

        // Lower Median: Bei gerader Anzahl den unteren der beiden mittleren Werte
        // Index: (n-1) / 2 (Integer-Division)
        int midIndex = (sorted.Length - 1) / 2;
        return sorted[midIndex];
    }
}
