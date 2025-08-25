using System;
using System.Net;
using System.Text.Json.Serialization;

namespace WebSearchShortcut.Shortcut;

internal sealed record class ShortcutEntry
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public string Url { get; set; } = string.Empty;
    // public string[]? Urls { get; set; }
    public string? SuggestionProvider { get; set; }
    public string? ReplaceWhitespace { get; set; }
    public string? IconUrl { get; set; }
    public string? HomePage { get; set; }
    public string? BrowserPath { get; set; }
    public string? BrowserArgs { get; set; }

    [JsonIgnore]
    public string Domain
    {
        get
        {
            return new Uri(Url.Split(' ')[0].Split('?')[0]).GetLeftPart(UriPartial.Authority);
        }
    }

    public ShortcutEntry WithId(string id) => new()
    {
        Id = id,
        Name = Name,
        Keyword = Keyword,
        Url = Url,
        SuggestionProvider = SuggestionProvider,
        ReplaceWhitespace = ReplaceWhitespace,
        IconUrl = IconUrl,
        HomePage = HomePage,
        BrowserPath = BrowserPath,
        BrowserArgs = BrowserArgs
    };

    public ShortcutEntry Copy() => new()
    {
        Id = Id,
        Name = Name,
        Keyword = Keyword,
        Url = Url,
        SuggestionProvider = SuggestionProvider,
        ReplaceWhitespace = ReplaceWhitespace,
        IconUrl = IconUrl,
        HomePage = HomePage,
        BrowserPath = BrowserPath,
        BrowserArgs = BrowserArgs
    };

    public static string UrlEncode(ShortcutEntry shortcut, string query)
    {
        if (string.IsNullOrWhiteSpace(shortcut.ReplaceWhitespace) || shortcut.ReplaceWhitespace == " ")
        {
            return WebUtility.UrlEncode(query);
        }
        if (shortcut.ReplaceWhitespace == "%20")
        {
            return WebUtility.UrlEncode(query).Replace("+", "%20");
        }
        return WebUtility.UrlEncode(query.Replace(" ", shortcut.ReplaceWhitespace));
    }

    public static string GetSearchUrl(ShortcutEntry shortcut, string query)
    {
        string arguments = shortcut.Url.Replace("%s", UrlEncode(shortcut, query));

        return arguments;
    }

    public static string GetHomePageUrl(ShortcutEntry shortcut)
    {
        return !string.IsNullOrWhiteSpace(shortcut.HomePage)
                ? shortcut.HomePage
                : shortcut.Domain;
    }
}
