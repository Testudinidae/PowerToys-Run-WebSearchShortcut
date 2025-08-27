using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.History;

internal static class HistoryService
{
    public static string HistoryFilePath
    {
        get
        {
            var directory = Utilities.BaseSettingsPath("WebSearchShortcut");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "WebSearchShortcut_history.json");
        }
    }

    private static readonly Lock _lock = new();
    private static readonly HistoryStore _store = new();
    private static Dictionary<string, List<HistoryEntry>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string[]> _shortcutQueriesMap = new(StringComparer.OrdinalIgnoreCase);

    static HistoryService()
    {
        Reload();
    }

    public static string[] Get(string shortcutName)
    {
        lock (_lock)
        {
            return [.. _shortcutQueriesMap.GetValueOrDefault(shortcutName, [])];
        }
    }

    public static string[] Search(string shortcutName, string searchText)
    {
        lock (_lock)
        {
            return [
                .. Get(shortcutName)
                    .Where(query => query.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
            ];
        }
    }

    public static void Add(string shortcutName, string query)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(shortcutName, out var entries) || entries is null)
            {
                _cache[shortcutName] = entries = [];
            }

            entries.RemoveAll(entry => string.Equals(entry.Query, query, StringComparison.OrdinalIgnoreCase));

            entries.Insert(0, new HistoryEntry(query));

            RebuildShortcutQueriesMap();

            Save();

            ExtensionHost.LogMessage($"[HistoryService] Added/Updated: Shortcut: {shortcutName}, Query: \"{query}\"");
        }
    }

    public static void Remove(string shortcutName, string query)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(shortcutName, out var entries) || entries is null)
            {
                _cache[shortcutName] = entries = [];
            }

            entries.RemoveAll(entry => string.Equals(entry.Query, query, StringComparison.Ordinal));

            RebuildShortcutQueriesMap();

            Save();

            ExtensionHost.LogMessage($"[HistoryService] Delete: Shortcut: {shortcutName}, Query: \"{query}\"");
        }
    }

    public static void RemoveAll(string shortcutName)
    {
        lock (_lock)
        {
            _cache[shortcutName] = [];

            RebuildShortcutQueriesMap();

            Save();

            ExtensionHost.LogMessage($"[HistoryService] Delete: Shortcut: {shortcutName}");
        }
    }

    public static void Reload()
    {
        lock (_lock)
        {
            try
            {
                _cache = _store.LoadOrCreate(HistoryFilePath) ?? new(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage($"[HistoryService] Reload failed: {ex}");

                return;
            }

            RebuildShortcutQueriesMap();

            ExtensionHost.LogMessage($"[HistoryService] Reload");
        }
    }

    private static void Save()
    {
        try
        {
            _store.Save(HistoryFilePath, _cache);
        }
        catch (Exception ex)
        {
            ExtensionHost.LogMessage($"[HistoryService] Save failed: {ex}");
        }
    }

    private static void RebuildShortcutQueriesMap()
    {
        _shortcutQueriesMap.Clear();

        foreach (var (shortcutName, historyEntries) in _cache)
        {
            _shortcutQueriesMap[shortcutName] = [
                .. (historyEntries ?? Enumerable.Empty<HistoryEntry>())
                    .OrderByDescending(entry => entry.Timestamp)
                    .Select(entry => entry.Query)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
        }
    }
}
