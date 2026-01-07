namespace QTS.Edge.TradeManagement.Configuration;

/// <summary>
/// Konfiguration für das Trade-Management Modul.
/// Alle Parameter sind konservativ für Phase 1 gesetzt.
/// </summary>
public sealed class TradeManagementConfig
{
    // ============================================================
    // EXIT PARAMETER
    // ============================================================

    /// <summary>
    /// Emergency Stop bei PnL <= -X Ticks pro Kontrakt.
    /// Default: -10 Ticks = -$125 bei ES.
    /// </summary>
    public int EmergencyStopTicks { get; init; } = 10;

    /// <summary>
    /// Time Exit nach X Sekunden in Position.
    /// Default: 300 Sekunden (5 Minuten).
    /// </summary>
    public int MaxPositionSeconds { get; init; } = 300;

    // ============================================================
    // COOLDOWN PARAMETER
    // ============================================================

    /// <summary>
    /// Normaler Cooldown nach Exit in Millisekunden.
    /// Default: 2000ms (2 Sekunden).
    /// </summary>
    public int CooldownNormalMs { get; init; } = 2000;

    /// <summary>
    /// Erhöhter Cooldown nach Emergency Stop in Millisekunden.
    /// Default: 10000ms (10 Sekunden).
    /// </summary>
    public int CooldownEscalatedMs { get; init; } = 10000;

    // ============================================================
    // POSITION PARAMETER
    // ============================================================

    /// <summary>
    /// Anzahl Kontrakte pro Trade.
    /// Phase 1: Immer 1 (kein Scaling).
    /// </summary>
    public int ContractSize { get; init; } = 1;

    // ============================================================
    // DAILY LOSS LIMITS (STRICTEST-GUARD)
    // ============================================================

    /// <summary>
    /// Daily Loss Limit als Prozent des DayStartEquity.
    /// null = nicht aktiv.
    /// </summary>
    public double? DailyLossLimitPct { get; init; } = null;

    /// <summary>
    /// Daily Loss Limit als R-Multiple.
    /// Default: 3R = 3 × $125 = $375.
    /// null = nicht aktiv.
    /// </summary>
    public int? DailyMaxLossR { get; init; } = 3;

    /// <summary>
    /// Daily Loss Limit als fixer Dollar-Betrag (Hard-Cap).
    /// WICHTIG: Negativer Wert (z.B. -500 für $500 Loss Limit).
    /// Default: -$500.
    /// null = nicht aktiv.
    /// </summary>
    public decimal? DailyLossLimitDollars { get; init; } = -500m;

    // ============================================================
    // RISK GATES
    // ============================================================

    /// <summary>
    /// Max Intraday Drawdown in Dollar.
    /// Bei Überschreitung: HardDisabled.
    /// Default: $750.
    /// </summary>
    public decimal MaxIntradayDrawdown { get; init; } = 750m;

    /// <summary>
    /// Max aufeinanderfolgende Verluste vor SoftPause.
    /// Default: 5.
    /// </summary>
    public int MaxConsecutiveLosses { get; init; } = 5;

    /// <summary>
    /// SoftPause Dauer in Minuten.
    /// Default: 10 Minuten.
    /// </summary>
    public int SoftPauseMinutes { get; init; } = 10;

    /// <summary>
    /// Max Spread in Ticks für Entry.
    /// Default: 2 Ticks.
    /// </summary>
    public int MaxSpreadTicks { get; init; } = 2;

    /// <summary>
    /// Max Trades pro Tag.
    /// Default: 50 Round-Turns.
    /// </summary>
    public int MaxTradesPerDay { get; init; } = 50;

    // ============================================================
    // TRADING WINDOW (RTH)
    // ============================================================

    /// <summary>
    /// Trading-Fenster Start (UTC).
    /// Default: 14:30 UTC = 09:30 ET.
    /// </summary>
    public TimeSpan TradingWindowStartUtc { get; init; } = new TimeSpan(14, 30, 0);

    /// <summary>
    /// Trading-Fenster Ende (UTC).
    /// Default: 21:00 UTC = 16:00 ET.
    /// </summary>
    public TimeSpan TradingWindowEndUtc { get; init; } = new TimeSpan(21, 0, 0);

    // ============================================================
    // ES FUTURES KONSTANTEN
    // ============================================================

    /// <summary>
    /// Tick Size für ES Futures.
    /// </summary>
    public decimal TickSize { get; init; } = 0.25m;

    /// <summary>
    /// Tick Value für ES Futures (USD pro Tick pro Kontrakt).
    /// </summary>
    public decimal TickValue { get; init; } = 12.50m;

    /// <summary>
    /// Commission pro Kontrakt (Round-Turn).
    /// </summary>
    public decimal CommissionPerContractRT { get; init; } = 4.50m;

    // ============================================================
    // VALIDATION
    // ============================================================

    /// <summary>
    /// Validiert die Konfiguration. Wirft Exception bei ungültigen Werten.
    /// FAIL-FAST: Mindestens ein DailyLoss Limit muss aktiv sein!
    /// </summary>
    public void Validate()
    {
        // Mindestens ein DailyLoss Limit muss aktiv sein
        bool hasAnyLimit = DailyLossLimitPct.HasValue
                        || DailyMaxLossR.HasValue
                        || DailyLossLimitDollars.HasValue;

        if (!hasAnyLimit)
        {
            throw new InvalidOperationException(
                "No daily loss limits configured. At least one limit must be active " +
                "(DailyLossLimitPct, DailyMaxLossR, or DailyLossLimitDollars).");
        }

        // Weitere Validierungen
        if (EmergencyStopTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(EmergencyStopTicks), "Must be > 0");

        if (MaxPositionSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxPositionSeconds), "Must be > 0");

        if (CooldownNormalMs < 0)
            throw new ArgumentOutOfRangeException(nameof(CooldownNormalMs), "Must be >= 0");

        if (CooldownEscalatedMs < 0)
            throw new ArgumentOutOfRangeException(nameof(CooldownEscalatedMs), "Must be >= 0");

        if (ContractSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(ContractSize), "Must be > 0");

        if (MaxIntradayDrawdown <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxIntradayDrawdown), "Must be > 0");

        if (MaxConsecutiveLosses <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxConsecutiveLosses), "Must be > 0");

        if (SoftPauseMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(SoftPauseMinutes), "Must be > 0");

        if (MaxSpreadTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxSpreadTicks), "Must be > 0");

        if (MaxTradesPerDay <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxTradesPerDay), "Must be > 0");

        if (TickSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(TickSize), "Must be > 0");

        if (TickValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(TickValue), "Must be > 0");

        if (CommissionPerContractRT < 0)
            throw new ArgumentOutOfRangeException(nameof(CommissionPerContractRT), "Must be >= 0");

        if (TradingWindowStartUtc >= TradingWindowEndUtc)
            throw new ArgumentException("TradingWindowStartUtc must be before TradingWindowEndUtc");
    }

    // ============================================================
    // FACTORY
    // ============================================================

    /// <summary>
    /// Erstellt eine Default-Konfiguration (Phase 1).
    /// </summary>
    public static TradeManagementConfig Default => new();
}
