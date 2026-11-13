using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EVA.Desktop.Models;

namespace EVA.Desktop.Services;

public sealed class DesktopConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".eva",
        "desktop-config.json");

    public DesktopAppConfig Current { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(_configPath))
        {
            Current = new DesktopAppConfig();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            Current = JsonSerializer.Deserialize<DesktopAppConfig>(json) ?? new DesktopAppConfig();
        }
        catch
        {
            Current = new DesktopAppConfig();
        }
    }

    public async Task SaveAsync(DesktopAppConfig config)
    {
        Current = config;
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(_configPath, json);
    }
}
