using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Interfaces;

/// <summary>
/// Interface für Trade Journal.
/// Speichert und verwaltet Trade-Historie.
/// </summary>
public interface ITradeJournal
{
    /// <summary>
    /// Zeichnet einen Trade auf.
    /// </summary>
    /// <param name="trade">Abgeschlossener Trade.</param>
    void RecordTrade(TradeRecord trade);

    /// <summary>
    /// Gibt Trades im angegebenen Zeitraum zurück.
    /// </summary>
    /// <param name="from">Start-Zeitpunkt (optional).</param>
    /// <param name="to">End-Zeitpunkt (optional).</param>
    /// <returns>Liste der Trades.</returns>
    IReadOnlyList<TradeRecord> GetTrades(DateTime? from = null, DateTime? to = null);

    /// <summary>
    /// Gibt alle Trades von heute zurück.
    /// </summary>
    /// <returns>Liste der heutigen Trades.</returns>
    IReadOnlyList<TradeRecord> GetTodayTrades();

    /// <summary>
    /// Löscht alle Trades (für ResetDay oder Tests).
    /// </summary>
    void Clear();
}
