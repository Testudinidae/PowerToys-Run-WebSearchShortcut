using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Windows.ApplicationModel;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace WebSearchShortcut.Shortcut;

internal sealed class ShortcutsChangedEventArgs(ShortcutEntry? before, ShortcutEntry? after) : EventArgs
{
    public ShortcutEntry? Before { get; } = before;
    public ShortcutEntry? After { get; } = after;
}

internal static class ShortcutService
{
    public static string ShortcutFilePath
    {
        get
        {
            var directory = Utilities.BaseSettingsPath("WebSearchShortcut");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "WebSearchShortcut.json");
        }
    }
    private static readonly Lock _lock = new();
    private static readonly ShortcutStore _store = new();
    private static readonly Lazy<List<ShortcutEntry>> _cache =
        new(
            () =>
            {
                var shortcuts = _store.LoadOrCreate(ShortcutFilePath) ?? [];

                if (EnsureIds(shortcuts))
                    _store.Save(ShortcutFilePath, shortcuts);

                return shortcuts;
            },
            LazyThreadSafetyMode.ExecutionAndPublication
        );

    public static event EventHandler<ShortcutsChangedEventArgs>? ChangedEvent;

    public static IReadOnlyList<ShortcutEntry> GetShortcutsSnapshot()
    {
        lock (_lock)
        {
            var shortcuts = _cache.Value;

            var snapshot = new ShortcutEntry[shortcuts.Count];
            for (int i = 0; i < shortcuts.Count; i++)
                snapshot[i] = shortcuts[i] with { };

            return snapshot;
        }
    }

    public static void Add(ShortcutEntry shortcut)
    {
        ShortcutsChangedEventArgs args;
        ShortcutEntry addedShortcut;

        lock (_lock)
        {
            var shortcuts = _cache.Value;

            if (shortcuts.Exists(s => string.Equals(s.Id, shortcut.Id, StringComparison.Ordinal)))
            {
                throw new ArgumentException($"Shortcut with Id '{shortcut.Id}' already exists.", nameof(shortcut));
            }

            shortcuts.Add(shortcut with { });

            EnsureIds(shortcuts);

            addedShortcut = shortcuts[^1] with { };

            _store.Save(ShortcutFilePath, shortcuts);

            args = new ShortcutsChangedEventArgs(before: null, after: addedShortcut);
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] Add Shortcut {addedShortcut}");

        ChangedEvent?.Invoke(typeof(ShortcutService), args);
    }

    public static void Remove(string Id)
    {
        ShortcutsChangedEventArgs args;
        ShortcutEntry removedShortcut;

        lock (_lock)
        {
            var shortcuts = _cache.Value;
            var idx = shortcuts.FindIndex(s => s.Id == Id);

            if (idx < 0)
                throw new KeyNotFoundException($"Shortcut with Id '{Id}' was not found.");

            removedShortcut = shortcuts[idx];
            shortcuts.RemoveAt(idx);

            _store.Save(ShortcutFilePath, shortcuts);

            args = new ShortcutsChangedEventArgs(before: removedShortcut, after: null);
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] Remove Shortcut {removedShortcut}");

        ChangedEvent?.Invoke(typeof(ShortcutService), args);
    }

    public static void Update(ShortcutEntry shortcut)
    {
        ShortcutsChangedEventArgs args;
        ShortcutEntry updatedShortcut;

        lock (_lock)
        {
            var shortcuts = _cache.Value;
            var idx = shortcuts.FindIndex(s => s.Id == shortcut.Id);

            if (idx < 0)
                throw new KeyNotFoundException($"Shortcut with Id '{shortcut.Id}' was not found.");

            var before = shortcuts[idx];
            shortcuts[idx] = shortcut with { };
            updatedShortcut = shortcut with { };

            _store.Save(ShortcutFilePath, shortcuts);
            args = new ShortcutsChangedEventArgs(before: before, after: updatedShortcut);
        }

        ExtensionHost.LogMessage($"[WebSearchShortcut] Update Shortcut {updatedShortcut}");

        ChangedEvent?.Invoke(typeof(ShortcutService), args);
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
                shortcuts[i] = shortcut with { Id = id };
                modified = true;
            }
            seenIds.Add(shortcut.Id);
        }

        return modified;
    }

    private static string GenerateNewId()
    {
        string prefix = Package.Current.DisplayName;

        byte[] buffer = new byte[8];
        Random.Shared.NextBytes(buffer);
        ulong randomNumber = BitConverter.ToUInt64(buffer, 0);

        return $"{prefix}{randomNumber}";
    }
}
