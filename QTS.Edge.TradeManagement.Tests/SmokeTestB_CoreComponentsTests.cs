using FluentAssertions;
using QTS.Edge.Core.Models;
using QTS.Edge.TradeManagement.Configuration;
using QTS.Edge.TradeManagement.Models;
using QTS.Edge.TradeManagement.Services;
using Xunit;

namespace QTS.Edge.TradeManagement.Tests;

/// <summary>
/// Smoke Test B: Validiert alle Phase B Core Components.
/// </summary>
public class SmokeTestB_CoreComponentsTests
{
    private readonly TradeManagementConfig _config = TradeManagementConfig.Default;
    private readonly DateTime _baseTime = new(2026, 1, 5, 15, 0, 0, DateTimeKind.Utc); // Innerhalb Window

    // ============================================================
    // POSITION TRACKER TESTS
    // ============================================================

    [Fact]
    public void PositionTracker_EntryExitCooldownEntry_FullCycle()
    {
        var tracker = new PositionTracker(_config);

        // Initial: Flat
        tracker.CurrentPosition.State.Should().Be(PositionState.Flat);

        // Entry Long
        var entrySuccess = tracker.TryEntry(1, 5000.00m, _baseTime, 1);
        entrySuccess.Should().BeTrue();
        tracker.CurrentPosition.State.Should().Be(PositionState.Long);
        tracker.CurrentPosition.Direction.Should().Be(1);

        // Exit
        var exitTime = _baseTime.AddSeconds(30);
        var record = tracker.ExecuteExit(5001.00m, exitTime, DecisionReasons.TIME_EXIT);
        tracker.CurrentPosition.State.Should().Be(PositionState.Flat);
        record.PnLTicks.Should().Be(4m); // (5001-5000)/0.25 = 4 Ticks

        // Cooldown aktiv
        tracker.SetCooldown(exitTime, isEmergencyStop: false);
        tracker.IsInCooldown(exitTime.AddMilliseconds(1000)).Should().BeTrue();
        tracker.IsInCooldown(exitTime.AddMilliseconds(2500)).Should().BeFalse();

        // Entry nach Cooldown
        var reentrySuccess = tracker.TryEntry(-1, 5001.00m, exitTime.AddMilliseconds(2500), 1);
        reentrySuccess.Should().BeTrue();
        tracker.CurrentPosition.State.Should().Be(PositionState.Short);
    }

    [Fact]
    public void PositionTracker_EmergencyStop_TriggersEscalatedCooldown()
    {
        var tracker = new PositionTracker(_config);

        // Entry
        tracker.TryEntry(1, 5000.00m, _baseTime, 1);

        // Price drops 10+ ticks → Emergency
        var exitTime = _baseTime.AddSeconds(5);
        var checkResult = tracker.CheckExit(4997.50m, exitTime); // -10 Ticks
        checkResult.ShouldExit.Should().BeTrue();
        checkResult.Reason.Should().Be(DecisionReasons.EMERGENCY_STOP);

        // Execute Exit with escalated cooldown
        tracker.ExecuteExit(4997.50m, exitTime, DecisionReasons.EMERGENCY_STOP);
        tracker.SetCooldown(exitTime, isEmergencyStop: true);

        // Escalated Cooldown = 10s
        tracker.IsInCooldown(exitTime.AddMilliseconds(5000)).Should().BeTrue();
        tracker.IsInCooldown(exitTime.AddMilliseconds(10500)).Should().BeFalse();
    }

    // ============================================================
    // RISK MANAGER TESTS
    // ============================================================

    [Fact]
    public void RiskManager_ConsecutiveLosses_TriggersSoftPause()
    {
        var riskManager = new RiskManager(_config, 10000m);

        riskManager.CurrentState.Should().Be(RiskState.Normal);

        // 5 consecutive losses
        for (int i = 0; i < 5; i++)
        {
            var lossRecord = CreateTradeRecord(netPnL: -50m, exitTime: _baseTime.AddMinutes(i));
            riskManager.RecordTrade(lossRecord);
        }

        riskManager.CurrentState.Should().Be(RiskState.SoftPaused);
        riskManager.IsEntryAllowed.Should().BeFalse();
        riskManager.GetBlockReason().Should().Be(DecisionReasons.SOFT_PAUSED);
    }

    [Fact]
    public void RiskManager_SoftPause_AutoRecoveryAfterTimeout()
    {
        var riskManager = new RiskManager(_config, 10000m);

        // Trigger SoftPause
        for (int i = 0; i < 5; i++)
        {
            riskManager.RecordTrade(CreateTradeRecord(netPnL: -50m, exitTime: _baseTime.AddMinutes(i)));
        }

        riskManager.CurrentState.Should().Be(RiskState.SoftPaused);

        // Nach 10 Minuten: Auto-Recovery
        riskManager.CheckSoftPauseExpiry(_baseTime.AddMinutes(15));
        riskManager.CurrentState.Should().Be(RiskState.Normal);
    }

