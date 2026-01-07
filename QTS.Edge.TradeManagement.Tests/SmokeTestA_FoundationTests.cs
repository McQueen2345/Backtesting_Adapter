using FluentAssertions;
using QTS.Edge.TradeManagement.Configuration;
using QTS.Edge.TradeManagement.Models;
using Xunit;

namespace QTS.Edge.TradeManagement.Tests;

/// <summary>
/// Smoke Test A: Validiert alle Phase A Foundation-Komponenten.
/// </summary>
public class SmokeTestA_FoundationTests
{
    // ============================================================
    // ENUMS TESTS
    // ============================================================

    [Fact]
    public void PositionState_HasExactly3Values()
    {
        var values = Enum.GetValues<PositionState>();
        values.Should().HaveCount(3);
        values.Should().Contain(PositionState.Flat);
        values.Should().Contain(PositionState.Long);
        values.Should().Contain(PositionState.Short);
    }

    [Fact]
    public void TradeAction_HasExactly4Values()
    {
        var values = Enum.GetValues<TradeAction>();
        values.Should().HaveCount(4);
        values.Should().Contain(TradeAction.Hold);
        values.Should().Contain(TradeAction.EnterLong);
        values.Should().Contain(TradeAction.EnterShort);
        values.Should().Contain(TradeAction.Exit);
    }

    [Fact]
    public void RiskState_HasExactly3Values()
    {
        var values = Enum.GetValues<RiskState>();
        values.Should().HaveCount(3);
        values.Should().Contain(RiskState.Normal);
        values.Should().Contain(RiskState.SoftPaused);
        values.Should().Contain(RiskState.HardDisabled);
    }

    // ============================================================
    // DECISION REASONS TESTS
    // ============================================================

    [Fact]
    public void DecisionReasons_AllExitReasonsExist()
    {
        DecisionReasons.RISK_KILL_SWITCH.Should().Be("RISK_KILL_SWITCH");
        DecisionReasons.EMERGENCY_STOP.Should().Be("EMERGENCY_STOP");
        DecisionReasons.TIME_EXIT.Should().Be("TIME_EXIT");
        DecisionReasons.WINDOW_CLOSE.Should().Be("WINDOW_CLOSE");
        DecisionReasons.DAY_RESET.Should().Be("DAY_RESET");
        DecisionReasons.ADAPTER_DISCONNECT.Should().Be("ADAPTER_DISCONNECT");
    }

    [Fact]
    public void DecisionReasons_AllHoldReasonsExist()
    {
        DecisionReasons.TRADING_DISABLED.Should().Be("TRADING_DISABLED");
        DecisionReasons.OUTSIDE_WINDOW.Should().Be("OUTSIDE_WINDOW");
        DecisionReasons.IN_POSITION.Should().Be("IN_POSITION");
        DecisionReasons.SOFT_PAUSED.Should().Be("SOFT_PAUSED");
        DecisionReasons.SPREAD_GATE.Should().Be("SPREAD_GATE");
        DecisionReasons.NO_SIGNAL.Should().Be("NO_SIGNAL");
        DecisionReasons.DATA_STALE.Should().Be("DATA_STALE");
        DecisionReasons.FEED_ERROR.Should().Be("FEED_ERROR");
        DecisionReasons.COOLDOWN.Should().Be("COOLDOWN");
    }

    [Fact]
    public void DecisionReasons_SpecialReasonsExist()
    {
        DecisionReasons.DAY_RESET_COMPLETE.Should().Be("DAY_RESET_COMPLETE");
    }

    // ============================================================
    // TRADE DECISION FLAGS TESTS
    // ============================================================

    [Fact]
    public void TradeDecisionFlags_DefaultsAreFalse()
    {
        var flags = new TradeDecisionFlags();

        flags.CooldownActive.Should().BeFalse();
        flags.SpreadGateActive.Should().BeFalse();
        flags.DataStale.Should().BeFalse();
    }

    [Fact]
    public void TradeDecisionFlags_CanSetValues()
    {
        var flags = new TradeDecisionFlags
        {
            CooldownActive = true,
            SpreadGateActive = true,
            DataStale = true
        };

        flags.CooldownActive.Should().BeTrue();
        flags.SpreadGateActive.Should().BeTrue();
        flags.DataStale.Should().BeTrue();
    }

    // ============================================================
    // TRADE DECISION TESTS
    // ============================================================

