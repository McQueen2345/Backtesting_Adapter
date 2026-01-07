namespace QTS.Backtest.Adapter.Configuration;

/// <summary>
/// Validates AdapterConfig instances.
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// Validates the configuration and throws if invalid.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when config values are invalid.</exception>
    public static void Validate(AdapterConfig config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config), "Config cannot be null.");
        }

        if (config.BucketMs <= 0)
        {
            throw new ArgumentException("BucketMs must be greater than 0.", nameof(config));
        }

        if (config.StaleThresholdSoftMs <= 0)
        {
            throw new ArgumentException("StaleThresholdSoftMs must be greater than 0.", nameof(config));
        }

        if (config.StaleThresholdHardMs < config.StaleThresholdSoftMs)
        {
            throw new ArgumentException(
                $"StaleThresholdHardMs ({config.StaleThresholdHardMs}) must be >= StaleThresholdSoftMs ({config.StaleThresholdSoftMs}).",
                nameof(config));
        }

        if (config.LevelAccept is null)
        {
            throw new ArgumentException("LevelAccept cannot be null.", nameof(config));
        }

        if (config.LevelAccept.Count == 0)
        {
            throw new ArgumentException("LevelAccept cannot be empty.", nameof(config));
        }
    }

    /// <summary>
    /// Tries to validate the configuration without throwing.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <param name="error">The error message if validation fails, null otherwise.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool TryValidate(AdapterConfig config, out string? error)
    {
        error = null;

        if (config is null)
        {
            error = "Config cannot be null.";
            return false;
        }

        if (config.BucketMs <= 0)
        {
            error = "BucketMs must be greater than 0.";
            return false;
        }

        if (config.StaleThresholdSoftMs <= 0)
        {
            error = "StaleThresholdSoftMs must be greater than 0.";
            return false;
        }

        if (config.StaleThresholdHardMs < config.StaleThresholdSoftMs)
        {
            error = $"StaleThresholdHardMs ({config.StaleThresholdHardMs}) must be >= StaleThresholdSoftMs ({config.StaleThresholdSoftMs}).";
            return false;
        }

        if (config.LevelAccept is null)
        {
            error = "LevelAccept cannot be null.";
            return false;
        }

        if (config.LevelAccept.Count == 0)
        {
            error = "LevelAccept cannot be empty.";
            return false;
        }

        return true;
    }
}
