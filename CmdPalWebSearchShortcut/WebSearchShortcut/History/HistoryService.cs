using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.History;

internal static class HistoryService
{
    public static string HistoryFilePath => Path.Combine(Utilities.BaseSettingsPath("WebSearchShortcut"), "WebSearchShortcut_history.json");

    private static readonly HistoryStore _store = new();
    private static Dictionary<string, List<HistoryEntry>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string[]> _shortcutQueriesMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock _lock = new();

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
                _cache[shortcutName] = entries = [];

            entries.Insert(0, new HistoryEntry(query));

            RebuildShortcutQueriesMap();

            Save();

            ExtensionHost.LogMessage($"[WebSearchShortcut] History: Add Query shortcut=\"{shortcutName}\" query=\"{query}\"");
        }
    }

    public static void Remove(string shortcutName, string query)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(shortcutName, out var entries) || entries is null)
                return;

            entries.RemoveAll(entry => string.Equals(entry.Query, query, StringComparison.Ordinal));

            RebuildShortcutQueriesMap();

            Save();

            ExtensionHost.LogMessage($"[WebSearchShortcut] History: Delete Query shortcut=\"{shortcutName}\" query=\"{query}\"");
        }
    }

    public static void RemoveAll(string shortcutName)
    {
        lock (_lock)
        {
            _cache[shortcutName] = [];

            RebuildShortcutQueriesMap();

            Save();

            ExtensionHost.LogMessage($"[WebSearchShortcut] History: Claer shortcut=\"{shortcutName}\"");
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
                ExtensionHost.LogMessage(new LogMessage() { Message = $"[WebSearchShortcut] History: Reload failed: {ex}", State = MessageState.Error });

                return;
            }

            RebuildShortcutQueriesMap();

            ExtensionHost.LogMessage($"[WebSearchShortcut] History: Reload succeeded");
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
            ExtensionHost.LogMessage(new LogMessage() { Message = $"[WebSearchShortcut] History: Save failed: {ex}", State = MessageState.Error });

            return;
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Save succeeded");
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
