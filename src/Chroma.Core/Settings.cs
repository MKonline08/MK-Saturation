using System.Text.Json;
namespace MkSaturation;
public sealed record Settings(int Saturation = 100, bool GlobalEnabled = false, bool StartMinimized = false)
{
    public Settings Validated() => this with { Saturation = Math.Clamp(Saturation, 0, 200) };
}
public sealed class SettingsStore(string path)
{
    public string? Warning { get; private set; }
    public Settings Load()
    {
        try { return File.Exists(path) ? (JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings()).Validated() : new Settings(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        { Warning = "Settings could not be loaded; defaults are in use. " + e.Message; return new Settings(); }
    }
    public void Save(Settings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings.Validated(), new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }
}
