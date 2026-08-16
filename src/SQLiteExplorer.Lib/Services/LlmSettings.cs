using System;
using System.IO;
using System.Text.Json;

namespace SQLiteExplorer.Lib.Services;

/// <summary>
/// Settings for the built-in OpenAI-compatible LLM service, used by the
/// standalone app. Hosts that inject their own <see cref="ILlmService"/>
/// never touch this file.
/// </summary>
public class LlmSettings
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.2;
    public bool ThinkingMode { get; set; }
    public string ThinkingEffort { get; set; } = "medium";

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SQLiteExplorer",
        "llm-settings.json");

    public static LlmSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path))
            {
                return new LlmSettings();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LlmSettings>(json) ?? new LlmSettings();
        }
        catch (Exception)
        {
            return new LlmSettings();
        }
    }

    public void Save()
    {
        var path = SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
