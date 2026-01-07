using System;
using System.Collections.Generic;

namespace QTS.Backtest.Adapter.Mapping;

/// <summary>
/// SynonymRegistry für Spaltennamen: mappt beliebige Header-Spalten (Synonyme) auf Canonical-Namen.
/// Default-Synonyme werden im Konstruktor gesetzt, sind aber überschreibbar/erweiterbar.
/// </summary>
public sealed class SynonymRegistry
{
    // canonical -> set(synonyms-lower)
    private Dictionary<string, HashSet<string>> _synonyms;

    /// <summary>
    /// Exponiert die aktuelle Synonym-Tabelle (konfigurierbar).
    /// Hinweis: Werte sollten normalized (lower) gehalten werden.
    /// </summary>
    public Dictionary<string, HashSet<string>> Synonyms
    {
        get => _synonyms;
        set => _synonyms = value ?? throw new ArgumentNullException(nameof(value));
    }

    public SynonymRegistry(Dictionary<string, HashSet<string>>? synonyms = null)
    {
        _synonyms = synonyms ?? CreateDefault();
        NormalizeInPlace(_synonyms);
    }

    /// <summary>
    /// Liefert den Canonical-Namen für einen gegebenen Spaltennamen (oder null, wenn unbekannt).
    /// </summary>
    public string? GetCanonicalName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return null;

        var lower = columnName.Trim().ToLowerInvariant();

        foreach (var kvp in _synonyms)
        {
            var canonical = kvp.Key;      // canonical bleibt wie definiert (z.B. "timestamp")
            var syns = kvp.Value;         // enthält lower-Values
            if (syns.Contains(lower))
                return canonical;
        }

        return null;
    }

    private static Dictionary<string, HashSet<string>> CreateDefault()
    {
        // Defaults aus Task-Slicing/Spec
        return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "timestamp", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "timestamp", "time", "ts", "datetime", "date_time" } },
            { "mdt",       new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mdt", "message_type", "msg_type", "type" } },
            { "operation", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "operation", "op", "action", "update_action" } },
            { "price",     new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "price", "px", "prc" } },
            { "size",      new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "size", "qty", "quantity", "volume", "vol" } },
            { "depth",     new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "depth", "level", "depth_level", "price_level" } },
            { "level",     new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "level", "lvl", "book_level" } },
        };
    }

    private static void NormalizeInPlace(Dictionary<string, HashSet<string>> dict)
    {
        // Sicherstellen: alle Synonyme in lower-case (robust für spätere direkte HashSet Contains)
        foreach (var kvp in dict)
        {
            var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in kvp.Value)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                normalized.Add(s.Trim().ToLowerInvariant());
            }

            // Canonical immer enthalten
            if (!string.IsNullOrWhiteSpace(kvp.Key))
                normalized.Add(kvp.Key.Trim().ToLowerInvariant());

            kvp.Value.Clear();
            foreach (var s in normalized) kvp.Value.Add(s);
        }
    }
}
