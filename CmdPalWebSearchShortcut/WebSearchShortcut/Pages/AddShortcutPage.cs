using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.ApplicationModel;
using WebSearchShortcut.Properties;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

internal sealed partial class AddShortcutPage : ContentPage
{
    private readonly ShortcutEntry? _shortcut;

    public AddShortcutPage(ShortcutEntry? shortcut)
    {
        _shortcut = shortcut;

        bool isAdd = shortcut is null;

        Id = $"{Package.Current.DisplayName}1";
        Icon = IconHelpers.FromRelativePath("Assets\\SearchAdd.png");
        Title = isAdd ? Resources.AddShortcut_AddTitle : Resources.SearchShortcut_EditTitle;
        Name = isAdd ? Resources.AddShortcut_AddName : Resources.SearchShortcut_EditName;
    }

    public override IContent[] GetContent()
    {
        return [new AddShortcutForm(_shortcut)];
    }
}
