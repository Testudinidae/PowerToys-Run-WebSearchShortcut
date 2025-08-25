// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

public partial class WebSearchShortcutCommandsProvider : CommandProvider
{
    private readonly AddShortcutPage _addShortcutPage = new(null);
    private ICommandItem[] _topLevelCommands = [];

    public WebSearchShortcutCommandsProvider()
    {
        DisplayName = Resources.WebSearchShortcut_DisplayName;
        Icon = IconHelpers.FromRelativePath("Assets\\Search.png");

        ShortcutService.ChangedEvent += OnShortcutsChanged;
    }

    public override ICommandItem[] TopLevelCommands()
    {
        if (_topLevelCommands.Length == 0)
            ReloadCommands();

        return _topLevelCommands;
    }

    private void OnShortcutsChanged(object? sender, ShortcutsChangedEventArgs args)
    {
        var before = args.Before;
        var after = args.After;

        ReloadCommands();

        RaiseItemsChanged(0);

        if (after is null) return;
        if (before == after) return;

        UpdateIconUrlAsync(after);
    }

    private void ReloadCommands()
    {
        List<CommandItem> items = [new CommandItem(_addShortcutPage)];

        items.AddRange(
            ShortcutService
                .GetShortcutsSnapshot()
                .Select(CreateCommandItem)
        );

        _topLevelCommands = [.. items];
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
        var editCommand = new CommandContextItem(new AddShortcutPage(shortcut))
        {
            Icon = Icons.Edit
        };

        var deleteCommand = new CommandContextItem(
            title: Resources.SearchShortcut_DeleteTitle,
            name: Resources.SearchShortcut_DeleteName,
            action: () => ShortcutService.Remove(shortcut.Id),
            result: CommandResult.KeepOpen()
        )
        {
            Icon = Icons.Delete,
            IsCritical = true
        };

        var commandItem = new CommandItem(new SearchWebPage(shortcut))
        {
            Subtitle = StringFormatter.Format(Resources.SearchShortcut_SubtitleTemplate, new() { ["engine"] = shortcut.Name }),
            MoreCommands = [editCommand, deleteCommand]
        };

        return commandItem;
    }
}
