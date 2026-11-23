using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GameLibrary.Logic;
using GameLibrary.Logic.Enums;
using Runix.Logic.Helpers;

namespace RunixLauncher.Helpers;

public static class ThemeHelper
{
    public static string currentThemeName { get; private set; } = "";
    private static string[] GetThemeRoots() => [Path.Combine(AppContext.BaseDirectory, "Themes")];

    public static async Task Startup()
    {
        string theme = "default";

        if (ConfigHandler.configProvider!.GetInteger(ConfigKeys.Appearance_Theme, out int savedThemeIndex))
        {
            try
            {
                theme = GetThemes()[savedThemeIndex];
            }
            catch (Exception e)
            {
                LoggingHelper.LogError(e);
                await ConfigHandler.configProvider!.SaveInteger(ConfigKeys.Appearance_Theme, 0);
            }
        }

        await ChangeTheme(theme);
    }

    public static async Task<bool> ChangeTheme(string? to)
    {
        if (string.IsNullOrEmpty(to))
            to = "default";

        string? themePath = null;
        foreach (string root in GetThemeRoots())
        {
            if (!Directory.Exists(root))
                continue;

            string path = Path.Combine(root, $"{to}.json");

            if (File.Exists(path))
            {
                themePath = path;
                break;
            }
        }

        if (string.IsNullOrEmpty(themePath))
            return false;

        using (StreamReader reader = new StreamReader(themePath))
        {
            string json = await reader.ReadToEndAsync();
            JsonDocument doc = JsonDocument.Parse(json);

            foreach (JsonProperty el in doc.RootElement.EnumerateObject())
            {
                try
                {
                    string key = el.Name;
                    string? val = el.Value.GetString();

                    if (string.IsNullOrEmpty(val))
                        continue;

                    string type = key.Split("_").First().ToLowerInvariant();

                    switch (type)
                    {
                        case "colour":
                            Application.Current!.Resources[key] = Color.Parse(val);
                            break;

                        case "cornerradius":
                            Application.Current!.Resources[key] = new CornerRadius(int.Parse(val));
                            break;
                    }
                }
                catch (Exception e)
                {
                    LoggingHelper.LogError(e);
                }
            }
        }

        currentThemeName = to;
        return true;
    }

    public static string[] GetThemes()
    {
        return ["default", .. GetThemeRoots().SelectMany(SearchDir)];

        string[] SearchDir(string root)
        {
            if (!Directory.Exists(root))
                return [];

            return Directory.GetFiles(root)
                .Where(f => !f.EndsWith("default.json") && f.EndsWith(".json"))
                .Select(f => Path.GetFileName(f).Replace(".json", ""))
                .ToArray();
        }
    }

    public static T GetValueFromTheme<T>(string key)
    {
        return (T)Application.Current!.FindResource(key)!;
    }
}
