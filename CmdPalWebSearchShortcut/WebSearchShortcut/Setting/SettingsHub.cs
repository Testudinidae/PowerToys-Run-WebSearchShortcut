using System.IO;
using Windows.Foundation;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;

namespace WebSearchShortcut.Setting;

internal static class SettingsHub
{
    private const int _defaultMaxDisplayCount = 20;
    private const int _defaultMaxHistoryDisplayCount = 3;
    private static string SettingsJsonPath
    {
        get
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "settings.json");
        }
    }

    private static string Namespaced(string propertyName) => $"WebSearchShortcut.{propertyName}";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1507:Use nameof to express symbol names", Justification = "<Pending>")]
    private static readonly IntSetting _maxDisplayCount = new(
        key: Namespaced("MaxDisplayCount"),
        label: Resources.Settings_MaxDisplayCount_Label,
        description: Resources.Settings_MaxDisplayCount_Description,
        defaultValue: _defaultMaxDisplayCount,
        min: 1,
        max: null
    )
    { ErrorMessage = Resources.Settings_MaxDisplayCount_ErrorMessage };

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1507:Use nameof to express symbol names", Justification = "<Pending>")]
    private static readonly IntSetting _maxHistoryDisplayCount = new(
        key: Namespaced("MaxHistoryDisplayCount"),
        label: Resources.Settings_MaxHistoryDisplayCount_Label,
        description: Resources.Settings_MaxHistoryDisplayCount_Description,
        defaultValue: _defaultMaxHistoryDisplayCount,
        min: 0,
        max: null
    )
    { ErrorMessage = Resources.Settings_MaxHistoryDisplayCount_ErrorMessage };

    private static readonly SettingsManager _settingManager = new(SettingsJsonPath);

    public static Settings Settings => _settingManager.Settings;

    public static int MaxDisplayCount => _maxDisplayCount.Value;
    public static int MaxHistoryDisplayCount => _maxHistoryDisplayCount.Value;

    public static event TypedEventHandler<object, Settings>? SettingsChanged
    {
        add => _settingManager.Settings.SettingsChanged += value;
        remove => _settingManager.Settings.SettingsChanged -= value;
    }

    private class SettingsManager : JsonSettingsManager
    {
        public SettingsManager(string filePath)
        {
            FilePath = filePath;

            Settings.Add(_maxDisplayCount);
            Settings.Add(_maxHistoryDisplayCount);

            LoadSettings();

            Settings.SettingsChanged += (s, a) => SaveSettings();
        }
    }
}
