using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Setting;

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

    private static readonly HistoryStorage _storage = new();
    private static readonly Dictionary<string, string[]> _shortcutQueriesCache = new(StringComparer.OrdinalIgnoreCase);

    private static bool _initialized;

    private static int MaxHistoryPerShortcut => SettingsHub.MaxHistoryPerShortcut;

    private static readonly Lock _lock = new();

    static HistoryService()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref _initialized, true, false))
            return;

        SettingsHub.SettingsChanged += (_, __) => Reload();

        Reload();
    }

    public static IReadOnlyList<string> Get(string shortcutName)
    {
        lock (_lock)
        {
            return [.. _shortcutQueriesCache.GetValueOrDefault(shortcutName, [])];
        }
    }

    public static IReadOnlyList<string> Search(string shortcutName, string searchText)
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
            if (!_storage.Data.TryGetValue(shortcutName, out var historyEntries) || historyEntries is null)
                _storage.Data[shortcutName] = historyEntries = [];

            historyEntries.Insert(0, new HistoryEntry(query));
            historyEntries.Sort((entryA, entryB) => entryB.Timestamp.CompareTo(entryA.Timestamp));

            if (historyEntries.Count > MaxHistoryPerShortcut)
                historyEntries.RemoveRange(MaxHistoryPerShortcut, historyEntries.Count - MaxHistoryPerShortcut);

            RebuildShortcutQueriesMap();

            Save();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Add Query shortcut=\"{shortcutName}\" query=\"{query}\"");
    }

    public static void Remove(string shortcutName, string query)
    {
        lock (_lock)
        {
            if (!_storage.Data.TryGetValue(shortcutName, out var historyEntries) || historyEntries is null)
                return;

            historyEntries.RemoveAll(entry => string.Equals(entry.Query, query, StringComparison.Ordinal));

            RebuildShortcutQueriesMap();

            Save();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Delete Query shortcut=\"{shortcutName}\" query=\"{query}\"");
    }

    public static void RemoveAll(string shortcutName)
    {
        lock (_lock)
        {
            _storage.Data[shortcutName] = [];

            RebuildShortcutQueriesMap();

            Save();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Clear shortcut=\"{shortcutName}\"");
    }

    public static void Reload()
    {
        lock (_lock)
        {
            HistoryStorage storage;
            try
            {
                storage = HistoryStorage.ReadFromFile(HistoryFilePath);
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = $"[WebSearchShortcut] History: Reload failed: {ex}", State = MessageState.Error });

                return;
            }

            _storage.Data = storage?.Data ?? [];

            bool modified = false;
            foreach (var (shortcutName, historyEntries) in _storage.Data)
            {
                if (historyEntries is null)
                    continue;

                if (historyEntries.Count > MaxHistoryPerShortcut)
                {
                    modified = true;

                    historyEntries.Sort((entryA, entryB) => entryB.Timestamp.CompareTo(entryA.Timestamp));
                    historyEntries.RemoveRange(MaxHistoryPerShortcut, historyEntries.Count - MaxHistoryPerShortcut);
                }
            }
            if (modified)
                Save();

            RebuildShortcutQueriesMap();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Reload succeeded");
    }

    private static void Save()
    {
        try
        {
            HistoryStorage.WriteToFile(HistoryFilePath, _storage);
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
        _shortcutQueriesCache.Clear();

        foreach (var (shortcutName, historyEntries) in _storage.Data)
        {
            _shortcutQueriesCache[shortcutName] = [
                .. (historyEntries ?? [])
                    .OrderByDescending(entry => entry.Timestamp)
                    .Select(entry => entry.Query)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
        }
    }
}
