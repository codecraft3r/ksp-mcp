using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KspMcp.Configuration;
using KspMcp.Craft;
using KspMcp.Kos;
using KspMcp.Maths;
using KspMcp.Parts;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace KspMcp.Tools;

public class KspTools
{
    private readonly KspConfig _config;
    private readonly PartCatalog _catalog;
    private readonly StageBuilder _builder;
    private readonly KosScriptManager _scriptManager;
    private readonly KosTelnetClient _telnetClient;
    private readonly ILogger? _logger;

    public KspTools(
        KspConfig config,
        PartCatalog catalog,
        StageBuilder builder,
        KosScriptManager scriptManager,
        KosTelnetClient telnetClient,
        ILogger? logger = null)
    {
        _config = config;
        _catalog = catalog;
        _builder = builder;
        _scriptManager = scriptManager;
        _telnetClient = telnetClient;
        _logger = logger;
    }

    public void RegisterTools(McpServerOptions options)
    {
        options.ToolCollection ??= new();
        options.ToolCollection.Add(McpServerTool.Create(
            (string? category, string? searchTerm, string? size) =>
            {
                var results = _catalog.Search(category, searchTerm, size)
                    .Take(50)
                    .Select(p => new
                    {
                        p.Name,
                        p.Title,
                        p.Category,
                        p.DryMass,
                        p.WetMass,
                        p.Cost,
                        Profiles = string.Join(",", p.BulkheadProfiles),
                        HasEngine = p.Engine != null,
                        MaxThrust = p.Engine?.MaxThrust,
                        IspVac = p.Engine?.IspVac,
                        p.IsCommand,
                        p.IsDecoupler,
                        p.IsParachute
                    });

                return results;
            },
            new McpServerToolCreateOptions
            {
                Name = "ksp_search_parts",
                Description = "Search the KSP parts catalog by category (Pods, Engines, FuelTank, Coupling, Aero, Utility), size (size0, size1, size2), or keyword."
            }));

        // 2. Get Part Details
        options.ToolCollection.Add(McpServerTool.Create(
            (string partName) =>
            {
                var p = _catalog.GetPart(partName);
                if (p == null)
                    return (object)new { Error = $"Part '{partName}' not found in catalog." };

                return new
                {
                    p.Name,
                    p.Title,
                    p.Category,
                    p.DryMass,
                    p.WetMass,
                    p.Cost,
                    p.BulkheadProfiles,
                    AttachNodes = p.AttachNodes.Select(n => new { n.Id, Pos = $"{n.Position.X},{n.Position.Y},{n.Position.Z}", n.Size }),
                    Resources = p.Resources.Select(r => new { r.Name, r.Amount, r.MaxAmount, Mass = r.Mass }),
                    Engine = p.Engine == null ? null : new
                    {
                        p.Engine.MaxThrust,
                        p.Engine.MinThrust,
                        p.Engine.IspVac,
                        p.Engine.IspAsl,
                        p.Engine.Propellants
                    },
                    p.IsCommand,
                    p.IsDecoupler,
                    p.IsParachute,
                    p.HasKos
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "ksp_get_part_details",
                Description = "Get comprehensive specifications for a specific part (dry/wet mass, resources, attachment nodes, engine thrust, and Isp)."
            }));

        // 3. Calculate Stage Delta-V
        options.ToolCollection.Add(McpServerTool.Create(
            (string engineName, string tankName, int tankCount, double payloadMass) =>
            {
                var engine = _catalog.GetPart(engineName);
                if (engine == null || engine.Engine == null)
                    return (object)new { Error = $"Engine '{engineName}' not found or has no engine module." };

                var tank = _catalog.GetPart(tankName);
                if (tank == null)
                    return (object)new { Error = $"Fuel tank '{tankName}' not found." };

                var tanks = Enumerable.Repeat(tank, Math.Max(1, tankCount));
                var analysis = RocketMath.AnalyzeStage(engine, tanks, payloadMass);

                return new
                {
                    Engine = engine.Title,
                    Tank = $"{tankCount}x {tank.Title}",
                    PayloadMass = payloadMass,
                    analysis.DryMass,
                    analysis.WetMass,
                    analysis.PropellantMass,
                    analysis.DeltaVVac,
                    analysis.DeltaVAsl,
                    analysis.TwrVac,
                    analysis.TwrAsl,
                    analysis.BurnTime,
                    analysis.TotalThrust
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "ksp_calculate_stage_deltav",
                Description = "Calculates Tsiolkovsky delta-v, wet/dry mass, Kerbin TWR, and burn time for a proposed engine + fuel tank + payload configuration."
            }));

        // 4. Build Launch Vehicle
        options.ToolCollection.Add(McpServerTool.Create(
            (string vesselName, string? payloadType, bool? includeParachute, bool? includeHeatShield,
             string? upperEngineName, string? upperTankName, int? upperTankCount,
             string? boosterEngineName, string? boosterTankName, int? boosterTankCount, bool? includeFins) =>
            {
                var config = new LaunchVehicleConfig
                {
                    VesselName = string.IsNullOrWhiteSpace(vesselName) ? "Ai_Rocket" : vesselName,
                    PayloadType = payloadType ?? "Crewed",
                    IncludeParachute = includeParachute ?? true,
                    IncludeHeatShield = includeHeatShield ?? true,
                    UpperEngineName = upperEngineName ?? "liquidEngine3_v2",
                    UpperTankName = upperTankName ?? "fuelTank_long",
                    UpperTankCount = upperTankCount ?? 1,
                    BoosterEngineName = boosterEngineName ?? "liquidEngine_v2",
                    BoosterTankName = boosterTankName ?? "fuelTank_long",
                    BoosterTankCount = boosterTankCount ?? 2,
                    IncludeFins = includeFins ?? true
                };

                var vessel = _builder.BuildLaunchVehicle(config);
                var validation = CraftValidator.ValidateVessel(vessel);

                var outPath = Path.Combine(_config.ShipsVabPath, $"{vessel.Name}.craft");
                CraftWriter.SaveCraft(vessel, outPath);

                return (object)new
                {
                    Success = true,
                    SavedTo = outPath,
                    VesselName = vessel.Name,
                    PartCount = vessel.Parts.Count,
                    TotalMass = validation.TotalMass,
                    IsValid = validation.IsValid,
                    ValidationWarnings = validation.Warnings,
                    ValidationErrors = validation.Errors,
                    PartsList = vessel.Parts.Select(p => new { p.FullId, Pos = $"{p.Position.X:F2},{p.Position.Y:F2},{p.Position.Z:F2}", Stage = p.IgnitionStage })
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "ksp_build_launch_vehicle",
                Description = "Assembles and saves a complete multi-stage rocket directly to KSP's Ships/VAB directory. Configures node snapping, staging, and payload."
            }));

        // 5. Validate Craft
        options.ToolCollection.Add(McpServerTool.Create(
            (string craftName) =>
            {
                var craftPath = craftName.EndsWith(".craft", StringComparison.OrdinalIgnoreCase)
                    ? (File.Exists(craftName) ? craftName : Path.Combine(_config.ShipsVabPath, craftName))
                    : Path.Combine(_config.ShipsVabPath, $"{craftName}.craft");

                var validation = CraftValidator.ValidateFile(craftPath, _catalog);
                return validation;
            },
            new McpServerToolCreateOptions
            {
                Name = "ksp_validate_craft",
                Description = "Inspects an existing .craft file in Ships/VAB for structural integrity, part count, command module, and propulsion."
            }));

        // 6. Deploy Helper Libraries
        options.ToolCollection.Add(McpServerTool.Create(
            () =>
            {
                _scriptManager.DeployHelperLibraries();
                return new
                {
                    Success = true,
                    Message = "Standard flight helper libraries deployed to kOS Archive (Ships/Script/).",
                    Libraries = new[] { "lib_math.ks", "lib_ascent.ks", "lib_node.ks", "lib_orbit.ks", "lib_telemetry.ks" }
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_deploy_helper_libraries",
                Description = "Deploys reusable kOS autopilot helper libraries (ascent guidance, maneuver nodes, circularization, math, telemetry) to Ships/Script/."
            }));

        // 7. List kOS Scripts
        options.ToolCollection.Add(McpServerTool.Create(
            () =>
            {
                var scripts = _scriptManager.ListScripts();
                return scripts;
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_list_scripts",
                Description = "Lists all KerboScript flight scripts and libraries located in the kOS Archive folder (Ships/Script/)."
            }));

        // 8. Read kOS Script
        options.ToolCollection.Add(McpServerTool.Create(
            (string scriptName) =>
            {
                var content = _scriptManager.ReadScript(scriptName);
                if (content == null)
                    return (object)new { Error = $"Script '{scriptName}' not found in kOS Archive." };

                return new { ScriptName = scriptName, Content = content };
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_read_script",
                Description = "Reads the content of a KerboScript (.ks) file from the kOS Archive folder."
            }));

        // 9. Write kOS Script
        options.ToolCollection.Add(McpServerTool.Create(
            (string scriptName, string content) =>
            {
                var validation = _scriptManager.WriteScript(scriptName, content);
                return new
                {
                    Success = validation.IsValid,
                    ScriptName = scriptName,
                    Errors = validation.Errors,
                    Warnings = validation.Warnings
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_write_script",
                Description = "Writes or updates a KerboScript (.ks) file in the kOS Archive. Performs pre-flight syntax checks (braces, parens, statements)."
            }));

        // 10. Execute kOS Command over Telnet
        options.ToolCollection.Add(McpServerTool.Create(
            async (string command, int? timeoutSeconds) =>
            {
                var timeout = TimeSpan.FromSeconds(timeoutSeconds ?? 5);
                var response = await _telnetClient.ExecuteCommandAsync(command, timeout);
                return new
                {
                    Command = command,
                    Output = response,
                    IsConnected = _telnetClient.IsConnected
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_execute_command",
                Description = "Sends a command or initiates a flight script over kOS Telnet (port 5410) to the active vessel in KSP."
            }));

        // 11. Read kOS Terminal Output
        options.ToolCollection.Add(McpServerTool.Create(
            async (int? waitMilliseconds) =>
            {
                var timeout = TimeSpan.FromMilliseconds(waitMilliseconds ?? 500);
                var output = await _telnetClient.ReadTerminalAsync(timeout);
                return new
                {
                    TerminalBuffer = output,
                    IsConnected = _telnetClient.IsConnected
                };
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_get_terminal_output",
                Description = "Reads the latest terminal screen buffer from the active kOS Telnet connection."
            }));

        // 12. Get Flight Telemetry
        options.ToolCollection.Add(McpServerTool.Create(
            async () =>
            {
                var telemetry = await _telnetClient.GetTelemetryAsync();
                return telemetry;
            },
            new McpServerToolCreateOptions
            {
                Name = "kos_get_telemetry",
                Description = "Queries live flight telemetry (altitude, apoapsis, periapsis, orbital speed, vessel mass) from the active vessel via kOS."
            }));
    }
}
