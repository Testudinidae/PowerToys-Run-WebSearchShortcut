using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Browsers;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.History;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut.Commands;

internal sealed partial class SearchWebCommand : InvokableCommand
{
    public new string Id
    {
        get => base.Id;
        set => base.Id = value;
    }
    public string Query { get; internal set; } = string.Empty;
    private readonly ShortcutEntry _shortcut;
    private readonly BrowserExecutionInfo _browserInfo;

    public SearchWebCommand(ShortcutEntry shortcut, string query)
    {
        Name = StringFormatter.Format(Resources.SearchQuery_NameTemplate, new() { ["engine"] = shortcut.Name, ["query"] = query });
        Icon = Icons.Search;
        Query = query;
        _shortcut = shortcut;
        _browserInfo = new BrowserExecutionInfo(shortcut);
    }

    public override CommandResult Invoke()
    {
        if (!ShellHelpers.OpenCommandInShell(_browserInfo.Path, _browserInfo.ArgumentsPattern, ShortcutEntry.GetSearchUrl(_shortcut, Query)))
        {
            // TODO GH# 138 --> actually display feedback from the extension somewhere.
            return CommandResult.KeepOpen();
        }

        if (_shortcut.RecordHistory ?? true)
            HistoryService.Add(_shortcut.Name, Query);

        return CommandResult.Dismiss();
    }
}