    [Fact]
    public void TradeDecision_HasDefaultFlags()
    {
        var decision = new TradeDecision
        {
            Action = TradeAction.Hold,
            Reason = DecisionReasons.NO_SIGNAL,
            Timestamp = DateTime.UtcNow,
            MidPrice = 5000m
        };

        decision.Flags.Should().NotBeNull();
        decision.Flags.CooldownActive.Should().BeFalse();
    }

    [Fact]
    public void TradeDecision_CanSetAllProperties()
    {
        var timestamp = new DateTime(2026, 1, 5, 15, 0, 0, DateTimeKind.Utc);
        var flags = new TradeDecisionFlags { CooldownActive = true };

        var decision = new TradeDecision
        {
            Action = TradeAction.EnterLong,
            Reason = "ENTRY",
            Timestamp = timestamp,
            MidPrice = 5000.25m,
            Direction = 1,
            Quantity = 1,
            RiskState = RiskState.Normal,
            Flags = flags
        };

        decision.Action.Should().Be(TradeAction.EnterLong);
        decision.Reason.Should().Be("ENTRY");
        decision.Timestamp.Should().Be(timestamp);
        decision.MidPrice.Should().Be(5000.25m);
        decision.Direction.Should().Be(1);
        decision.Quantity.Should().Be(1);
        decision.RiskState.Should().Be(RiskState.Normal);
        decision.Flags.CooldownActive.Should().BeTrue();
    }

    // ============================================================
    // POSITION TESTS
    // ============================================================

    [Fact]
    public void Position_Flat_ReturnsCorrectDefaults()
    {
        var flat = Position.Flat;

        flat.State.Should().Be(PositionState.Flat);
        flat.Direction.Should().Be(0);
        flat.Quantity.Should().Be(0);
        flat.EntryPrice.Should().Be(0m);
        flat.EntryTime.Should().BeNull();
        flat.UnrealizedPnLTicks.Should().Be(0m);
        flat.UnrealizedPnLDollars.Should().Be(0m);
    }

    [Fact]
    public void Position_CanCreateLongPosition()
    {
        var entryTime = new DateTime(2026, 1, 5, 15, 0, 0, DateTimeKind.Utc);

        var position = new Position
        {
            State = PositionState.Long,
            Direction = 1,
            Quantity = 1,
            EntryPrice = 5000.00m,
            EntryTime = entryTime,
            UnrealizedPnLTicks = 4m,
            UnrealizedPnLDollars = 50m
        };

        position.State.Should().Be(PositionState.Long);
        position.Direction.Should().Be(1);
        position.Quantity.Should().Be(1);
        position.EntryPrice.Should().Be(5000.00m);
        position.EntryTime.Should().Be(entryTime);
        position.UnrealizedPnLTicks.Should().Be(4m);
        position.UnrealizedPnLDollars.Should().Be(50m);
    }

    // ============================================================
    // TRADE RECORD TESTS
    // ============================================================

    [Fact]
    public void TradeRecord_IsWin_TrueWhenNetPositive()
    {
        var record = new TradeRecord
        {
            EntryTime = DateTime.UtcNow.AddMinutes(-1),
            ExitTime = DateTime.UtcNow,
            EntryPrice = 5000.00m,
            ExitPrice = 5001.00m,
            Direction = 1,
            Quantity = 1,
            PnLTicks = 4m,
            GrossPnLDollars = 50m,
            FeesDollars = 4.50m,
            NetPnLDollars = 45.50m,
            ExitReason = DecisionReasons.TIME_EXIT,
            Duration = TimeSpan.FromMinutes(1)
        };

        record.IsWin.Should().BeTrue();
    }

    [Fact]
    public void TradeRecord_IsWin_FalseWhenNetNegative()
    {
        var record = new TradeRecord
        {
            EntryTime = DateTime.UtcNow.AddMinutes(-1),
            ExitTime = DateTime.UtcNow,
            EntryPrice = 5000.00m,
            ExitPrice = 4999.00m,
            Direction = 1,
            Quantity = 1,
            PnLTicks = -4m,
            GrossPnLDollars = -50m,
            FeesDollars = 4.50m,
            NetPnLDollars = -54.50m,
            ExitReason = DecisionReasons.EMERGENCY_STOP,
            Duration = TimeSpan.FromMinutes(1)
        };

        record.IsWin.Should().BeFalse();
    }

