using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using WebSearchShortcut.Commands;
using WebSearchShortcut.Constants;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Services;
using Windows.System;

namespace WebSearchShortcut;

public partial class SearchPage : DynamicListPage
{
  public string Url { get; }
  public WebSearchShortcutItem Item { get; }

  private List<ListItem> allItems;
  private List<ListItem> allSuggestItems;
  private readonly ListItem _emptyListItem;

  public SearchPage(WebSearchShortcutItem data)
  {
    Item = data;
    Name = data.Name;
    Url = data.Url;
    Icon = IconService.GetIconInfo(data);
    _emptyListItem = new ListItem(new OpenHomePageCommand(data));
    allItems = [_emptyListItem];

    _lastSuggestionId = 0;
    allSuggestItems = [];
  }

  public override IListItem[] GetItems()
  {
    return [
      ..allItems,
    ];
  }

  public List<ListItem> Query(string query)
  {
    var results = new List<ListItem>();
    // empty query
    if (string.IsNullOrEmpty(query))
    {
      allSuggestItems = [];
      results.Add(_emptyListItem);
    }
    else
    {
      var searchTerm = query;
      var result = new ListItem(new SearchWebCommand(searchTerm, Item))
      {
        Title = searchTerm,
        Subtitle = $"Search {Name} for '{searchTerm}'",
        MoreCommands = [new CommandContextItem(
          title: $"Open {Name}",
          name: $"Open {Name}",
          action: () => HomePageLauncher.OpenHomePageWithBrowser(Item)
        )]
      };
      results.Add(result);
    }

    return results;
  }

  private int _lastSuggestionId;
  public override async void UpdateSearchText(string oldSearch, string newSearch)
  {
    var ignoreId = ++_lastSuggestionId;
    var queryItems = Query(newSearch);
    allItems = [.. queryItems, .. allSuggestItems];
    RaiseItemsChanged(allItems.Count);
    if (string.IsNullOrWhiteSpace(Item.SuggestionProvider) || string.IsNullOrEmpty(newSearch))
    {
      return;
    }
    var suggestions = await Suggestions.QuerySuggestionsAsync(Item.SuggestionProvider, newSearch);
    if (ignoreId != _lastSuggestionId)
    {
      return;
    }
    List<ListItem> suggestItems = [.. suggestions
      .Select(s => new ListItem(new SearchWebCommand(s.Title, Item))
      {
        Title = s.Title,
        Subtitle = s.Description ?? "",
        // TextToSuggest = s.Title,
        MoreCommands = [new CommandContextItem(
          title: $"Open {Name}",
          name: $"Open {Name}",
          action: () => HomePageLauncher.OpenHomePageWithBrowser(Item)
        )]
      })];
    List<ListItem> items = [.. queryItems, .. suggestItems];
    allSuggestItems = suggestItems;

    allItems = items;
    RaiseItemsChanged(allItems.Count);
  }
}