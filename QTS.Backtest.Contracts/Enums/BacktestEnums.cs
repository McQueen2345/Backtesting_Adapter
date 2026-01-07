namespace QTS.Backtest.Contracts.Enums;

/// <summary>
/// Order-Typ für Backtest-Engine.
/// </summary>
public enum OrderType
{
    Buy,
    Sell,
    Flat
}

/// <summary>
/// Position-Richtung.
/// </summary>
public enum Direction
{
    Long,
    Short
}

/// <summary>
/// Speed-Modus für Backtest.
/// </summary>
public enum SpeedMode
{
    /// <summary>
    /// Maximale Geschwindigkeit (keine Verzögerung).
    /// </summary>
    Max,
    
    /// <summary>
    /// Echtzeit-Simulation.
    /// </summary>
    Realtime,
    
    /// <summary>
    /// Beschleunigt mit Multiplier.
    /// </summary>
    Fast
}

/// <summary>
/// Session-Typ für Trading.
/// </summary>
public enum SessionType
{
    /// <summary>
    /// Regular Trading Hours (09:30-16:00 ET).
    /// </summary>
    RTH,
    
    /// <summary>
    /// Extended Trading Hours (18:00-17:00 ET, Wrap-Around!).
    /// </summary>
    ETH,
    
    /// <summary>
    /// Volle 24 Stunden.
    /// </summary>
    Full24H,
    
    /// <summary>
    /// Benutzerdefiniert.
    /// </summary>
    Custom
}

/// <summary>
/// Emit-Policy für Adapter.
/// </summary>
public enum EmitPolicy
{
    /// <summary>
    /// Emit bei BBO-Änderung.
    /// </summary>
    OnBboChange,
    
    /// <summary>
    /// Emit pro Zeit-Bucket.
    /// </summary>
    OnTimeBucket,
    
    /// <summary>
    /// Beide Strategien kombiniert.
    /// </summary>
    OnBoth
}

/// <summary>
/// Tag-Status im DayIndex.
/// </summary>
public enum DayStatus
{
    /// <summary>
    /// Alle Quality-Gates bestanden.
    /// </summary>
    USABLE,
    
    /// <summary>
    /// Soft-Gates verletzt, aber nutzbar.
    /// </summary>
    DEGRADED,
    
    /// <summary>
    /// Hard-Gates verletzt, übersprungen.
    /// </summary>
    SKIPPED,
    
    /// <summary>
    /// Keine Snapshots emittiert.
    /// </summary>
    EMPTY_DAY
}

/// <summary>
/// Policy für fehlende Level-Angaben.
/// </summary>
public enum MissingLevelPolicy
{
    Accept,
    Reject
}