    [Fact]
    public void TradeRecord_IsWin_FalseWhenNetZero()
    {
        var record = new TradeRecord
        {
            EntryTime = DateTime.UtcNow.AddMinutes(-1),
            ExitTime = DateTime.UtcNow,
            EntryPrice = 5000.00m,
            ExitPrice = 5000.00m,
            Direction = 1,
            Quantity = 1,
            PnLTicks = 0m,
            GrossPnLDollars = 0m,
            FeesDollars = 0m,
            NetPnLDollars = 0m,
            ExitReason = DecisionReasons.TIME_EXIT,
            Duration = TimeSpan.FromMinutes(1)
        };

        record.IsWin.Should().BeFalse();
    }

    // ============================================================
    // DAILY STATISTICS TESTS
    // ============================================================

    [Fact]
    public void DailyStatistics_WinRate_ZeroWhenNoTrades()
    {
        var stats = DailyStatistics.Empty(10000m);

        stats.WinRate.Should().Be(0.0);
    }

    [Fact]
    public void DailyStatistics_WinRate_CalculatesCorrectly()
    {
        var stats = new DailyStatistics
        {
            TradeCount = 10,
            WinCount = 6,
            LossCount = 4,
            ConsecutiveLosses = 1,
            GrossPnLDollars = 100m,
            TotalFeesDollars = 45m,
            NetPnLDollars = 55m,
            EquityPeakToday = 10100m,
            CurrentDrawdown = 50m
        };

        stats.WinRate.Should().BeApproximately(0.6, 0.001);
    }

    [Fact]
    public void DailyStatistics_Empty_HasCorrectDefaults()
    {
        var dayStartEquity = 10000m;
        var stats = DailyStatistics.Empty(dayStartEquity);

        stats.TradeCount.Should().Be(0);
        stats.WinCount.Should().Be(0);
        stats.LossCount.Should().Be(0);
        stats.ConsecutiveLosses.Should().Be(0);
        stats.GrossPnLDollars.Should().Be(0m);
        stats.TotalFeesDollars.Should().Be(0m);
        stats.NetPnLDollars.Should().Be(0m);
        stats.EquityPeakToday.Should().Be(dayStartEquity);
        stats.CurrentDrawdown.Should().Be(0m);
    }

    // ============================================================
    // EXIT RESULT TESTS
    // ============================================================

    [Fact]
    public void ExitResult_NoExit_HasCorrectValues()
    {
        var result = ExitResult.NoExit;

        result.ShouldExit.Should().BeFalse();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void ExitResult_Exit_HasCorrectValues()
    {
        var result = ExitResult.Exit(DecisionReasons.EMERGENCY_STOP);

        result.ShouldExit.Should().BeTrue();
        result.Reason.Should().Be(DecisionReasons.EMERGENCY_STOP);
    }

    // ============================================================
    // CONFIG TESTS
    // ============================================================

    [Fact]
    public void Config_Default_HasCorrectValues()
    {
        var config = TradeManagementConfig.Default;

        config.EmergencyStopTicks.Should().Be(10);
        config.MaxPositionSeconds.Should().Be(300);
        config.CooldownNormalMs.Should().Be(2000);
        config.CooldownEscalatedMs.Should().Be(10000);
        config.ContractSize.Should().Be(1);
        config.DailyMaxLossR.Should().Be(3);
        config.DailyLossLimitDollars.Should().Be(-500m);
        config.MaxIntradayDrawdown.Should().Be(750m);
        config.MaxConsecutiveLosses.Should().Be(5);
        config.SoftPauseMinutes.Should().Be(10);
        config.MaxSpreadTicks.Should().Be(2);
        config.TickSize.Should().Be(0.25m);
        config.TickValue.Should().Be(12.50m);
    }

    [Fact]
    public void Config_Validate_ThrowsWhenNoDailyLossLimits()
    {
        var config = new TradeManagementConfig
        {
            DailyLossLimitPct = null,
            DailyMaxLossR = null,
            DailyLossLimitDollars = null
        };

        var act = () => config.Validate();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*daily loss limits*");
    }

    [Fact]
    public void Config_Validate_PassesWithDefaultConfig()
    {
        var config = TradeManagementConfig.Default;

        var act = () => config.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Config_Validate_ThrowsForInvalidEmergencyStopTicks()
    {
        var config = new TradeManagementConfig { EmergencyStopTicks = 0 };

        var act = () => config.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
