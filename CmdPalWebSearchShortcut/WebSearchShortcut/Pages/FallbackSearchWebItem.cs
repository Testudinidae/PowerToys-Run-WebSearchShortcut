using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Commands;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Services;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

internal sealed partial class FallbackSearchWebItem : FallbackCommandItem
{
    private readonly SearchWebCommand _searchWebCommand;
    private readonly ShortcutEntry _shortcut;

    public FallbackSearchWebItem(ShortcutEntry shortcut)
        : base(new SearchWebCommand(shortcut, string.Empty) { Id = $"{shortcut.Id}.fallback" }, shortcut.Name)
    {
        _shortcut = shortcut;

        _searchWebCommand = (SearchWebCommand)Command!;
        _searchWebCommand.Name = string.Empty;

        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = IconService.GetIconInfo(shortcut);
        MoreCommands = [new CommandContextItem(new OpenHomePageCommand(_shortcut))];
    }

    public override void UpdateQuery(string query)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(query);

        // Order matters: update command state before Title. (Verified 2025-08-21)
        // Title may be derived from/overwritten by Command.Name.
        // Set Command.Name before Title to avoid stale or inconsistent UI.
        _searchWebCommand.Name = isEmpty ? string.Empty : StringFormatter.Format(Resources.SearchQuery_NameTemplate, new() { ["engine"] = _shortcut.Name, ["query"] = query });
        _searchWebCommand.Query = query;

        Title = isEmpty ? string.Empty : query;
        Subtitle = isEmpty ? string.Empty : StringFormatter.Format(Resources.SearchQuery_SubtitleTemplate, new() { ["engine"] = _shortcut.Name, ["query"] = query });
    }
}
