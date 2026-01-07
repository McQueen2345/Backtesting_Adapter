using QTS.Backtest.Contracts.Enums;

namespace QTS.Backtest.Adapter.Configuration;

/// <summary>
/// Configuration for the Backtest Adapter.
/// </summary>
public class AdapterConfig
{
    #region Emit Policy

    /// <summary>
    /// Controls when snapshots are emitted.
    /// Default: OnBboChange
    /// </summary>
    public EmitPolicy EmitPolicy { get; set; } = EmitPolicy.OnBboChange;

    /// <summary>
    /// Time bucket size in milliseconds for OnTimeBucket policy.
    /// Default: 100ms
    /// </summary>
    public int BucketMs { get; set; } = 100;

    #endregion

    #region Stale Detection

    /// <summary>
    /// Soft threshold for stale data detection in milliseconds.
    /// Triggers warning but continues processing.
    /// Default: 500ms
    /// </summary>
    public int StaleThresholdSoftMs { get; set; } = 500;

    /// <summary>
    /// Hard threshold for stale data detection in milliseconds.
    /// Triggers error and may halt processing.
    /// Default: 5000ms
    /// </summary>
    public int StaleThresholdHardMs { get; set; } = 5000;

    #endregion

    #region Level Filter (v1.0.1)

    /// <summary>
    /// Set of accepted depth levels. Events with levels not in this set are filtered out.
    /// Default: { 2 } - only L2 data
    /// </summary>
    public HashSet<int> LevelAccept { get; set; } = new() { 2 };

    /// <summary>
    /// Policy for handling events with missing level information.
    /// Accept: Include events with null/missing level
    /// Reject: Exclude events with null/missing level
    /// Default: Accept
    /// </summary>
    public MissingLevelPolicy MissingLevelPolicy { get; set; } = MissingLevelPolicy.Accept;

    #endregion

    #region Deduplication (v1.0.1)

    /// <summary>
    /// Enable deduplication of consecutive identical events.
    /// Default: false
    /// </summary>
    public bool EnableDeduplication { get; set; } = false;

    #endregion

    #region Factory

    /// <summary>
    /// Creates a new AdapterConfig with default values.
    /// </summary>
    public static AdapterConfig Default => new();

    #endregion
}
