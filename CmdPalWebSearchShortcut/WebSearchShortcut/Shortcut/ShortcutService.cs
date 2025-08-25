using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Windows.ApplicationModel;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.Shortcut;

internal static class ShortcutService
{
    private static readonly string _path = GetShortcutsJsonPath();
    private static readonly Lock _lock = new();
    private static readonly ShortcutStore _store = new();
    private static readonly Lazy<List<ShortcutEntry>> _cache =
        new(
            () =>
            {
                var shortcuts = _store.LoadOrCreate(_path);

                if (EnsureIds(shortcuts))
                    _store.Save(_path, shortcuts);

                return shortcuts;
            },
            LazyThreadSafetyMode.ExecutionAndPublication
        );

    public static IReadOnlyList<ShortcutEntry> GetShortcutsSnapshot()
    {
        lock (_lock)
        {
            var shortcuts = _cache.Value;

            var snapshot = new ShortcutEntry[shortcuts.Count];
            for (int i = 0; i < shortcuts.Count; i++)
                snapshot[i] = shortcuts[i].Copy();

            return snapshot;
        }
    }

    public static ShortcutEntry Add(ShortcutEntry shortcut)
    {
        ShortcutEntry addedShortcut;

        lock (_lock)
        {
            var shortcuts = _cache.Value;

            if (shortcuts.Exists(s => string.Equals(s.Id, shortcut.Id, StringComparison.Ordinal)))
            {
                throw new ArgumentException($"Shortcut with Id '{shortcut.Id}' already exists.", nameof(shortcut));
            }

            shortcuts.Add(shortcut.Copy());

            EnsureIds(shortcuts);

            addedShortcut = shortcuts[^1].Copy();

            _store.Save(_path, shortcuts);
        }

        return addedShortcut;
    }

    public static void Remove(string Id)
    {
        lock (_lock)
        {
            var shortcuts = _cache.Value;
            var idx = shortcuts.FindIndex(s => s.Id == Id);

            if (idx < 0)
                throw new KeyNotFoundException($"Shortcut with Id '{Id}' was not found.");

            var removedShortcut = shortcuts[idx];
            shortcuts.RemoveAt(idx);

            _store.Save(_path, shortcuts);
        }
    }

    public static void Update(ShortcutEntry shortcut)
    {
        lock (_lock)
        {
            var shortcuts = _cache.Value;
            var idx = shortcuts.FindIndex(s => s.Id == shortcut.Id);

            if (idx < 0)
                throw new KeyNotFoundException($"Shortcut with Id '{shortcut.Id}' was not found.");

            var before = shortcuts[idx];
            shortcuts[idx] = shortcut.Copy();

            _store.Save(_path, shortcuts);
        }
    }

    internal static string GetShortcutsJsonPath()
    {
        string directory = Utilities.BaseSettingsPath("WebSearchShortcut");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "WebSearchShortcut.json");
    }

    private static bool EnsureIds(List<ShortcutEntry> shortcuts)
    {
        bool modified = false;

        HashSet<string> seenIds = [];
        for (int i = 0; i < shortcuts.Count; i++)
        {
            var shortcut = shortcuts[i];
            string id = shortcut.Id;
            if (string.IsNullOrWhiteSpace(id) || seenIds.Contains(id))
            {
                id = GenerateNewId();
                shortcuts[i] = shortcut.WithId(id);
                modified = true;
            }
            seenIds.Add(shortcut.Id);
        }

        return modified;
    }

    private static string GenerateNewId()
    {
        string prefix = Package.Current.Id.FamilyName;

        byte[] buffer = new byte[8];
        Random.Shared.NextBytes(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);

        return $"{prefix}!App!ID{randomNumber}";
    }
}
