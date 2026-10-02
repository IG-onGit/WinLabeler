using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinLabeler;

public enum Corner { BottomLeft, BottomRight, TopLeft, TopRight }

public sealed class DesktopSettings
{
    public string Label { get; set; } = "";          // empty = "Desktop N"
    public string Color { get; set; } = "#2D6CDF";
    public Corner Corner { get; set; } = Corner.BottomLeft;
}

public sealed class WorkplaceOption
{
    public string Label { get; set; } = "";
    public string Command { get; set; } = "";     // run through cmd.exe
}

public sealed class AppSettings
{
    public bool AlwaysOnTop { get; set; } = true;
    public Dictionary<string, DesktopSettings> Desktops { get; set; } = new();
    public List<WorkplaceOption> Workplaces { get; set; } = new();

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLabeler");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
        }
        catch { /* corrupt file -> start fresh */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Options));
        }
        catch { /* ignore disk errors */ }
    }
}
