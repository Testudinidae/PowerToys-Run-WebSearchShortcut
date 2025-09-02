using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSearchShortcut.History;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut.Helpers;

[JsonSourceGenerationOptions(
    IncludeFields = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(ShortcutEntry))]
[JsonSerializable(typeof(DataFile<List<ShortcutEntry>>))]
[JsonSerializable(typeof(HistoryStorage))]
[JsonSerializable(typeof(HistoryEntry))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
