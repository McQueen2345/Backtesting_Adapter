namespace QTS.Edge.TradeManagement.Services;

/// <summary>
/// Risk Manager - STRICTEST-GUARD Daily Loss Limit Berechnung.
/// PARTIAL CLASS: DailyLoss.cs implementiert GetDailyLossLimit und R-Berechnung.
/// </summary>
public sealed partial class RiskManager
{
    // ============================================================
    // STRICTEST-GUARD DAILY LOSS LIMIT
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// STRICTEST-GUARD: Gibt max(limits) zurück weil alle Limits NEGATIV sind.
    /// Beispiel: max(-375, -500) = -375 → strengeres Limit greift.
    /// </remarks>
    public decimal GetDailyLossLimit()
    {
        var limits = new List<decimal>();

        // 1. Percent of DayStartEquity
        if (_config.DailyLossLimitPct.HasValue)
        {
            decimal pctLimit = -(_dayStartEquity * (decimal)_config.DailyLossLimitPct.Value / 100m);
            limits.Add(pctLimit);
        }

        // 2. R-Multiple
        if (_config.DailyMaxLossR.HasValue)
        {
            decimal rLimit = -(_config.DailyMaxLossR.Value * GetR());
            limits.Add(rLimit);
        }

        // 3. Fixed Dollar (bereits negativ in Config!)
        if (_config.DailyLossLimitDollars.HasValue)
        {
            limits.Add(_config.DailyLossLimitDollars.Value);
        }

        // FAIL-FAST: Mindestens ein Limit muss aktiv sein
        // (Bereits in Config.Validate() geprüft, aber defensive Programmierung)
        if (limits.Count == 0)
        {
            throw new InvalidOperationException(
                "No daily loss limits configured. At least one limit must be active.");
        }

        // STRICTEST-GUARD: max() weil negativ → strengstes Limit!
        return limits.Max();
    }

    // ============================================================
    // R-BERECHNUNG
    // ============================================================

    /// <summary>
    /// Berechnet R = EmergencyStopTicks × TickValue × ContractSize.
    /// R ist der maximale Verlust pro Trade bei Emergency Stop.
    /// </summary>
    /// <returns>R-Wert in Dollar (positiv).</returns>
    public decimal GetR()
    {
        return _config.EmergencyStopTicks * _config.TickValue * _config.ContractSize;
    }

    // ============================================================
    // DAILY LOSS CHECK
    // ============================================================

    /// <summary>
    /// Prüft ob Daily Loss Limit erreicht wurde und triggert HardDisabled.
    /// Muss nach jedem Trade aufgerufen werden.
    /// </summary>
    /// <returns>True wenn HardDisabled getriggert wurde.</returns>
    public bool CheckDailyLossLimit()
    {
        // Bereits HardDisabled?
        if (_currentState == Models.RiskState.HardDisabled)
            return true;

        // Net PnL berechnen
        decimal netPnL = CalculateTodayNetPnL();

        // Gegen Limit prüfen (beides negativ!)
        decimal limit = GetDailyLossLimit();

        if (netPnL <= limit)
        {
            TriggerHardDisabled();
            return true;
        }

        return false;
    }

    // ============================================================
    // PNL HELPER
    // ============================================================

    /// <summary>
    /// Berechnet das Net PnL aller heutigen Trades.
    /// </summary>
    private decimal CalculateTodayNetPnL()
    {
        decimal total = 0m;
        foreach (var trade in _todayTrades)
        {
            total += trade.NetPnLDollars;
        }
        return total;
    }
}
