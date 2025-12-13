using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Properties;

namespace WebSearchShortcut.SuggestionsProviders;

internal sealed class Wikipedia : ISuggestionsProvider
{
    public string Name => "Wikipedia";

    private HttpClient Http { get; }

    public Wikipedia()
    {
        Http = new HttpClient();
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
    }

    public async Task<IReadOnlyList<Suggestion>> GetSuggestionsAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            const string api = "https://en.wikipedia.org/w/rest.php/v1/search/title?limit=10&q=";

            await using var resultStream = await Http
                .GetStreamAsync(api + Uri.EscapeDataString(query), cancellationToken)
                .ConfigureAwait(false);

            using var json = await JsonDocument
                .ParseAsync(resultStream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var results = json.RootElement.GetProperty("pages");

            Suggestion[] items = [
                .. results
                    .EnumerateArray()
                    .Select(o => (
                        Title: o.TryGetProperty("title", out var t) ? t.GetString() : null,
                        Description: o.TryGetProperty("description", out var d) ? d.GetString() : null
                    ))
                    .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                    .Select(p => new Suggestion(p.Title!, p.Description ?? ""))
            ];

            return items;
        }
        catch (Exception e)
        {
            ExtensionHost.LogMessage($"{e.Message}");

            return [];
        }
    }

    public override string ToString()
    {
        return "Wikipedia";
    }
}
