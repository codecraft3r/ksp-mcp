using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KspMcp.Configuration;
using KspMcp.Craft;
using KspMcp.Kos;
using KspMcp.Parts;
using KspMcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KspMcp;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var config = KspConfig.Discover();
        config.EnsureDirectoriesExist();

        // Setup logger to STDERR (Crucial: stdout is reserved exclusively for MCP JSON-RPC!)
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger("KspMcp");

        var catalog = new PartCatalog(logger);
        var builder = new StageBuilder(catalog);
        var scriptManager = new KosScriptManager(config);
        var telnetClient = new KosTelnetClient(config.KosTelnetHost, config.KosTelnetPort, logger);
        var kspTools = new KspTools(config, catalog, builder, scriptManager, telnetClient, logger);

        // CLI test modes
        if (args.Length > 0)
        {
            var command = args[0].ToLowerInvariant();
            switch (command)
            {
                case "--test-scan":
                    Console.Error.WriteLine($"[CLI] Scanning GameData at {config.GameDataPath}...");
                    await catalog.ScanGameDataAsync(config.GameDataPath);
                    Console.Error.WriteLine($"[CLI] Parts catalog loaded: {catalog.Count} parts.");
                    return 0;

                case "--test-build":
                    Console.Error.WriteLine("[CLI] Building test orbital vehicle...");
                    var testVessel = builder.BuildLaunchVehicle(new LaunchVehicleConfig
                    {
                        VesselName = "Ai_Test_Orbiter",
                        PayloadType = "Crewed",
                        IncludeParachute = true,
                        IncludeHeatShield = true,
                        BoosterTankCount = 2
                    });
                    var validation = CraftValidator.ValidateVessel(testVessel);
                    var outPath = Path.Combine(config.ShipsVabPath, $"{testVessel.Name}.craft");
                    CraftWriter.SaveCraft(testVessel, outPath);
                    Console.Error.WriteLine($"[CLI] Vessel saved to: {outPath}");
                    Console.Error.WriteLine($"[CLI] Parts: {testVessel.Parts.Count}, Total Mass: {validation.TotalMass}t, IsValid: {validation.IsValid}");
                    return validation.IsValid ? 0 : 1;

                case "--build-mun":
                    Console.Error.WriteLine("[CLI] Building Mun Explorer I (Trans-Munar Injection capable)...");
                    var munVessel = builder.BuildLaunchVehicle(new LaunchVehicleConfig
                    {
                        VesselName = "Mun_Explorer_I",
                        PayloadType = "Crewed",
                        IncludeParachute = true,
                        IncludeHeatShield = true,
                        UpperEngineName = "liquidEngine3_v2",
                        UpperTankName = "fuelTank_long",
                        UpperTankCount = 1,
                        BoosterEngineName = "liquidEngine_v2",
                        BoosterTankName = "fuelTank_long",
                        BoosterTankCount = 2,
                        IncludeFins = true
                    });
                    var munVal = CraftValidator.ValidateVessel(munVessel);
                    var munPath = Path.Combine(config.ShipsVabPath, $"{munVessel.Name}.craft");
                    CraftWriter.SaveCraft(munVessel, munPath);
                    Console.Error.WriteLine($"[CLI] Mun vessel saved to: {munPath}");
                    Console.Error.WriteLine($"[CLI] Parts: {munVessel.Parts.Count}, Total Mass: {munVal.TotalMass}t, IsValid: {munVal.IsValid}");
                    return munVal.IsValid ? 0 : 1;

                case "--deploy-libs":
                    Console.Error.WriteLine($"[CLI] Deploying kOS helper libraries to {config.ShipsScriptPath}...");
                    scriptManager.DeployHelperLibraries();
                    Console.Error.WriteLine("[CLI] Deployment complete.");
                    return 0;

                case "--help":
                case "-h":
                    Console.Error.WriteLine("KSP MCP Server - Autonomous Driver for Kerbal Space Program");
                    Console.Error.WriteLine("Usage: dotnet run [options]");
                    Console.Error.WriteLine("  (no args)       Run as standard MCP server over stdio");
                    Console.Error.WriteLine("  --test-scan     Scan KSP GameData and report part count");
                    Console.Error.WriteLine("  --test-build    Generate and save a test rocket to Ships/VAB");
                    Console.Error.WriteLine("  --deploy-libs   Deploy kOS flight libraries to Ships/Script");
                    return 0;
            }
        }

        // Run as MCP Server over stdio
        logger.LogInformation("Starting KSP MCP Server (C# / .NET 10)...");
        logger.LogInformation("KSP Path: {Path}", config.KspPath);

        // Pre-scan GameData in background
        if (Directory.Exists(config.GameDataPath))
        {
            _ = Task.Run(async () =>
            {
                await catalog.ScanGameDataAsync(config.GameDataPath);
                logger.LogInformation("Part catalog ready with {Count} parts.", catalog.Count);
            });
        }

        // Deploy helper libraries to kOS Archive automatically on startup
        if (Directory.Exists(config.ShipsScriptPath))
        {
            scriptManager.DeployHelperLibraries();
            logger.LogInformation("kOS helper libraries deployed to {Path}", config.ShipsScriptPath);
        }

        var mcpOptions = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = "ksp-mcp",
                Version = "1.0.0"
            },
            ServerInstructions = "KSP Autonomous Aerospace & Flight MCP Server. Use tools to search parts, size rocket stages, assemble .craft files in Ships/VAB, deploy and author kOS KerboScript flight plans, and monitor missions over Telnet.",
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = true }
            }
        };

        // Register tools
        kspTools.RegisterTools(mcpOptions);

        var services = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton(catalog)
            .AddSingleton(builder)
            .AddSingleton(scriptManager)
            .AddSingleton(telnetClient)
            .BuildServiceProvider();

        var transport = new StdioServerTransport(mcpOptions, loggerFactory);
        var server = McpServer.Create(transport, mcpOptions, loggerFactory, services);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await server.RunAsync(cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "KSP MCP Server terminated unexpectedly");
            return 1;
        }
    }
}
