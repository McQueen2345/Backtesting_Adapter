using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Risk Manager - Equity Tracking und Drawdown Protection.
/// PARTIAL CLASS: Equity.cs implementiert UpdateEquity, Drawdown und ResetDay.
/// </summary>
public sealed partial class RiskManager
{
    // ============================================================
    // EQUITY UPDATE
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// Muss bei JEDEM Tick aufgerufen werden mit Mark-to-Market Equity.
    /// Aktualisiert Peak und prüft Drawdown-Limit.
    /// </remarks>
    public void UpdateEquity(decimal currentEquity)
    {
        _currentEquity = currentEquity;

        // Peak Update (High Water Mark)
        if (currentEquity > _equityPeak)
        {
            _equityPeak = currentEquity;
        }

        // Drawdown Check
        CheckDrawdownLimit();
    }

    // ============================================================
    // DRAWDOWN PROTECTION
    // ============================================================

    /// <summary>
    /// Berechnet den aktuellen Drawdown (Peak - Current).
    /// </summary>
    /// <returns>Drawdown in Dollar (positiver Wert).</returns>
    public decimal GetCurrentDrawdown()
    {
        return _equityPeak - _currentEquity;
    }

    /// <summary>
    /// Prüft ob Drawdown-Limit erreicht wurde und triggert HardDisabled.
    /// </summary>
    /// <returns>True wenn HardDisabled getriggert wurde.</returns>
    private bool CheckDrawdownLimit()
    {
        // Bereits HardDisabled?
        if (_currentState == RiskState.HardDisabled)
            return true;

        decimal drawdown = GetCurrentDrawdown();

        if (drawdown >= _config.MaxIntradayDrawdown)
        {
            TriggerHardDisabled();
            return true;
        }

        return false;
    }

    // ============================================================
    // DAILY STATISTICS
    // ============================================================

    /// <inheritdoc />
    public DailyStatistics GetDailyStats()
    {
        int winCount = 0;
        int lossCount = 0;
        decimal grossPnL = 0m;
        decimal totalFees = 0m;
        decimal netPnL = 0m;

        foreach (var trade in _todayTrades)
        {
            if (trade.IsWin)
                winCount++;
            else
                lossCount++;

            grossPnL += trade.GrossPnLDollars;
            totalFees += trade.FeesDollars;
            netPnL += trade.NetPnLDollars;
        }

        return new DailyStatistics
        {
            TradeCount = _todayTrades.Count,
            WinCount = winCount,
            LossCount = lossCount,
            ConsecutiveLosses = _consecutiveLosses,
            GrossPnLDollars = grossPnL,
            TotalFeesDollars = totalFees,
            NetPnLDollars = netPnL,
            EquityPeakToday = _equityPeak,
            CurrentDrawdown = GetCurrentDrawdown()
        };
    }

    // ============================================================
    // RESET DAY
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// Setzt RiskManager für neuen Trading-Tag zurück.
    /// State wird auf Normal gesetzt (auch wenn vorher HardDisabled).
    /// </remarks>
    public void ResetDay(decimal dayStartEquity)
    {
        // Equity Reset
        _dayStartEquity = dayStartEquity;
        _equityPeak = dayStartEquity;
        _currentEquity = dayStartEquity;

        // State Reset
        _currentState = RiskState.Normal;
        _softPauseEndTime = null;

        // Trade Tracking Reset
        _consecutiveLosses = 0;
        _todayTrades.Clear();
    }
}
