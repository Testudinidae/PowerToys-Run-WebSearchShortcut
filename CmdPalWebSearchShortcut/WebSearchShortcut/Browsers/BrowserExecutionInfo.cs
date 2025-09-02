using System;
using System.Linq;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut.Browsers;

internal sealed class BrowserExecutionInfo
{
    public string? Path { get; }
    public string? ArgumentsPattern { get; }

    public BrowserExecutionInfo(ShortcutEntry shortcut)
    {
        DefaultBrowserProvider.UpdateIfTimePassed();

        Path = !string.IsNullOrWhiteSpace(shortcut.BrowserPath)
               ? shortcut.BrowserPath
               : DefaultBrowserProvider.Path;

        string? trimmedArgs;

        if (!string.IsNullOrWhiteSpace(shortcut.BrowserArgs))
        {
            trimmedArgs = shortcut.BrowserArgs?.Trim();
        }
        else if (string.IsNullOrWhiteSpace(shortcut.BrowserPath))
        {
            trimmedArgs = DefaultBrowserProvider.ArgumentsPattern?.Trim();
        }
        else
        {
            trimmedArgs = BrowserDiscovery
                .GetAllInstalledBrowsers()
                .FirstOrDefault(browerInfo => string.Equals(browerInfo.Path, shortcut.BrowserPath, StringComparison.OrdinalIgnoreCase))?
                .ArgumentsPattern?
                .Trim();
        }

        if (string.IsNullOrWhiteSpace(trimmedArgs))
        {
            trimmedArgs = "%1";
        }
        else if (!trimmedArgs.Contains("%1"))
        {
            trimmedArgs += " %1";
        }

        ArgumentsPattern = trimmedArgs;
    }
}
