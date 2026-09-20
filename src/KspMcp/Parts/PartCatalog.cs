using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KspMcp.Parts;

public class PartCatalog
{
    private readonly ConcurrentDictionary<string, PartInfo> _parts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger? _logger;

    public PartCatalog(ILogger? logger = null)
    {
        _logger = logger;
        LoadBuiltinParts();
    }

    public int Count => _parts.Count;

    public void RegisterPart(PartInfo part)
    {
        _parts[part.Name] = part;
    }

    public PartInfo? GetPart(string name)
    {
        if (_parts.TryGetValue(name, out var part))
            return part;

        var dotName = name.Replace('_', '.');
        var underName = name.Replace('.', '_');

        return _parts.Values.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(dotName, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Equals(underName, StringComparison.OrdinalIgnoreCase) ||
            p.Title.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Replace("_", "").Equals(name.Replace("_", "").Replace(".", ""), StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<PartInfo> GetAllParts() => _parts.Values;

    public IEnumerable<PartInfo> Search(string? category = null, string? searchTerm = null, string? size = null)
    {
        var query = _parts.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            query = query.Where(p => p.BulkheadProfiles.Any(bp => bp.Equals(size, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(p => p.Category).ThenBy(p => p.Cost);
    }

    public async Task ScanGameDataAsync(string gameDataPath)
    {
        if (string.IsNullOrWhiteSpace(gameDataPath) || !Directory.Exists(gameDataPath))
        {
            _logger?.LogWarning("GameData directory does not exist: {Path}", gameDataPath);
            return;
        }

        _logger?.LogInformation("Scanning GameData for part configs: {Path}", gameDataPath);

        await Task.Run(() =>
        {
            try
            {
                var cfgFiles = Directory.EnumerateFiles(gameDataPath, "*.cfg", SearchOption.AllDirectories);
                int scanned = 0;
                int added = 0;

                Parallel.ForEach(cfgFiles, file =>
                {
                    try
                    {
                        var text = File.ReadAllText(file);
                        if (!text.Contains("PART", StringComparison.OrdinalIgnoreCase))
                            return;

                        var nodes = ConfigNode.ParseString(text);
                        foreach (var node in nodes)
                        {
                            if (node.Name.Equals("PART", StringComparison.OrdinalIgnoreCase))
                            {
                                var part = CfgParser.ParsePartNode(node);
                                if (part != null)
                                {
                                    RegisterPart(part);
                                    System.Threading.Interlocked.Increment(ref added);
                                }
                            }
                        }
                        System.Threading.Interlocked.Increment(ref scanned);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug("Error reading cfg file {File}: {Error}", file, ex.Message);
                    }
                });

                _logger?.LogInformation("Scan complete: {Scanned} files parsed, {Total} total parts in catalog ({Added} from disk)",
                    scanned, _parts.Count, added);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to scan GameData directory");
            }
        });
    }

    private void LoadBuiltinParts()
    {
        // Stock Pods & Probes
        RegisterPart(new PartInfo
        {
            Name = "mk1pod_v2",
            Title = "Mk1 Command Pod",
            Category = "Pods",
            DryMass = 0.8,
            Cost = 600,
            BulkheadProfiles = { "size1", "size0" },
            IsCommand = true,
            HasKos = true,
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.642f, 0), Orientation = new Vector3(0, 1, 0), Size = 0 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.405f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            },
            Resources = { new PartResource { Name = "ElectricCharge", Amount = 50, MaxAmount = 50 }, new PartResource { Name = "MonoPropellant", Amount = 10, MaxAmount = 10 } }
        });

        RegisterPart(new PartInfo
        {
            Name = "probeCoreOcto_v2",
            Title = "Probodobodyne OKTO",
            Category = "Pods",
            DryMass = 0.1,
            Cost = 450,
            BulkheadProfiles = { "size0" },
            IsCommand = true,
            HasKos = true,
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.187f, 0), Orientation = new Vector3(0, 1, 0), Size = 0 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.187f, 0), Orientation = new Vector3(0, -1, 0), Size = 0 }
            },
            Resources = { new PartResource { Name = "ElectricCharge", Amount = 10, MaxAmount = 10 } }
        });

        // Dedicated kOS Parts
        RegisterPart(new PartInfo
        {
            Name = "kOSProcessor1",
            Title = "kOS Machine 1m",
            Category = "Control",
            DryMass = 0.08,
            Cost = 1200,
            BulkheadProfiles = { "size1" },
            HasKos = true,
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.1f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.1f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            },
            Resources = { new PartResource { Name = "ElectricCharge", Amount = 5, MaxAmount = 5 } }
        });

