using FluentAssertions;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using Xunit;

namespace QTS.Edge.Tests.Calculators;

public class SignalGeneratorTests
{
    private readonly SignalGenerator _generator;
    private readonly DateTime _baseTime;

    public SignalGeneratorTests()
    {
        _generator = new SignalGenerator(EdgeConfiguration.Default);
        _baseTime = DateTime.UtcNow;
    }

    // === T061: Gate Tests ===

    [Fact]
    public void Generate_ContextNotWarm_ReturnsZero()
    {
        var result = _generator.Generate(
            zScore: 2.0,
            timestamp: _baseTime,
            isContextWarm: false,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(0);
    }

    [Fact]
    public void Generate_DataStale_ReturnsZero()
    {
        var result = _generator.Generate(
            zScore: 2.0,
            timestamp: _baseTime,
            isContextWarm: true,
            isDataStale: true,
            qualityGatePassed: true
        );

        result.Should().Be(0);
    }

    [Fact]
    public void Generate_QualityGateFailed_ReturnsZero()
    {
        var result = _generator.Generate(
            zScore: 2.0,
            timestamp: _baseTime,
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: false
        );

        result.Should().Be(0);
    }

    // === T062: Threshold Tests ===

    [Fact]
    public void Generate_ZBelowThreshold_ReturnsZero()
    {
        // Z = 1.4 (unter 1.5 Threshold) → kein Signal
        var result = _generator.Generate(
            zScore: 1.4,
            timestamp: _baseTime,
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(0);
    }

    [Fact]
    public void Generate_ZAboveThreshold_ReturnsLong()
    {
        // Z = 1.5 (genau am Threshold) → LONG
        var result = _generator.Generate(
            zScore: 1.5,
            timestamp: _baseTime,
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(1);
    }

    [Fact]
    public void Generate_ZBelowNegThreshold_ReturnsShort()
    {
        // Z = -1.5 (genau am negativen Threshold) → SHORT
        var result = _generator.Generate(
            zScore: -1.5,
            timestamp: _baseTime,
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(-1);
    }

    // === T063: Hysterese Tests ===

    [Fact]
    public void Generate_LongToShort_RequiresHysteresis()
    {
        // Erst LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.CurrentSignal.Should().Be(1);

        // Z = -1.7 reicht NICHT für Reversal (braucht -2.25)
        var result = _generator.Generate(
            zScore: -1.7,
            timestamp: _baseTime.AddSeconds(2),
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(1, "Bleibt LONG weil Hysterese nicht erreicht");
    }

    [Fact]
    public void Generate_LongToShort_WithHysteresis_Works()
    {
        // Erst LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.CurrentSignal.Should().Be(1);

        // Z = -2.3 überschreitet Reversal-Threshold (-2.25)
        var result = _generator.Generate(
            zScore: -2.3,
            timestamp: _baseTime.AddSeconds(2),
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(-1, "Wechselt zu SHORT weil Hysterese erreicht");
    }

    [Fact]
    public void Generate_LongExit_RequiresLowerThreshold()
    {
        // Erst LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.CurrentSignal.Should().Be(1);

        // Z = 0.8 ist unter Exit-Threshold (0.75) → Exit zu FLAT
        // KORREKTUR: Exit bei Z < 0.75, also Z=0.7 sollte Exit auslösen
        var result = _generator.Generate(
            zScore: 0.7,
            timestamp: _baseTime.AddSeconds(1),
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(0, "Exit zu FLAT weil unter Exit-Threshold");
    }

    // === T064: Cooldown Tests ===

    [Fact]
    public void Generate_SignalChange_RequiresCooldown()
    {
        // LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.CurrentSignal.Should().Be(1);

        // Sofort Reversal versuchen (nur 500ms später, Cooldown = 1000ms)
        var result = _generator.Generate(
            zScore: -2.5,
            timestamp: _baseTime.AddMilliseconds(500),
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(1, "Reversal blockiert weil Cooldown nicht abgelaufen");
    }

    [Fact]
    public void Generate_SignalChange_AfterCooldown_Works()
    {
        // LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.CurrentSignal.Should().Be(1);

        // Reversal nach Cooldown (1001ms später)
        var result = _generator.Generate(
            zScore: -2.5,
            timestamp: _baseTime.AddMilliseconds(1001),
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(-1, "Reversal erlaubt weil Cooldown abgelaufen");
    }

    // === T065: Reset Tests ===

    [Fact]
    public void Reset_ClearsSignal()
    {
        // LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.CurrentSignal.Should().Be(1);

        // Reset
        _generator.Reset();

        _generator.CurrentSignal.Should().Be(0);
    }

    [Fact]
    public void Reset_ClearsTimestamp()
    {
        // LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);
        _generator.SignalTimestamp.Should().NotBeNull();

        // Reset
        _generator.Reset();

        _generator.SignalTimestamp.Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsCooldown_AllowsImmediateSignal()
    {
        // LONG eingehen
        _generator.Generate(1.5, _baseTime, true, false, true);

        // Reset
        _generator.Reset();

        // Sofort neues Signal möglich (kein Cooldown)
        var result = _generator.Generate(
            zScore: -1.5,
            timestamp: _baseTime.AddMilliseconds(1),
            isContextWarm: true,
            isDataStale: false,
            qualityGatePassed: true
        );

        result.Should().Be(-1, "Nach Reset sofort neues Signal möglich");
    }
}
