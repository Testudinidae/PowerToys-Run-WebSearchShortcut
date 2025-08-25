using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.ApplicationModel;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

internal sealed partial class AddShortcutPage : ContentPage
{
    private readonly AddShortcutForm _addShortcutForm;

    public AddShortcutPage(ShortcutEntry? shortcut)
    {
        var name = shortcut?.Name ?? string.Empty;
        var url = shortcut?.Url ?? string.Empty;
        var isAdd = string.IsNullOrEmpty(name) && string.IsNullOrEmpty(url);

        _addShortcutForm = new AddShortcutForm(shortcut);

        Id = $"{Package.Current.DisplayName}1";
        Icon = IconHelpers.FromRelativePath("Assets\\SearchAdd.png");
        Title = isAdd ? Resources.AddShortcut_AddTitle : Resources.SearchShortcut_EditTitle;
        Name = isAdd ? Resources.AddShortcut_AddName : Resources.SearchShortcut_EditName;
    }

    public override IContent[] GetContent() => [_addShortcutForm];
}
