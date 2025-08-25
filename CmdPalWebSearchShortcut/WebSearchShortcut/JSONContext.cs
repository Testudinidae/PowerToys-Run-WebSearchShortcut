using System.Collections.Generic;
using System.Text.Json.Serialization;
using WebSearchShortcut.Helpers;
using WebSearchShortcut.Shortcut;

namespace WebSearchShortcut;

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(ShortcutEntry))]
[JsonSerializable(typeof(DataFile<List<ShortcutEntry>>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
