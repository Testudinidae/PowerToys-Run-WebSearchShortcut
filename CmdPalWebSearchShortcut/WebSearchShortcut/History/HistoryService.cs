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

    private static readonly Lock _lock = new();

    static HistoryService()
    {
        Reload();
    }

    public static IReadOnlyList<string> Get(string shortcutName)
    {
        lock (_lock)
        {
            return [.. _shortcutQueriesCache.GetValueOrDefault(shortcutName, [])];
        }
    }

    public static void Add(string shortcutName, string query)
    {
        lock (_lock)
        {
            if (!_storage.Data.TryGetValue(shortcutName, out var historyEntries) || historyEntries is null)
                _storage.Data[shortcutName] = historyEntries = [];

            historyEntries.Insert(0, new HistoryEntry(query));

            RebuildShortcutQueriesMap();

            Save();
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] History: Add Query shortcut=\"{shortcutName}\" query=\"{query}\"");
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
