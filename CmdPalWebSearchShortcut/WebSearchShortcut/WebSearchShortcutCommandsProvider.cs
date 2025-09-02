// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Commands;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.History;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

public partial class WebSearchShortcutCommandsProvider : CommandProvider
{
    private static readonly SettingsManager _settingsManager = new();

    private readonly ICommandItem _addShortcutItem;
    private ICommandItem[] _topLevelCommands = [];
    private IFallbackCommandItem[] _fallbackCommands = [];

    public WebSearchShortcutCommandsProvider()
    {
        DisplayName = Resources.WebSearchShortcut_DisplayName;
        Icon = Icons.Logo;
        Settings = _settingsManager.Settings;

        _addShortcutItem = new CommandItem(
            new AddShortcutPage(null)
            {
                Name = Resources.AddShortcutItem_Name
            }
        )
        {
            Title = Resources.AddShortcutItem_Title,
            Icon = Icons.AddShortcut
        };

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

        Debug.Assert(
            before is not null || after is not null,
            "ShortcutsChanged: both Before and After are null (unexpected)."
        );

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
        _topLevelCommands = [
            _addShortcutItem,
            .. ShortcutService
                .GetShortcutsSnapshot()
                .Select(CreateCommandItem)
        ];
        _fallbackCommands = [
            .. ShortcutService
                .GetShortcutsSnapshot()
                .Select(shortcut => new FallbackSearchWebItem(shortcut))
        ];
    }

    private static async void UpdateIconUrlAsync(ShortcutEntry shortcut)
    {
        if (!string.IsNullOrWhiteSpace(shortcut.IconUrl)) return;

        var iconUrl = await IconService.UpdateIconUrlAsync(shortcut);

        ShortcutService.Update(shortcut);

        ExtensionHost.LogMessage($"[WebSearchShortcut] Updating icon URL for shortcut id={shortcut.Id} name=\"{shortcut.Name}\" url={shortcut.Url} to {iconUrl}");
    }

    private CommandItem CreateCommandItem(ShortcutEntry shortcut)
    {
        var searchWebPage = new SearchWebPage(shortcut, _settingsManager)
        {
            Name = StringFormatter.Format(Resources.ShortcutItem_NameTemplate, new() { ["shortcut"] = shortcut.Name })
        };

        var openHomepageCommand = new CommandContextItem(
            new OpenHomePageCommand(shortcut)
            {
                Name = StringFormatter.Format(Resources.OpenHomepageItem_NameTemplate, new() { ["shortcut"] = shortcut.Name })
            }
        )
        {
            Title = StringFormatter.Format(Resources.OpenHomepageItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            Icon = Icons.Home
        };

        var editShortcutPage = new AddShortcutPage(shortcut)
        {
            Name = StringFormatter.Format(Resources.ShortcutItem_NameTemplate, new() { ["shortcut"] = shortcut.Name })
        };

        var editCommand = new CommandContextItem(
            new AddShortcutPage(shortcut)
            {
                Name = StringFormatter.Format(Resources.EditShortcutItem_NameTemplate, new() { ["shortcut"] = shortcut.Name }),
            }
        )
        {
            Title = StringFormatter.Format(Resources.EditShortcutItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            Icon = Icons.Edit
        };

        var deleteCommand = new CommandContextItem(
            title: StringFormatter.Format(Resources.DeleteShortcutItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            name: $"[UNREACHABLE] DeleteCommand.Name - shortcut='{shortcut.Name}'",
            action: null,
            result: CommandResult.Confirm(
                new ConfirmationArgs()
                {
                    Title = StringFormatter.Format(Resources.DeleteShortcutConfirm_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
                    Description = !ShortcutService.ReadOnlyMode
                        ? StringFormatter.Format(Resources.DeleteShortcutConfirm_DescriptionTemplate, new() { ["shortcut"] = shortcut.Name })
                        : StringFormatter.Format(Resources.DeleteShortcutConfirm_DescriptionTemplate_ReadOnlyMode, new() { ["shortcut"] = shortcut.Name, ["filePath"] = ShortcutService.ShortcutFilePath }),
                    PrimaryCommand = new AnonymousCommand(() => ShortcutService.Remove(shortcut.Id))
                    {
                        Name = StringFormatter.Format(Resources.DeleteShortcutConfirm_ButtonTemplate, new() { ["shortcut"] = shortcut.Name }),
                        Result = CommandResult.GoHome()
                    },
                    IsPrimaryCommandCritical = true
                }
            )
        )
        {
            Icon = Icons.Delete,
            IsCritical = true
        };

        var clearHistoryCommand = new CommandContextItem(
            title: StringFormatter.Format(Resources.ClearHistory_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            name: $"[UNREACHABLE] ClearHistory.Name - shortcut='{shortcut.Name}'",
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
            Title = StringFormatter.Format(Resources.ShortcutItem_TitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            Subtitle = StringFormatter.Format(Resources.ShortcutItem_SubtitleTemplate, new() { ["shortcut"] = shortcut.Name }),
            Icon = IconService.GetIconInfo(shortcut),
            MoreCommands = [openHomepageCommand, clearHistoryCommand, editCommand, deleteCommand]
        };

        return commandItem;
    }
}
