using QTS.Edge.Core.Interfaces;
using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Interfaces;

/// <summary>
/// Hauptinterface für das Trade-Management Modul.
/// Orchestriert Position Tracking, Risk Management und Trade Execution.
/// </summary>
public interface ITradeManager
{
    /// <summary>
    /// Verarbeitet einen Tick und gibt eine Trading-Entscheidung zurück.
    /// </summary>
    /// <param name="signal">Edge-Signal vom Algorithmus.</param>
    /// <param name="dom">DOM-Snapshot (Zeitquelle!).</param>
    /// <param name="currentEquity">Aktuelle Mark-to-Market Equity.</param>
    /// <returns>Trading-Entscheidung.</returns>
    TradeDecision ProcessTick(IEdgeSignal signal, IDomSnapshot dom, decimal currentEquity);

    /// <summary>
    /// Erzwingt einen Exit mit angegebenem Grund.
    /// </summary>
    /// <param name="dom">DOM-Snapshot für Preis und Timestamp.</param>
    /// <param name="reason">Grund für den Exit (z.B. ADAPTER_DISCONNECT).</param>
    /// <returns>Exit-Decision oder Hold wenn keine Position offen.</returns>
    TradeDecision ForceExit(IDomSnapshot dom, string reason);

    /// <summary>
    /// Setzt den Tag zurück (neuer Trading-Tag).
    /// 2-Stage: Stage-1 gibt Exit(DAY_RESET) wenn Position offen,
    /// Stage-2 gibt Hold(DAY_RESET_COMPLETE) wenn Position FLAT.
    /// </summary>
    /// <param name="dayStartEquity">Equity zu Beginn des neuen Tages.</param>
    /// <param name="dom">DOM-Snapshot für Preis und Timestamp.</param>
    /// <returns>Trading-Entscheidung.</returns>
    TradeDecision ResetDay(decimal dayStartEquity, IDomSnapshot dom);

    /// <summary>
    /// Aktuelle Position.
    /// </summary>
    Position CurrentPosition { get; }

    /// <summary>
    /// Aktueller Risk State.
    /// </summary>
    RiskState CurrentRiskState { get; }

    /// <summary>
    /// Tages-Statistiken.
    /// </summary>
    DailyStatistics TodayStats { get; }

    /// <summary>
    /// Liste aller Trades heute.
    /// </summary>
    IReadOnlyList<TradeRecord> TodayTrades { get; }
}
