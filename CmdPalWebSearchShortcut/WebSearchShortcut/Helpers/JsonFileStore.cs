using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace WebSearchShortcut.Helpers;

public sealed class DataFile<T>
{
    public string Version { get; set; } = "1.0";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public T Data { get; set; } = default!;
}

public abstract class JsonFileStore<T>
{
    protected abstract string CurrentVersion { get; }
    protected abstract JsonTypeInfo<DataFile<T>> TypeInfo { get; }
    protected abstract T CreateDefault();

    public T LoadOrCreate(string path)
    {
        EnsureDirectory(path);
        if (!File.Exists(path))
        {
            var data = CreateDefault();
            Save(path, data);
            return data;
        }
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize(json, TypeInfo) ?? throw new InvalidDataException("資料檔無法解析。");
        return file.Data;
    }

    public void Save(string path, T data)
    {
        EnsureDirectory(path);
        var file = new DataFile<T> { Version = CurrentVersion, UpdatedAt = DateTime.UtcNow, Data = data };
        var json = Helpers.JsonPrettyFormatter.ToPrettyJson(file, TypeInfo);
        File.WriteAllText(path, json);
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory!);
    }
}
