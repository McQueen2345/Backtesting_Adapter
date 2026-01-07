using FluentAssertions;
using QTS.Edge.Core;
using QTS.Edge.Core.Calculators;
using QTS.Edge.Core.Configuration;
using QTS.Edge.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace QTS.Edge.Tests.Integration;

/// <summary>
/// Phase E: Hysterese & Cooldown Integration Tests.
/// Testet die Signal-State-Machine gemäß Spec v1.2, Kapitel 7.2.
/// </summary>
public class HystereseCooldownTests
{
    private readonly ITestOutputHelper _output;
    private readonly EdgeConfiguration _config;
    private readonly DateTime _baseTime;

    public HystereseCooldownTests(ITestOutputHelper output)
    {
        _output = output;
        _config = EdgeConfiguration.Default;
        _baseTime = DateTime.UtcNow;
    }

    // ============================================================
    // STATE MACHINE: FLAT TRANSITIONS
    // ============================================================

    [Fact]
    public void StateMachine_FlatToLong_AtThreshold()
    {
        var gen = new SignalGenerator(_config);

        // FLAT, Z = 1.5 → LONG
        var signal = gen.Generate(1.5, _baseTime, true, false, true);

        signal.Should().Be(1, "Z >= 1.5 should transition FLAT → LONG");
        gen.CurrentSignal.Should().Be(1);

        _output.WriteLine($"FLAT + Z=1.5 → {SignalName(signal)}");
    }

    [Fact]
    public void StateMachine_FlatToShort_AtThreshold()
    {
        var gen = new SignalGenerator(_config);

        // FLAT, Z = -1.5 → SHORT
        var signal = gen.Generate(-1.5, _baseTime, true, false, true);

        signal.Should().Be(-1, "Z <= -1.5 should transition FLAT → SHORT");
        gen.CurrentSignal.Should().Be(-1);

        _output.WriteLine($"FLAT + Z=-1.5 → {SignalName(signal)}");
    }

    // ============================================================
    // STATE MACHINE: LONG TRANSITIONS
    // ============================================================

    [Fact]
    public void StateMachine_LongToFlat_BelowExitThreshold()
    {
        var gen = new SignalGenerator(_config);

        // Erst LONG
        gen.Generate(1.8, _baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);

        // LONG, Z = 0.74 → FLAT (Exit bei Z < 0.75)
        var signal = gen.Generate(0.74, _baseTime.AddSeconds(2), true, false, true);

        signal.Should().Be(0, "Z < 0.75 should transition LONG → FLAT");

        _output.WriteLine($"LONG + Z=0.74 → {SignalName(signal)}");
    }

    [Fact]
    public void StateMachine_LongStays_AboveExitThreshold()
    {
        var gen = new SignalGenerator(_config);

        // Erst LONG
        gen.Generate(1.8, _baseTime, true, false, true);

        // LONG, Z = 1.2 → bleibt LONG (1.2 >= 0.75)
        var signal = gen.Generate(1.2, _baseTime.AddSeconds(2), true, false, true);

        signal.Should().Be(1, "Z >= 0.75 should keep LONG");

        _output.WriteLine($"LONG + Z=1.2 → {SignalName(signal)}");
    }

    [Fact]
    public void StateMachine_LongToShort_RequiresHysteresis()
    {
        var gen = new SignalGenerator(_config);

        // Erst LONG
        gen.Generate(1.8, _baseTime, true, false, true);

        // LONG, Z = -1.7 → bleibt LONG (braucht Z ≤ -2.25 für Reversal)
        var signal1 = gen.Generate(-1.7, _baseTime.AddSeconds(2), true, false, true);
        signal1.Should().NotBe(-1, "Z = -1.7 should not trigger reversal (needs -2.25)");

        _output.WriteLine($"LONG + Z=-1.7 → {SignalName(signal1)} (no reversal)");

        // LONG, Z = -2.25 → wird SHORT
        gen.Reset();
        gen.Generate(1.8, _baseTime, true, false, true);
        var signal2 = gen.Generate(-2.25, _baseTime.AddSeconds(2), true, false, true);
        signal2.Should().Be(-1, "Z = -2.25 should trigger LONG → SHORT reversal");

        _output.WriteLine($"LONG + Z=-2.25 → {SignalName(signal2)} (reversal!)");
    }

    // ============================================================
    // STATE MACHINE: SHORT TRANSITIONS
    // ============================================================

