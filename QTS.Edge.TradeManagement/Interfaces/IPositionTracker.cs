using QTS.Edge.TradeManagement.Models;

namespace QTS.Edge.TradeManagement.Interfaces;

/// <summary>
/// Interface für Position Tracking.
/// Verwaltet Position State Machine, Entry/Exit und Cooldowns.
/// </summary>
public interface IPositionTracker
{
    /// <summary>
    /// Aktuelle Position.
    /// </summary>
    Position CurrentPosition { get; }

    /// <summary>
    /// Prüft ob Cooldown aktiv ist.
    /// </summary>
    /// <param name="currentTime">Aktueller Zeitpunkt (aus dom.Timestamp).</param>
    /// <returns>True wenn Cooldown noch aktiv.</returns>
    bool IsInCooldown(DateTime currentTime);

    /// <summary>
    /// Versucht einen Entry durchzuführen.
    /// </summary>
    /// <param name="signal">Signal: +1 (Long) oder -1 (Short).</param>
    /// <param name="midPrice">MidPrice für Entry.</param>
    /// <param name="timestamp">Zeitstempel des Entries.</param>
    /// <param name="quantity">Anzahl Kontrakte.</param>
    /// <returns>True wenn Entry erfolgreich (war FLAT).</returns>
    bool TryEntry(int signal, decimal midPrice, DateTime timestamp, int quantity);

    /// <summary>
    /// Prüft ob Exit-Bedingung erfüllt ist (Emergency, Time).
    /// </summary>
    /// <param name="midPrice">Aktueller MidPrice.</param>
    /// <param name="timestamp">Aktueller Zeitpunkt.</param>
    /// <returns>ExitResult mit ShouldExit und Reason.</returns>
    ExitResult CheckExit(decimal midPrice, DateTime timestamp);

    /// <summary>
    /// Führt Exit durch und erstellt TradeRecord.
    /// </summary>
    /// <param name="midPrice">Exit-Preis.</param>
    /// <param name="timestamp">Exit-Zeitpunkt.</param>
    /// <param name="reason">Exit-Grund.</param>
    /// <returns>Abgeschlossener TradeRecord.</returns>
    TradeRecord ExecuteExit(decimal midPrice, DateTime timestamp, string reason);

    /// <summary>
    /// Setzt Cooldown nach Exit.
    /// </summary>
    /// <param name="timestamp">Zeitpunkt des Exits.</param>
    /// <param name="isEmergencyStop">True wenn Emergency Stop (längerer Cooldown).</param>
    void SetCooldown(DateTime timestamp, bool isEmergencyStop);

    /// <summary>
    /// Setzt Position Tracker zurück (neuer Tag).
    /// </summary>
    void Reset();
}
