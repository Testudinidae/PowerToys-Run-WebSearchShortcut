using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using WebSearchShortcut.Helpers;

namespace WebSearchShortcut.History;

internal sealed class HistoryStore : JsonFileStore<Dictionary<string, List<HistoryEntry>>>
{
    public override string CurrentVersion => "1.0";
    public override JsonTypeInfo<DataFile<Dictionary<string, List<HistoryEntry>>>> TypeInfo
        => AppJsonSerializerContext.Default.DataFileDictionaryStringListHistoryEntry;
}
