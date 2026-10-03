using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinLabeler;

public enum Corner { BottomLeft, BottomRight, TopLeft, TopRight, BottomCenter, TopCenter }

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
    public string Color { get; set; } = "#2D6CDF"; // label background while this workplace is active
}

public sealed class AppSettings
{
    public bool AlwaysOnTop { get; set; } = true;
    public bool RememberLabels { get; set; } = true;   // off: desktops start as plain numbers, bottom center
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
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
                if (!loaded.RememberLabels) loaded.Desktops.Clear();
                return loaded;
            }
        }
        catch { /* corrupt file -> start fresh */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            {
                // With "Remember labels" off, per-desktop labels are kept in memory only.
                var toSave = new AppSettings
                {
                    AlwaysOnTop = AlwaysOnTop,
                    RememberLabels = RememberLabels,
                    Workplaces = Workplaces,
                    Desktops = RememberLabels ? Desktops : new(),
                };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(toSave, Options));
            }
        }
        catch { /* ignore disk errors */ }
    }
}
