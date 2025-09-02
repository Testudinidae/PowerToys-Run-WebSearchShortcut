using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using WebSearchShortcut.Helpers;

namespace WebSearchShortcut.History;

internal sealed class HistoryStore : JsonFileStore<Dictionary<string, List<HistoryEntry>>>
{
    public override string CurrentVersion
    {
        get
        {
            var info = Assembly.GetEntryAssembly()?
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion;

            var semver = info?.Split('+')[0];
            return string.IsNullOrWhiteSpace(semver) ? "0.0.0" : semver;
        }
    }
    public override JsonTypeInfo<DataFile<Dictionary<string, List<HistoryEntry>>>> TypeInfo
        => AppJsonSerializerContext.Default.DataFileDictionaryStringListHistoryEntry;
}
