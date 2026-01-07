using QTS.Edge.Core.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Interfaces;

/// <summary>
/// Interface für Risk Management.
/// Verwaltet Risk State, Gates und Daily Loss Limits.
/// </summary>
public interface IRiskManager
{
    /// <summary>
    /// Aktueller Risk State (Normal, SoftPaused, HardDisabled).
    /// </summary>
    RiskState CurrentState { get; }

    /// <summary>
    /// True wenn Entry erlaubt basierend auf State und SoftPause-Timeout.
    /// WICHTIG: Prüft NUR State + Timeout, NICHT Window/Spread!
    /// </summary>
    bool IsEntryAllowed { get; }

    /// <summary>
    /// Prüft ob Zeitpunkt innerhalb Trading-Fenster liegt (14:30-21:00 UTC).
    /// </summary>
    /// <param name="utcTime">Zu prüfender Zeitpunkt (UTC).</param>
    /// <returns>True wenn innerhalb Trading-Fenster.</returns>
    bool IsWithinTradingWindow(DateTime utcTime);

    /// <summary>
    /// Prüft ob Spread innerhalb Limit liegt.
    /// </summary>
    /// <param name="dom">DOM-Snapshot.</param>
    /// <returns>True wenn Spread <= MaxSpreadTicks.</returns>
    bool CheckSpreadGate(IDomSnapshot dom);

    /// <summary>
    /// Gibt den Grund zurück warum Entry blockiert ist.
    /// </summary>
    /// <returns>DecisionReason (TRADING_DISABLED, SOFT_PAUSED, etc.).</returns>
    string GetBlockReason();

    /// <summary>
    /// Zeichnet einen abgeschlossenen Trade auf und aktualisiert State.
    /// </summary>
    /// <param name="trade">Abgeschlossener Trade.</param>
    void RecordTrade(TradeRecord trade);

    /// <summary>
    /// Aktualisiert die aktuelle Equity (für Drawdown-Berechnung).
    /// Muss bei jedem Tick aufgerufen werden.
    /// </summary>
    /// <param name="currentEquity">Aktuelle Mark-to-Market Equity.</param>
    void UpdateEquity(decimal currentEquity);

    /// <summary>
    /// Gibt die aktuellen Tages-Statistiken zurück.
    /// </summary>
    /// <returns>DailyStatistics.</returns>
    DailyStatistics GetDailyStats();

    /// <summary>
    /// Berechnet das aktive Daily Loss Limit (STRICTEST-GUARD).
    /// </summary>
    /// <returns>Negativer Wert (z.B. -375 für $375 Limit).</returns>
    decimal GetDailyLossLimit();

    /// <summary>
    /// Setzt Risk Manager für neuen Tag zurück.
    /// </summary>
    /// <param name="dayStartEquity">Equity zu Beginn des Tages.</param>
    void ResetDay(decimal dayStartEquity);
}
