using QTS.Edge.Core.Interfaces;

namespace QTS.Backtest.Contracts.Interfaces;

/// <summary>
/// Quelle für DOM-Snapshots (Adapter-Output, Engine-Input).
/// </summary>
public interface ISnapshotSource
{
    /// <summary>
    /// Liefert einen Stream von DOM-Snapshots.
    /// Alle Timestamps sind UTC!
    /// </summary>
    IEnumerable<IDomSnapshot> GetSnapshots();
    
    /// <summary>
    /// Liefert den DayIndex mit Quality-Status pro Tag.
    /// </summary>
    DayIndex? GetDayIndex();
}

/// <summary>
/// Index über alle Tage mit Quality-Status.
/// </summary>
public class DayIndex
{
    public List<DayIndexEntry> Entries { get; } = new();
    
    public IEnumerable<DayIndexEntry> UsableDays => 
        Entries.Where(e => e.Status == Enums.DayStatus.USABLE);
}

/// <summary>
/// Ein Eintrag im DayIndex.
/// </summary>
public class DayIndexEntry
{
    public required DateTime Date { get; init; }
    public required Enums.DayStatus Status { get; init; }
    public required QualityMetrics Metrics { get; init; }
    public string? FilePath { get; init; }
}

/// <summary>
/// Quality-Metriken für einen Tag.
/// </summary>
public class QualityMetrics
{
    public int RowsParsed { get; set; }
    public int ParseErrors { get; set; }
    public int LevelRejects { get; set; }
    public int MissingLevelAccepts { get; set; }
    public int DedupSkips { get; set; }
    public int EmptyBookSkips { get; set; }
    public int EventsProcessed { get; set; }
    public int SnapshotsEmitted { get; set; }
    public int CrossedBookCount { get; set; }
    public int StaleSoftCount { get; set; }
    public int StaleHardCount { get; set; }
    
    public decimal CrossedRate => SnapshotsEmitted > 0 
        ? (decimal)CrossedBookCount / SnapshotsEmitted 
        : 0m;
}
