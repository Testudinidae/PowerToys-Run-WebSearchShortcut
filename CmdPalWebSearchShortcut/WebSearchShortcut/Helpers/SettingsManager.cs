using System.IO;
using Windows.Foundation;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;

namespace WebSearchShortcut.Helpers;

internal class SettingsManager : JsonSettingsManager
{
    private const string _namespace = "WebSearchShortcut";
    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    private const int _defaultMaxDisplayCount = 20;
    private const int _defaultMaxHistoryDisplayCount = 3;

    private readonly IntSetting _maxDisplayCount = new(
        key: Namespaced(nameof(MaxDisplayCount)),
        label: Resources.Settings_MaxDisplayCount_Label,
        description: Resources.Settings_MaxDisplayCount_Description,
        defaultValue: _defaultMaxDisplayCount,
        min: 1,
        max: null
    )
    { ErrorMessage = Resources.Settings_MaxDisplayCount_ErrorMessage };

    private readonly IntSetting _maxHistoryDisplayCount = new(
        key: Namespaced(nameof(MaxHistoryDisplayCount)),
        label: Resources.Settings_MaxHistoryDisplayCount_Label,
        description: Resources.Settings_MaxHistoryDisplayCount_Description,
        defaultValue: _defaultMaxHistoryDisplayCount,
        min: 0,
        max: null
    )
    { ErrorMessage = Resources.Settings_MaxHistoryDisplayCount_ErrorMessage };

    public int MaxDisplayCount => _maxDisplayCount.Value;
    public int MaxHistoryDisplayCount => _maxHistoryDisplayCount.Value;

    public event TypedEventHandler<object, Settings>? SettingsChanged
    {
        add => Settings.SettingsChanged += value;
        remove => Settings.SettingsChanged -= value;
    }

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");

        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_maxDisplayCount);
        Settings.Add(_maxHistoryDisplayCount);

        LoadSettings();

        Settings.SettingsChanged += (s, a) => SaveSettings();
    }
}
