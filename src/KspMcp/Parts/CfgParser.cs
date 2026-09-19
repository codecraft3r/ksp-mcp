using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KspMcp.Parts;

public static class CfgParser
{
    public static PartInfo? ParsePartNode(ConfigNode partNode)
    {
        var rawName = partNode.GetValue("name");
        if (string.IsNullOrWhiteSpace(rawName))
            return null;

        var part = new PartInfo
        {
            Name = rawName,
            Title = CleanLocString(partNode.GetValue("title") ?? rawName),
            Category = partNode.GetValue("category") ?? "Unknown",
            DryMass = ParseDouble(partNode.GetValue("mass"), 0.1),
            Cost = ParseDouble(partNode.GetValue("cost"), 100)
        };

        var profiles = partNode.GetValue("bulkheadProfiles");
        if (!string.IsNullOrEmpty(profiles))
        {
            part.BulkheadProfiles.AddRange(profiles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        // Parse attach nodes
        foreach (var kvp in partNode.Values)
        {
            if (kvp.Key.StartsWith("node_stack_", StringComparison.OrdinalIgnoreCase))
            {
                var nodeId = kvp.Key.Substring("node_stack_".Length);
                var node = AttachNode.Parse(kvp.Value, nodeId);
                if (node != null)
                {
                    part.AttachNodes.Add(node);
                }
            }
        }

        // Parse resources
        foreach (var resNode in partNode.GetNodes("RESOURCE"))
        {
            var resName = resNode.GetValue("name");
            if (!string.IsNullOrEmpty(resName))
            {
                var amount = ParseDouble(resNode.GetValue("amount"), 0);
                var maxAmount = ParseDouble(resNode.GetValue("maxAmount"), amount);
                part.Resources.Add(new PartResource
                {
                    Name = resName,
                    Amount = amount,
                    MaxAmount = maxAmount
                });
            }
        }

        // Parse modules
        foreach (var modNode in partNode.GetNodes("MODULE"))
        {
            var modName = modNode.GetValue("name");
            if (string.IsNullOrEmpty(modName))
                continue;

            if (modName.Equals("ModuleCommand", StringComparison.OrdinalIgnoreCase))
            {
                part.IsCommand = true;
            }
            else if (modName.Contains("Decouple", StringComparison.OrdinalIgnoreCase) || modName.Contains("Separator", StringComparison.OrdinalIgnoreCase))
            {
                part.IsDecoupler = true;
            }
            else if (modName.Contains("Parachute", StringComparison.OrdinalIgnoreCase))
            {
                part.IsParachute = true;
            }
            else if (modName.Contains("kOSProcessor", StringComparison.OrdinalIgnoreCase))
            {
                part.HasKos = true;
            }
            else if (modName.Equals("ModuleEngines", StringComparison.OrdinalIgnoreCase) ||
                     modName.Equals("ModuleEnginesFX", StringComparison.OrdinalIgnoreCase))
            {
                part.Engine = ParseEngineModule(modNode);
            }
        }

        return part;
    }

    private static EngineInfo ParseEngineModule(ConfigNode modNode)
    {
        var engine = new EngineInfo
        {
            MaxThrust = ParseDouble(modNode.GetValue("maxThrust"), 100),
            MinThrust = ParseDouble(modNode.GetValue("minThrust"), 0)
        };

        var atmCurve = modNode.GetNode("atmosphereCurve");
        if (atmCurve != null)
        {
            foreach (var keyLine in atmCurve.GetValues("key"))
            {
                var parts = keyLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var pressure = ParseDouble(parts[0], 0);
                    var isp = ParseDouble(parts[1], 300);
                    if (Math.Abs(pressure) < 0.05)
                    {
                        engine.IspVac = isp;
                    }
                    else if (Math.Abs(pressure - 1.0) < 0.1)
                    {
                        engine.IspAsl = isp;
                    }
                }
            }
        }

        if (engine.IspVac <= 0) engine.IspVac = 300;
        if (engine.IspAsl <= 0) engine.IspAsl = engine.IspVac * 0.85;

        var propellants = modNode.GetNodes("PROPELLANT");
        foreach (var prop in propellants)
        {
            var pName = prop.GetValue("name");
            if (!string.IsNullOrEmpty(pName))
                engine.Propellants.Add(pName);
        }

        return engine;
    }

    private static string CleanLocString(string text)
    {
        if (text.StartsWith("#autoLOC_") && text.Contains('='))
        {
            var eqIdx = text.LastIndexOf('=');
            return text.Substring(eqIdx + 1).Trim();
        }
        return text;
    }

    private static double ParseDouble(string? s, double fallback)
    {
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;
        return fallback;
    }
}
