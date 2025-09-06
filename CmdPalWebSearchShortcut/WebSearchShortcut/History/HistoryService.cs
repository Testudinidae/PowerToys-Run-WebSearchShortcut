using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    private static int? MaxHistoryPerShortcut => SettingsHub.MaxHistoryPerShortcut;
    private static TimeSpan? MaxHistoryLifetime => SettingsHub.MaxHistoryLifetime;

    private static readonly Lock _lock = new();
    private static readonly SemaphoreSlim _saveGate = new(1, 1);

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
            if (!_storage.Data.TryGetValue(shortcutName, out var historyEntries) || historyEntries is null)
                return [.. _shortcutQueriesCache.GetValueOrDefault(shortcutName, [])];

            if (Prune(historyEntries))
            {
                RebuildShortcutQueriesMap(shortcutName);

                Save();
            }

            return [.. _shortcutQueriesCache.GetValueOrDefault(shortcutName, [])];
        }
    }

    public static IReadOnlyList<string> SearchCache(string shortcutName, string searchText)
    {
        lock (_lock)
        {
            return [
                .. _shortcutQueriesCache.GetValueOrDefault(shortcutName, [])
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

            Prune(historyEntries);

            RebuildShortcutQueriesMap(shortcutName);

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

            RebuildShortcutQueriesMap(shortcutName);

            Save();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Delete Query shortcut=\"{shortcutName}\" query=\"{query}\"");
    }

    public static void RemoveAll(string shortcutName)
    {
        lock (_lock)
        {
            _storage.Data[shortcutName] = [];

            RebuildShortcutQueriesMap(shortcutName);

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

                if (EnsureSortedByTimestampDesc(historyEntries))
                    modified = true;

                if (Prune(historyEntries))
                    modified = true;
            }
            if (modified)
                Save();

            RebuildShortcutQueriesMap();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Reload succeeded");
    }

    private static void Save()
    {
        HistoryStorage snapshot = new()
        {
            Data = new Dictionary<string, List<HistoryEntry>>(_storage.Data.Comparer)
        };

        foreach (var (shortcutName, historyEntries) in _storage.Data)
        {
            snapshot.Data[shortcutName] = [.. historyEntries];
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _saveGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    HistoryStorage.WriteToFile(HistoryFilePath, snapshot);
                }
                finally
                {
                    _saveGate.Release();
                }

            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage { Message = $"[WebSearchShortcut] History: Save failed: {ex}", State = MessageState.Error });

                return;
            }

            ExtensionHost.LogMessage("[WebSearchShortcut] History: Save succeeded");
        });
    }

    private static bool Prune(List<HistoryEntry> historyEntries)
    {
        bool modified = false;

        if (MaxHistoryLifetime is TimeSpan maxLifeTime)
        {
            var cutoffTime = DateTimeOffset.UtcNow - maxLifeTime;
            int firstOld = historyEntries.FindLastIndex(entry => entry.Timestamp >= cutoffTime) + 1;

            if (firstOld < historyEntries.Count)
            {
                historyEntries.RemoveRange(firstOld, historyEntries.Count - firstOld);

                modified = true;
            }
        }
        else if (MaxHistoryPerShortcut is int max)
        {
            if(historyEntries.Count > max)
            {
                historyEntries.RemoveRange(max, historyEntries.Count - max);

                modified = true;
            }
        }

        return modified;
    }

    private static bool EnsureSortedByTimestampDesc(List<HistoryEntry> HistoryEntries)
    {
        for (int i = 1; i < HistoryEntries.Count; i++)
        {
            if (HistoryEntries[i - 1].Timestamp < HistoryEntries[i].Timestamp)
            {
                HistoryEntries.Sort((entryA, entryB) => entryB.Timestamp.CompareTo(entryA.Timestamp));

                return true;
            }
        }

        return false;
    }

    private static void RebuildShortcutQueriesMap()
    {
        _shortcutQueriesCache.Clear();

        foreach (var (shortcutName, historyEntries) in _storage.Data)
        {
            _shortcutQueriesCache[shortcutName] = [
                .. (historyEntries ?? [])
                    .Select(entry => entry.Query)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
        }
    }

    private static void RebuildShortcutQueriesMap(string shortcutName)
    {
        if (!_storage.Data.TryGetValue(shortcutName, out var historyEntries) || historyEntries is null)
        {
            _shortcutQueriesCache[shortcutName] = [];

            return;
        }

        _shortcutQueriesCache[shortcutName] = [
            .. historyEntries
                .Select(entry => entry.Query)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }
}
