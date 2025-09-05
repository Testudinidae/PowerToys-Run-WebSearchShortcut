using System.IO;
using Windows.Foundation;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;

namespace WebSearchShortcut.Setting;

internal static class SettingsHub
{
    private static string Namespaced(string propertyName) => $"WebSearchShortcut.{propertyName}";
    private const int _defaultMaxDisplayCount = 20;
    private const int _defaultMaxHistoryDisplayCount = 3;

    private static readonly IntSetting _maxDisplayCount = new(
        Namespaced(nameof(MaxDisplayCount)),
        Resources.Settings_MaxDisplayCount_Label,
        Resources.Settings_MaxDisplayCount_Description,
        _defaultMaxDisplayCount,
        1, null
    )
    { ErrorMessage = Resources.Settings_MaxDisplayCount_ErrorMessage };

    private static readonly IntSetting _maxHistoryDisplayCount = new(
        Namespaced(nameof(MaxHistoryDisplayCount)),
        Resources.Settings_MaxHistoryDisplayCount_Label,
        Resources.Settings_MaxHistoryDisplayCount_Description,
        _defaultMaxHistoryDisplayCount,
        0, null
    )
    { ErrorMessage = Resources.Settings_MaxHistoryDisplayCount_ErrorMessage };

    public static string SettingsJsonPath
    {
        get
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "settings.json");
        }
    }

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
