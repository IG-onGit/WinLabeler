using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSpace;

public enum Corner { BottomLeft, BottomRight, TopLeft, TopRight, BottomCenter, TopCenter }

public sealed class DesktopSettings
{
    public string Label { get; set; } = "";          // empty = "N"
    public string Color { get; set; } = "#3C3C3C";
    public Corner Corner { get; set; } = Corner.BottomCenter;
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
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinSpace");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>One-time move of the pre-rename (WinLabeler) settings folder.</summary>
    private static void MigrateLegacyFolder()
    {
        try
        {
            string legacy = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLabeler");
            if (Directory.Exists(legacy) && !Directory.Exists(Dir)) Directory.Move(legacy, Dir);
        }
        catch { /* keep going with defaults */ }
    }

    public static AppSettings Load()
    {
        MigrateLegacyFolder();
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
