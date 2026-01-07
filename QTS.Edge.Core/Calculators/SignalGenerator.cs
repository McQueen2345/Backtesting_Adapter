using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Interfaces;

namespace QTS.Edge.Core.Calculators;

/// <summary>
/// Generiert Trading-Signale basierend auf Z-Score mit Hysterese und Cooldown.
/// </summary>
public sealed class SignalGenerator : ISignalGenerator
{
    private readonly double _zThreshold;
    private readonly double _hysteresisFactor;
    private readonly int _signalCooldownMs;

    private int _currentSignal;
    private DateTime? _signalTimestamp;
    private DateTime? _lastSignalChangeTime;

    public SignalGenerator(EdgeConfiguration config)
    {
        _zThreshold = config.ZThreshold;
        _hysteresisFactor = config.HysteresisFactor;
        _signalCooldownMs = config.SignalCooldownMs;

        _currentSignal = 0;
        _signalTimestamp = null;
        _lastSignalChangeTime = null;
    }

    public int CurrentSignal => _currentSignal;

    public DateTime? SignalTimestamp => _signalTimestamp;

    public int Generate(double zScore, DateTime timestamp, bool isContextWarm, bool isDataStale, bool qualityGatePassed)
    {
        // Gates: Wenn nicht bereit, Signal = 0 zurückgeben (aber State nicht ändern)
        if (!isContextWarm || isDataStale || !qualityGatePassed)
        {
            return 0;
        }

        // Hysterese-Thresholds berechnen
        double entryThreshold = _zThreshold;                              // 1.5
        double exitThreshold = _zThreshold * _hysteresisFactor;           // 0.75
        double reversalThreshold = _zThreshold + exitThreshold;           // 2.25

        int newSignal = _currentSignal;

        switch (_currentSignal)
        {
            case 0: // FLAT
                if (zScore >= entryThreshold)
                {
                    newSignal = 1; // LONG
                }
                else if (zScore <= -entryThreshold)
                {
                    newSignal = -1; // SHORT
                }
                break;

            case 1: // LONG
                // WICHTIG: Reversal-Check ZUERST (bevor Exit-Check)
                if (zScore <= -reversalThreshold && IsCooldownElapsed(timestamp))
                {
                    newSignal = -1; // Reversal to SHORT
                }
                else if (zScore < exitThreshold && zScore > -entryThreshold)
                {
                    newSignal = 0; // Exit to FLAT (nur wenn nicht stark negativ)
                }
                break;

            case -1: // SHORT
                // WICHTIG: Reversal-Check ZUERST (bevor Exit-Check)
                if (zScore >= reversalThreshold && IsCooldownElapsed(timestamp))
                {
                    newSignal = 1; // Reversal to LONG
                }
                else if (zScore > -exitThreshold && zScore < entryThreshold)
                {
                    newSignal = 0; // Exit to FLAT (nur wenn nicht stark positiv)
                }
                break;
        }

        // Signal-Änderung verarbeiten
        if (newSignal != _currentSignal)
        {
            _currentSignal = newSignal;
            _signalTimestamp = timestamp;
            _lastSignalChangeTime = timestamp;
        }

        return _currentSignal;
    }

    public void Reset()
    {
        _currentSignal = 0;
        _signalTimestamp = null;
        _lastSignalChangeTime = null;
    }

    private bool IsCooldownElapsed(DateTime currentTimestamp)
    {
        if (!_lastSignalChangeTime.HasValue)
        {
            return true;
        }

        var elapsed = (currentTimestamp - _lastSignalChangeTime.Value).TotalMilliseconds;
        return elapsed >= _signalCooldownMs;
    }
}