    [Fact]
    public void StateMachine_ShortToFlat_AboveExitThreshold()
    {
        var gen = new SignalGenerator(_config);

        // Erst SHORT
        gen.Generate(-1.8, _baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(-1);

        // SHORT, Z = -0.74 → FLAT (Exit bei Z > -0.75)
        var signal = gen.Generate(-0.74, _baseTime.AddSeconds(2), true, false, true);

        signal.Should().Be(0, "Z > -0.75 should transition SHORT → FLAT");

        _output.WriteLine($"SHORT + Z=-0.74 → {SignalName(signal)}");
    }

    [Fact]
    public void StateMachine_ShortToLong_RequiresHysteresis()
    {
        var gen = new SignalGenerator(_config);

        // Erst SHORT
        gen.Generate(-1.8, _baseTime, true, false, true);

        // SHORT, Z = 1.7 → bleibt SHORT (braucht Z ≥ 2.25 für Reversal)
        var signal1 = gen.Generate(1.7, _baseTime.AddSeconds(2), true, false, true);
        signal1.Should().NotBe(1, "Z = 1.7 should not trigger reversal (needs 2.25)");

        _output.WriteLine($"SHORT + Z=1.7 → {SignalName(signal1)} (no reversal)");

        // SHORT, Z = 2.25 → wird LONG
        gen.Reset();
        gen.Generate(-1.8, _baseTime, true, false, true);
        var signal2 = gen.Generate(2.25, _baseTime.AddSeconds(2), true, false, true);
        signal2.Should().Be(1, "Z = 2.25 should trigger SHORT → LONG reversal");

        _output.WriteLine($"SHORT + Z=2.25 → {SignalName(signal2)} (reversal!)");
    }

    // ============================================================
    // COOLDOWN TESTS
    // ============================================================

    [Fact]
    public void Cooldown_BlocksSignalChange()
    {
        var gen = new SignalGenerator(_config);

        // LONG at T=0
        gen.Generate(1.8, _baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);

        // Z = -2.5 at T=500ms → bleibt LONG (Cooldown nicht abgelaufen)
        var signal = gen.Generate(-2.5, _baseTime.AddMilliseconds(500), true, false, true);

        signal.Should().Be(1, "Cooldown (500ms < 1000ms) should block reversal");

        _output.WriteLine($"Signal at 500ms: {SignalName(signal)} (blocked)");
    }

    [Fact]
    public void Cooldown_AllowsAfterExpiry()
    {
        var gen = new SignalGenerator(_config);

        // LONG at T=0
        gen.Generate(1.8, _baseTime, true, false, true);

        // Z = -2.5 at T=1001ms → wird SHORT (Cooldown OK)
        var signal = gen.Generate(-2.5, _baseTime.AddMilliseconds(1001), true, false, true);

        signal.Should().Be(-1, "After 1001ms cooldown, reversal should be allowed");

        _output.WriteLine($"Signal at 1001ms: {SignalName(signal)} (allowed)");
    }

    [Fact]
    public void Cooldown_DoesNotBlockExit()
    {
        var gen = new SignalGenerator(_config);

        // LONG at T=0
        gen.Generate(1.8, _baseTime, true, false, true);

        // Z = 0.5 at T=100ms → wird FLAT (Exit braucht kein Cooldown!)
        var signal = gen.Generate(0.5, _baseTime.AddMilliseconds(100), true, false, true);

        signal.Should().Be(0, "Exit to FLAT should NOT require cooldown");

        _output.WriteLine($"Exit at 100ms: {SignalName(signal)} (not blocked)");
    }

    [Fact]
    public void Cooldown_ResetsAfterSignalChange()
    {
        var gen = new SignalGenerator(_config);

        // FLAT → LONG at T=0 (Cooldown startet)
        gen.Generate(1.8, _baseTime, true, false, true);

        // LONG → SHORT at T=1500ms (OK, Cooldown war abgelaufen)
        gen.Generate(-2.5, _baseTime.AddMilliseconds(1500), true, false, true);
        gen.CurrentSignal.Should().Be(-1);

        // SHORT → LONG at T=2000ms → blockiert (neuer Cooldown läuft)
        var signal1 = gen.Generate(2.5, _baseTime.AddMilliseconds(2000), true, false, true);
        signal1.Should().Be(-1, "New cooldown should block reversal at 2000ms");

        // SHORT → LONG at T=2501ms → OK
        var signal2 = gen.Generate(2.5, _baseTime.AddMilliseconds(2501), true, false, true);
        signal2.Should().Be(1, "After new cooldown expires, reversal allowed");

        _output.WriteLine("Cooldown resets after each signal change - CORRECT");
    }

    [Fact]
    public void Cooldown_ResetClearsCooldown()
    {
        var gen = new SignalGenerator(_config);

        // LONG at T=0 (Cooldown startet)
        gen.Generate(1.8, _baseTime, true, false, true);

        // Reset() at T=500ms
        gen.Reset();

        // FLAT → SHORT at T=600ms → OK (kein Cooldown nach Reset)
        var signal = gen.Generate(-1.8, _baseTime.AddMilliseconds(600), true, false, true);

        signal.Should().Be(-1, "After Reset, no cooldown should apply");

        _output.WriteLine("Reset clears cooldown - CORRECT");
    }

    // ============================================================
    // FULL SEQUENCE TEST
    // ============================================================

    [Fact]
    public void FullSequence_RealisticScenario()
    {
        var gen = new SignalGenerator(_config);
        var t = _baseTime;
        var history = new List<string>();

        void LogState(string action, double z, int signal)
        {
            history.Add($"{action}: Z={z:F1} → {SignalName(signal)}");
        }

        // 1. Start: FLAT
        gen.CurrentSignal.Should().Be(0);
        history.Add("Start: FLAT");

        // 2. Z steigt auf 1.8 → LONG
        var s1 = gen.Generate(1.8, t, true, false, true);
        s1.Should().Be(1);
        LogState("Entry", 1.8, s1);
        t = t.AddSeconds(2);

        // 3. Z fällt auf 1.2 → bleibt LONG
        var s2 = gen.Generate(1.2, t, true, false, true);
        s2.Should().Be(1);
        LogState("Hold", 1.2, s2);
        t = t.AddSeconds(2);

        // 4. Z fällt auf 0.8 → bleibt LONG (> 0.75)
        var s3 = gen.Generate(0.8, t, true, false, true);
        s3.Should().Be(1);
        LogState("Hold", 0.8, s3);
        t = t.AddSeconds(2);

        // 5. Z fällt auf 0.5 → FLAT (exit)
        var s4 = gen.Generate(0.5, t, true, false, true);
        s4.Should().Be(0);
        LogState("Exit", 0.5, s4);
        t = t.AddSeconds(2);

        // 6. Z fällt auf -1.8 → SHORT
        var s5 = gen.Generate(-1.8, t, true, false, true);
        s5.Should().Be(-1);
        LogState("Entry", -1.8, s5);
        t = t.AddMilliseconds(500); // Nur 500ms

        // 7. Z steigt schnell auf 2.5 → blockiert (Cooldown)
        var s6 = gen.Generate(2.5, t, true, false, true);
        s6.Should().Be(-1, "Cooldown should block rapid reversal");
        LogState("Blocked", 2.5, s6);
        t = t.AddMilliseconds(600); // Jetzt 1100ms total

        // 8. Z noch bei 2.5 → LONG (Cooldown OK)
        var s7 = gen.Generate(2.5, t, true, false, true);
        s7.Should().Be(1);
        LogState("Reversal", 2.5, s7);

        _output.WriteLine("=== REALISTIC SEQUENCE ===");
        foreach (var h in history)
        {
            _output.WriteLine($"  {h}");
        }
    }

    // ============================================================
    // EDGE CASES
    // ============================================================

    [Fact]
    public void StateMachine_DirectReversalBlocked_WithoutHysteresis()
    {
        var gen = new SignalGenerator(_config);

        // LONG
        gen.Generate(1.8, _baseTime, true, false, true);

        // Z = -1.6 → Nicht stark genug für Reversal, aber unter Entry
        // Sollte zu FLAT wechseln, nicht zu SHORT
        var signal = gen.Generate(-1.6, _baseTime.AddSeconds(2), true, false, true);

        signal.Should().NotBe(-1, "Z = -1.6 should not trigger direct reversal");

        _output.WriteLine($"LONG + Z=-1.6 → {SignalName(signal)} (no direct reversal)");
    }

    [Fact]
    public void StateMachine_StaysInState_WhenGateFails()
    {
        var gen = new SignalGenerator(_config);

        // LONG
        gen.Generate(1.8, _baseTime, true, false, true);
        gen.CurrentSignal.Should().Be(1);

        // Gate Fail → Signal = 0, aber CurrentSignal bleibt LONG intern
        var signal = gen.Generate(-2.5, _baseTime.AddSeconds(2), true, false, false);

        signal.Should().Be(0, "Gate fail should return 0");

        // Interner State bleibt LONG
        gen.CurrentSignal.Should().Be(1, "Internal state should stay LONG");

        _output.WriteLine("Gate fail: Output=0 but internal state preserved");
    }

    private static string SignalName(int signal) => signal switch
    {
        1 => "LONG",
        -1 => "SHORT",
        _ => "FLAT"
    };
}
