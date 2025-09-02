using System.Collections.Generic;
using System.Text.Json.Serialization;
using WebSearchShortcut.History;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut.Helpers;

[JsonSourceGenerationOptions(
    IncludeFields = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(ShortcutEntry))]
[JsonSerializable(typeof(DataFile<List<ShortcutEntry>>))]
[JsonSerializable(typeof(HistoryStore))]
[JsonSerializable(typeof(DataFile<Dictionary<string, List<HistoryEntry>>>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