    [Fact]
    public void RiskManager_GetDailyLossLimit_StrictestGuard()
    {
        // Default Config: R=3 ($375), Dollars=-$500
        // STRICTEST-GUARD: max(-375, -500) = -375
        var riskManager = new RiskManager(_config, 10000m);

        var limit = riskManager.GetDailyLossLimit();
        limit.Should().Be(-375m); // 3R = 3 × $125 = $375
    }

    [Fact]
    public void RiskManager_Drawdown_TriggersHardDisabled()
    {
        var riskManager = new RiskManager(_config, 10000m);

        // Equity drops $750+
        riskManager.UpdateEquity(9200m); // Drawdown = $800

        riskManager.CurrentState.Should().Be(RiskState.HardDisabled);
        riskManager.GetBlockReason().Should().Be(DecisionReasons.TRADING_DISABLED);
    }

    [Fact]
    public void RiskManager_SpreadGate_DecimalPrecision()
    {
        var riskManager = new RiskManager(_config, 10000m);

        // Spread = 2 Ticks (0.50) → OK (MaxSpreadTicks = 2)
        var domOk = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.50m,
            BidSize: 100,
            AskSize: 100
        );
        riskManager.CheckSpreadGate(domOk).Should().BeTrue();

        // Spread = 3 Ticks (0.75) → FAIL
        var domFail = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.75m,
            BidSize: 100,
            AskSize: 100
        );
        riskManager.CheckSpreadGate(domFail).Should().BeFalse();
    }

    [Fact]
    public void RiskManager_TradingWindow_BoundaryChecks()
    {
        var riskManager = new RiskManager(_config, 10000m);

        // Before window (14:29:59 UTC)
        var beforeWindow = new DateTime(2026, 1, 5, 14, 29, 59, DateTimeKind.Utc);
        riskManager.IsWithinTradingWindow(beforeWindow).Should().BeFalse();

        // Start of window (14:30:00 UTC)
        var startWindow = new DateTime(2026, 1, 5, 14, 30, 0, DateTimeKind.Utc);
        riskManager.IsWithinTradingWindow(startWindow).Should().BeTrue();

        // End of window (21:00:00 UTC) → NOT included
        var endWindow = new DateTime(2026, 1, 5, 21, 0, 0, DateTimeKind.Utc);
        riskManager.IsWithinTradingWindow(endWindow).Should().BeFalse();

        // Just before end (20:59:59 UTC)
        var beforeEnd = new DateTime(2026, 1, 5, 20, 59, 59, DateTimeKind.Utc);
        riskManager.IsWithinTradingWindow(beforeEnd).Should().BeTrue();
    }

    // ============================================================
    // TRADE MANAGER TESTS
    // ============================================================

    [Fact]
    public void TradeManager_ProcessTick_FeedError_ReturnsHold()
    {
        var (tradeManager, _, _, _) = CreateTradeManager();

        // Bid > Ask = Feed Error
        var dom = new DomSnapshot(
            Timestamp: _baseTime,
            BidPrice: 5001.00m,
            AskPrice: 5000.00m, // Invertiert!
            BidSize: 100,
            AskSize: 100
        );
        var signal = CreateSignal(1, isDataStale: false);

        var decision = tradeManager.ProcessTick(signal, dom, 10000m);

        decision.Action.Should().Be(TradeAction.Hold);
        decision.Reason.Should().Be(DecisionReasons.FEED_ERROR);
    }

    [Fact]
    public void TradeManager_ProcessTick_DataStale_ReturnsHold()
    {
        var (tradeManager, _, _, _) = CreateTradeManager();

        var dom = CreateValidDom();
        var signal = CreateSignal(1, isDataStale: true); // STALE!

        var decision = tradeManager.ProcessTick(signal, dom, 10000m);

        decision.Action.Should().Be(TradeAction.Hold);
        decision.Reason.Should().Be(DecisionReasons.DATA_STALE);
        decision.Flags.DataStale.Should().BeTrue();
    }

    [Fact]
    public void TradeManager_ProcessTick_NoSignal_SetsFlags()
    {
        var (tradeManager, _, positionTracker, _) = CreateTradeManager();

        // Set cooldown active
        positionTracker.SetCooldown(_baseTime.AddSeconds(-1), isEmergencyStop: false);

        var dom = CreateValidDom();
        var signal = CreateSignal(0, isDataStale: false); // NO SIGNAL

        var decision = tradeManager.ProcessTick(signal, dom, 10000m);

        decision.Action.Should().Be(TradeAction.Hold);
        decision.Reason.Should().Be(DecisionReasons.NO_SIGNAL);
        decision.Flags.CooldownActive.Should().BeTrue(); // Secondary flag!
    }

    [Fact]
    public void TradeManager_ProcessTick_Entry_Success()
    {
        var (tradeManager, _, _, _) = CreateTradeManager();

        var dom = CreateValidDom();
        var signal = CreateSignal(1, isDataStale: false);

        var decision = tradeManager.ProcessTick(signal, dom, 10000m);

        decision.Action.Should().Be(TradeAction.EnterLong);
        decision.Direction.Should().Be(1);
        decision.Quantity.Should().Be(1);
        tradeManager.CurrentPosition.State.Should().Be(PositionState.Long);
    }

    [Fact]
    public void TradeManager_ResetDay_2Stage_WithOpenPosition()
    {
        var (tradeManager, _, _, _) = CreateTradeManager();

        // Enter position
        var dom = CreateValidDom();
        var signal = CreateSignal(1, isDataStale: false);
        tradeManager.ProcessTick(signal, dom, 10000m);
        tradeManager.CurrentPosition.State.Should().Be(PositionState.Long);

        // Stage 1: ResetDay with open position → Exit
        var resetDom = CreateValidDom(_baseTime.AddMinutes(1));
        var decision1 = tradeManager.ResetDay(10000m, resetDom);

        decision1.Action.Should().Be(TradeAction.Exit);
        decision1.Reason.Should().Be(DecisionReasons.DAY_RESET);
        tradeManager.CurrentPosition.State.Should().Be(PositionState.Flat);

        // Stage 2: Next ProcessTick finalizes reset
        var dom2 = CreateValidDom(_baseTime.AddMinutes(2));
        var signal2 = CreateSignal(0, isDataStale: false);
        tradeManager.ProcessTick(signal2, dom2, 10000m);

        // Verify reset happened (trades cleared)
        tradeManager.TodayTrades.Should().BeEmpty();
    }

    [Fact]
    public void TradeManager_ForceExit_WithPosition()
    {
        var (tradeManager, _, _, _) = CreateTradeManager();

        // Enter position
        var dom = CreateValidDom();
        var signal = CreateSignal(-1, isDataStale: false);
        tradeManager.ProcessTick(signal, dom, 10000m);
        tradeManager.CurrentPosition.State.Should().Be(PositionState.Short);

        // Force Exit
        var exitDom = CreateValidDom(_baseTime.AddSeconds(10));
        var decision = tradeManager.ForceExit(exitDom, DecisionReasons.ADAPTER_DISCONNECT);

        decision.Action.Should().Be(TradeAction.Exit);
        decision.Reason.Should().Be(DecisionReasons.ADAPTER_DISCONNECT);
        tradeManager.CurrentPosition.State.Should().Be(PositionState.Flat);
    }

    // ============================================================
    // TRADE JOURNAL TESTS
    // ============================================================

    [Fact]
    public void TradeJournal_RecordAndRetrieve()
    {
        var journal = new TradeJournal();

        var trade1 = CreateTradeRecord(netPnL: 50m, exitTime: _baseTime);
        var trade2 = CreateTradeRecord(netPnL: -30m, exitTime: _baseTime.AddMinutes(5));

        journal.RecordTrade(trade1);
        journal.RecordTrade(trade2);

        journal.GetTodayTrades().Should().HaveCount(2);

        journal.Clear();
        journal.GetTodayTrades().Should().BeEmpty();
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    private (TradeManager, RiskManager, PositionTracker, TradeJournal) CreateTradeManager()
    {
        var riskManager = new RiskManager(_config, 10000m);
        var positionTracker = new PositionTracker(_config);
        var tradeJournal = new TradeJournal();
        var tradeManager = new TradeManager(_config, riskManager, positionTracker, tradeJournal);

        return (tradeManager, riskManager, positionTracker, tradeJournal);
    }

    private DomSnapshot CreateValidDom(DateTime? timestamp = null)
    {
        return new DomSnapshot(
            Timestamp: timestamp ?? _baseTime,
            BidPrice: 5000.00m,
            AskPrice: 5000.25m, // 1 Tick spread
            BidSize: 100,
            AskSize: 100
        );
    }

    private static EdgeSignal CreateSignal(int signal, bool isDataStale)
    {
        return new EdgeSignal(
            Timestamp: DateTime.UtcNow,
            StructImbRaw: 0.0,
            StructImbZ: signal * 2.0, // Above threshold if signal != 0
            Signal: signal,
            IsContextWarm: true,
            IsDataStale: isDataStale,
            IsQualityGatePassed: true
        );
    }

    private TradeRecord CreateTradeRecord(decimal netPnL, DateTime exitTime)
    {
        return new TradeRecord
        {
            EntryTime = exitTime.AddMinutes(-1),
            ExitTime = exitTime,
            EntryPrice = 5000.00m,
            ExitPrice = 5000.00m + (netPnL / 12.50m * 0.25m),
            Direction = 1,
            Quantity = 1,
            PnLTicks = netPnL / 12.50m,
            GrossPnLDollars = netPnL + 4.50m,
            FeesDollars = 4.50m,
            NetPnLDollars = netPnL,
            ExitReason = DecisionReasons.TIME_EXIT,
            Duration = TimeSpan.FromMinutes(1)
        };
    }
}