        // Parachutes
        RegisterPart(new PartInfo
        {
            Name = "parachuteSingle",
            Title = "Mk16 Parachute",
            Category = "Utility",
            DryMass = 0.1,
            Cost = 422,
            BulkheadProfiles = { "size0" },
            IsParachute = true,
            AttachNodes =
            {
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.05f, 0), Orientation = new Vector3(0, -1, 0), Size = 0 }
            }
        });

        // Heat Shields
        RegisterPart(new PartInfo
        {
            Name = "HeatShield1",
            Title = "Heat Shield (1.25m)",
            Category = "Thermal",
            DryMass = 0.2,
            Cost = 300,
            BulkheadProfiles = { "size1" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.06f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.06f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            },
            Resources = { new PartResource { Name = "Ablator", Amount = 200, MaxAmount = 200 } }
        });

        // Decouplers
        RegisterPart(new PartInfo
        {
            Name = "Decoupler_1",
            Title = "TD-12 Decoupler",
            Category = "Coupling",
            DryMass = 0.04,
            Cost = 200,
            BulkheadProfiles = { "size1" },
            IsDecoupler = true,
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.05f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.05f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "Decoupler_2",
            Title = "TD-25 Decoupler",
            Category = "Coupling",
            DryMass = 0.16,
            Cost = 400,
            BulkheadProfiles = { "size2" },
            IsDecoupler = true,
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.08f, 0), Orientation = new Vector3(0, 1, 0), Size = 2 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.08f, 0), Orientation = new Vector3(0, -1, 0), Size = 2 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "Decoupler_3",
            Title = "TD-37 Decoupler",
            Category = "Coupling",
            DryMass = 0.36,
            Cost = 375,
            BulkheadProfiles = { "size3" },
            IsDecoupler = true,
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.15f, 0), Orientation = new Vector3(0, 1, 0), Size = 3 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.15f, 0), Orientation = new Vector3(0, -1, 0), Size = 3 }
            }
        });

        // Fuel Tanks (Size 1 - 1.25m)
        RegisterPart(new PartInfo
        {
            Name = "fuelTankSmallFlat",
            Title = "FL-T100 Fuel Tank",
            Category = "FuelTank",
            DryMass = 0.0625,
            Cost = 150,
            BulkheadProfiles = { "size1" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.278f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.278f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 45, MaxAmount = 45 }, new PartResource { Name = "Oxidizer", Amount = 55, MaxAmount = 55 } }
        });

        RegisterPart(new PartInfo
        {
            Name = "fuelTankSmall",
            Title = "FL-T200 Fuel Tank",
            Category = "FuelTank",
            DryMass = 0.125,
            Cost = 275,
            BulkheadProfiles = { "size1" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.555f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.555f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 90, MaxAmount = 90 }, new PartResource { Name = "Oxidizer", Amount = 110, MaxAmount = 110 } }
        });

        RegisterPart(new PartInfo
        {
            Name = "fuelTank_long",
            Title = "FL-T800 Fuel Tank",
            Category = "FuelTank",
            DryMass = 0.5,
            Cost = 800,
            BulkheadProfiles = { "size1" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 1.889f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -1.889f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 360, MaxAmount = 360 }, new PartResource { Name = "Oxidizer", Amount = 440, MaxAmount = 440 } }
        });

        // Fuel Tanks (Size 2 - 2.5m)
        RegisterPart(new PartInfo
        {
            Name = "Rockomax16_BW",
            Title = "Rockomax X200-16 Fuel Tank",
            Category = "FuelTank",
            DryMass = 1.0,
            Cost = 1550,
            BulkheadProfiles = { "size2" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.94f, 0), Orientation = new Vector3(0, 1, 0), Size = 2 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.94f, 0), Orientation = new Vector3(0, -1, 0), Size = 2 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 720, MaxAmount = 720 }, new PartResource { Name = "Oxidizer", Amount = 880, MaxAmount = 880 } }
        });

        RegisterPart(new PartInfo
        {
            Name = "Rockomax32_BW",
            Title = "Rockomax X200-32 Fuel Tank",
            Category = "FuelTank",
            DryMass = 2.0,
            Cost = 3000,
            BulkheadProfiles = { "size2" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 1.88f, 0), Orientation = new Vector3(0, 1, 0), Size = 2 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -1.88f, 0), Orientation = new Vector3(0, -1, 0), Size = 2 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 1440, MaxAmount = 1440 }, new PartResource { Name = "Oxidizer", Amount = 1760, MaxAmount = 1760 } }
        });

        // Fuel Tanks (Size 3 - 3.75m)
        RegisterPart(new PartInfo
        {
            Name = "Size3MediumTank",
            Title = "Kerbodyne S3-7200 Tank",
            Category = "FuelTank",
            DryMass = 4.5,
            Cost = 6500,
            BulkheadProfiles = { "size3" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 1.931f, 0), Orientation = new Vector3(0, 1, 0), Size = 3 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -1.937f, 0), Orientation = new Vector3(0, -1, 0), Size = 3 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 3240, MaxAmount = 3240 }, new PartResource { Name = "Oxidizer", Amount = 3960, MaxAmount = 3960 } }
        });

        RegisterPart(new PartInfo
        {
            Name = "Size3LargeTank",
            Title = "Kerbodyne S3-14400 Tank",
            Category = "FuelTank",
            DryMass = 9.0,
            Cost = 13000,
            BulkheadProfiles = { "size3" },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 3.74f, 0), Orientation = new Vector3(0, 1, 0), Size = 3 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -3.74f, 0), Orientation = new Vector3(0, -1, 0), Size = 3 }
            },
            Resources = { new PartResource { Name = "LiquidFuel", Amount = 6480, MaxAmount = 6480 }, new PartResource { Name = "Oxidizer", Amount = 7920, MaxAmount = 7920 } }
        });

        // Engines
        RegisterPart(new PartInfo
        {
            Name = "liquidEngine3_v2",
            Title = "LV-909 Terrier Liquid Fuel Engine",
            Category = "Engines",
            DryMass = 0.5,
            Cost = 390,
            BulkheadProfiles = { "size1" },
            Engine = new EngineInfo { MaxThrust = 60, MinThrust = 0, IspVac = 345, IspAsl = 85, Propellants = { "LiquidFuel", "Oxidizer" } },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.216f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.584f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "liquidEngine_v2",
            Title = "LV-T45 Swivel Liquid Fuel Engine",
            Category = "Engines",
            DryMass = 1.5,
            Cost = 1200,
            BulkheadProfiles = { "size1" },
            Engine = new EngineInfo { MaxThrust = 215, MinThrust = 0, IspVac = 320, IspAsl = 250, Propellants = { "LiquidFuel", "Oxidizer" } },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0.724f, 0), Orientation = new Vector3(0, 1, 0), Size = 1 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -0.724f, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "liquidEngineMainsail_v2",
            Title = "RE-M3 Mainsail Liquid Engine",
            Category = "Engines",
            DryMass = 6.0,
            Cost = 13000,
            BulkheadProfiles = { "size2" },
            Engine = new EngineInfo { MaxThrust = 1500, MinThrust = 0, IspVac = 310, IspAsl = 285, Propellants = { "LiquidFuel", "Oxidizer" } },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 1.144f, 0), Orientation = new Vector3(0, 1, 0), Size = 2 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -1.97f, 0), Orientation = new Vector3(0, -1, 0), Size = 2 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "liquidEngine2-2_v2",
            Title = "RE-L10 'Poodle' Liquid Fuel Engine",
            Category = "Engines",
            DryMass = 1.75,
            Cost = 1300,
            BulkheadProfiles = { "size2" },
            Engine = new EngineInfo { MaxThrust = 250, MinThrust = 0, IspVac = 375, IspAsl = 90, Propellants = { "LiquidFuel", "Oxidizer" } },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 0, 0), Orientation = new Vector3(0, 1, 0), Size = 2 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -1.5f, 0), Orientation = new Vector3(0, -1, 0), Size = 2 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "Size3AdvancedEngine",
            Title = "Kerbodyne KR-2L+ 'Rhino' Liquid Fuel Engine",
            Category = "Engines",
            DryMass = 9.0,
            Cost = 25000,
            BulkheadProfiles = { "size3" },
            Engine = new EngineInfo { MaxThrust = 2000, MinThrust = 0, IspVac = 340, IspAsl = 205, Propellants = { "LiquidFuel", "Oxidizer" } },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 1.488f, 0), Orientation = new Vector3(0, 1, 0), Size = 3 },
                new AttachNode { Id = "bottom", Position = new Vector3(0, -2.537f, 0), Orientation = new Vector3(0, -1, 0), Size = 3 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "Size3EngineCluster",
            Title = "S3 KS-25x4 'Mammoth' Liquid Fuel Engine",
            Category = "Engines",
            DryMass = 15.0,
            Cost = 39000,
            BulkheadProfiles = { "size3" },
            Engine = new EngineInfo { MaxThrust = 4000, MinThrust = 0, IspVac = 315, IspAsl = 295, Propellants = { "LiquidFuel", "Oxidizer" } },
            AttachNodes =
            {
                new AttachNode { Id = "top", Position = new Vector3(0, 1.527f, 0), Orientation = new Vector3(0, 1, 0), Size = 3 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "landingLeg1-2",
            Title = "LT-2 Landing Strut",
            Category = "Ground",
            DryMass = 0.1,
            Cost = 340,
            BulkheadProfiles = { "srf" }
        });

        // Aerodynamics & Accessories
        RegisterPart(new PartInfo
        {
            Name = "noseCone",
            Title = "Aerodynamic Nose Cone",
            Category = "Aero",
            DryMass = 0.03,
            Cost = 240,
            BulkheadProfiles = { "size1" },
            AttachNodes =
            {
                new AttachNode { Id = "bottom", Position = new Vector3(0, 0, 0), Orientation = new Vector3(0, -1, 0), Size = 1 }
            }
        });

        RegisterPart(new PartInfo
        {
            Name = "R8winglet",
            Title = "AV-R8 Winglet",
            Category = "Aero",
            DryMass = 0.05,
            Cost = 640,
            BulkheadProfiles = { "srf" }
        });
    }
}
