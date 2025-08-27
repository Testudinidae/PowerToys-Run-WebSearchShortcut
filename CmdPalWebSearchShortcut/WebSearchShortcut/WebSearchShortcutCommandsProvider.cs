// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.History;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

public partial class WebSearchShortcutCommandsProvider : CommandProvider
{
    private readonly AddShortcutPage _addShortcutPage = new(null);
    private static readonly SettingsManager _settingsManager = new();
    private ICommandItem[] _topLevelCommands = [];
    private IFallbackCommandItem[] _fallbackCommands = [];

    public WebSearchShortcutCommandsProvider()
    {
        DisplayName = Resources.WebSearchShortcut_DisplayName;
        Icon = IconHelpers.FromRelativePath("Assets\\Search.png");
        Settings = _settingsManager.Settings;

        ShortcutService.ChangedEvent += OnShortcutsChanged;
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (_topLevelCommands.Length == 0)
            ReloadCommands();

        return _topLevelCommands;
    }

    public override IFallbackCommandItem[] FallbackCommands() => _fallbackCommands;

    private void OnShortcutsChanged(object? sender, ShortcutsChangedEventArgs args)
    {
        var before = args.Before;
        var after = args.After;

        if ((before?.Name, before?.Domain, before?.IconUrl) == (after?.Name, after?.Domain, after?.IconUrl))
            return;

        ReloadCommands();

        RaiseItemsChanged(0);

        if (after is null) return;
        if (before == after) return;

        UpdateIconUrlAsync(after);
    }

    private void ReloadCommands()
    {
        List<CommandItem> items = [new CommandItem(_addShortcutPage)];
        List<FallbackCommandItem> fallbackItem = [];

        items.AddRange(
            ShortcutService
                .GetShortcutsSnapshot()
                .Select(CreateCommandItem)
        );
        fallbackItem.AddRange(
            ShortcutService
                .GetShortcutsSnapshot()
                .Select(shortcut => new FallbackSearchWebItem(shortcut))
        );

        _topLevelCommands = [.. items];
        _fallbackCommands = [.. fallbackItem];
    }

    private static async void UpdateIconUrlAsync(ShortcutEntry shortcut)
    {
        if (!string.IsNullOrWhiteSpace(shortcut.IconUrl)) return;

        var url = await IconService.UpdateIconUrlAsync(shortcut);

        ShortcutService.Update(shortcut);

        ExtensionHost.LogMessage($"[WebSearchShortcut] Updating icon URL for shortcut {shortcut} to {url}");
    }

    private CommandItem CreateCommandItem(ShortcutEntry shortcut)
    {
        var searchWebPage = new SearchWebPage(shortcut, _settingsManager);

        var editCommand = new CommandContextItem(new AddShortcutPage(shortcut))
        {
            Icon = Icons.Edit
        };

        var deleteCommand = new CommandContextItem(
            title: Resources.SearchShortcut_DeleteName,
            name: Resources.SearchShortcut_DeleteName,
            action: () => ShortcutService.Remove(shortcut.Id),
            result: CommandResult.KeepOpen()
        )
        {
            Icon = Icons.Delete,
            IsCritical = true
        };

        var clearHistoryCommand = new CommandContextItem(
            title: StringFormatter.Format(Resources.SearchShortcut_ClearHistoryNameTemplate, new() { ["engine"] = shortcut.Name }),
            action: () =>
            {
                HistoryService.RemoveAll(shortcut.Name);
                searchWebPage.Rebuild();
            },
            result: CommandResult.KeepOpen()
        )
        {
            Icon = Icons.DeleteHistory,
            IsCritical = true
        };

        var commandItem = new CommandItem(searchWebPage)
        {
            Subtitle = StringFormatter.Format(Resources.SearchShortcut_SubtitleTemplate, new() { ["engine"] = shortcut.Name }),
            MoreCommands = [editCommand, clearHistoryCommand, deleteCommand]
        };

        return commandItem;
    }
}
