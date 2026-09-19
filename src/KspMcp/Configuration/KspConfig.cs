using System;
using System.IO;

namespace KspMcp.Configuration;

public class KspConfig
{
    public string KspPath { get; set; } = string.Empty;
    public string KosTelnetHost { get; set; } = "127.0.0.1";
    public int KosTelnetPort { get; set; } = 5410;

    public string GameDataPath => Path.Combine(KspPath, "GameData");
    public string ShipsVabPath => Path.Combine(KspPath, "Ships", "VAB");
    public string ShipsSphPath => Path.Combine(KspPath, "Ships", "SPH");
    public string ShipsScriptPath => Path.Combine(KspPath, "Ships", "Script");

    public static KspConfig Discover()
    {
        var config = new KspConfig();

        // 1. Check environment variable
        var envPath = Environment.GetEnvironmentVariable("KSP_ROOT") ?? Environment.GetEnvironmentVariable("KSP_DIR");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            config.KspPath = envPath;
            return config;
        }

        // 2. Common Steam paths on Windows
        var defaultSteamPath = @"C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program";
        if (Directory.Exists(defaultSteamPath))
        {
            config.KspPath = defaultSteamPath;
            return config;
        }

        // 3. Fallback to current directory or placeholder
        config.KspPath = Directory.GetCurrentDirectory();
        return config;
    }

    public void EnsureDirectoriesExist()
    {
        if (!string.IsNullOrEmpty(KspPath) && Directory.Exists(KspPath))
        {
            Directory.CreateDirectory(ShipsVabPath);
            Directory.CreateDirectory(ShipsSphPath);
            Directory.CreateDirectory(ShipsScriptPath);
        }
    }
}
