using QTS.Backtest.Adapter.Enums;

namespace QTS.Backtest.Adapter.Mapping;

/// <summary>
/// Registry for mapping raw TTD values to canonical enum values.
/// Mappings are configurable - defaults are set in constructor but can be overridden.
/// </summary>
public class MappingRegistry
{
    /// <summary>
    /// Maps mdt integer values to Side enum.
    /// Default: 1 → Bid, 2 → Ask
    /// </summary>
    public Dictionary<int, Side> MdtToSide { get; set; }

    /// <summary>
    /// Maps operation integer values to Operation enum.
    /// Default: 0 → Add, 1 → Update, 2 → Delete
    /// </summary>
    public Dictionary<int, Operation> OperationIntMapping { get; set; }

    /// <summary>
    /// Maps operation string values to Operation enum (case-insensitive).
    /// Default: "add" → Add, "update" → Update, "delete" → Delete
    /// </summary>
    public Dictionary<string, Operation> OperationStringMapping { get; set; }

    /// <summary>
    /// Creates a new MappingRegistry with default mappings.
    /// </summary>
    public MappingRegistry()
    {
        // Default mdt to Side mapping
        MdtToSide = new Dictionary<int, Side>
        {
            { 1, Side.Bid },
            { 2, Side.Ask }
        };

        // Default operation int mapping
        OperationIntMapping = new Dictionary<int, Operation>
        {
            { 0, Operation.Add },
            { 1, Operation.Update },
            { 2, Operation.Delete }
        };

        // Default operation string mapping (lowercase for case-insensitive lookup)
        OperationStringMapping = new Dictionary<string, Operation>(StringComparer.OrdinalIgnoreCase)
        {
            { "add", Operation.Add },
            { "update", Operation.Update },
            { "delete", Operation.Delete }
        };
    }

    /// <summary>
    /// Tries to map an mdt value to a Side.
    /// </summary>
    /// <param name="mdt">The mdt value to map.</param>
    /// <returns>The mapped Side, or null if unknown.</returns>
    public Side? TryMapMdtToSide(int mdt)
    {
        return MdtToSide.TryGetValue(mdt, out var side) ? side : null;
    }

    /// <summary>
    /// Tries to map a string operation value to an Operation enum.
    /// Case-insensitive matching.
    /// </summary>
    /// <param name="value">The operation string to map.</param>
    /// <returns>The mapped Operation, or null if unknown.</returns>
    public Operation? TryMapOperation(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return OperationStringMapping.TryGetValue(value, out var operation) ? operation : null;
    }

    /// <summary>
    /// Tries to map an integer operation value to an Operation enum.
    /// </summary>
    /// <param name="value">The operation integer to map.</param>
    /// <returns>The mapped Operation, or null if unknown.</returns>
    public Operation? TryMapOperation(int value)
    {
        return OperationIntMapping.TryGetValue(value, out var operation) ? operation : null;
    }

    /// <summary>
    /// Checks if an mdt value is known/mappable.
    /// </summary>
    /// <param name="mdt">The mdt value to check.</param>
    /// <returns>True if the mdt value can be mapped, false otherwise.</returns>
    public bool IsKnownMdt(int mdt)
    {
        return MdtToSide.ContainsKey(mdt);
    }

    /// <summary>
    /// Checks if an operation string value is known/mappable.
    /// Case-insensitive matching.
    /// </summary>
    /// <param name="value">The operation string to check.</param>
    /// <returns>True if the operation can be mapped, false otherwise.</returns>
    public bool IsKnownOperation(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return OperationStringMapping.ContainsKey(value);
    }

    /// <summary>
    /// Checks if an operation integer value is known/mappable.
    /// </summary>
    /// <param name="value">The operation integer to check.</param>
    /// <returns>True if the operation can be mapped, false otherwise.</returns>
    public bool IsKnownOperation(int value)
    {
        return OperationIntMapping.ContainsKey(value);
    }
}
