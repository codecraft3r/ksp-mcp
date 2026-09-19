using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KspMcp.Parts;

namespace KspMcp.Craft;

public class CraftValidationResult
{
    public bool IsValid { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public int PartCount { get; set; }
    public double TotalMass { get; set; }
    public bool HasCommand { get; set; }
    public bool HasEngines { get; set; }
    public bool HasKos { get; set; }
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
}

public static class CraftValidator
{
    public static CraftValidationResult ValidateVessel(CraftVessel vessel)
    {
        var result = new CraftValidationResult
        {
            VesselName = vessel.Name,
            PartCount = vessel.Parts.Count
        };

        if (vessel.RootPart == null)
        {
            result.Errors.Add("Vessel has no root part.");
        }

        if (vessel.Parts.Count == 0)
        {
            result.Errors.Add("Vessel has 0 parts.");
            result.IsValid = false;
            return result;
        }

        double totalMass = 0;
        foreach (var p in vessel.Parts)
        {
            totalMass += p.Part.WetMass;
            if (p.Part.IsCommand) result.HasCommand = true;
            if (p.Part.Engine != null) result.HasEngines = true;
            if (p.Part.HasKos) result.HasKos = true;

            // Check node connections
            foreach (var kvp in p.NodeConnections)
            {
                var otherPart = kvp.Value.OtherPart;
                if (!vessel.Parts.Contains(otherPart))
                {
                    result.Errors.Add($"Part {p.FullId} connects to {otherPart.FullId} which is not in the vessel parts list.");
                }
            }
        }

        result.TotalMass = Math.Round(totalMass, 2);

        if (!result.HasCommand)
        {
            result.Errors.Add("Vessel has no command pod or probe core.");
        }

        if (!result.HasEngines)
        {
            result.Warnings.Add("Vessel has no propulsion engines (unpowered glider or station).");
        }

        if (!result.HasKos)
        {
            result.Warnings.Add("Vessel does not have a dedicated kOS processor (requires kOSforAll mod to be active).");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public static CraftValidationResult ValidateFile(string craftFilePath, PartCatalog catalog)
    {
        var result = new CraftValidationResult();
        if (!File.Exists(craftFilePath))
        {
            result.Errors.Add($"Craft file does not exist: {craftFilePath}");
            result.IsValid = false;
            return result;
        }

        var text = File.ReadAllText(craftFilePath);
        var nodes = ConfigNode.ParseString(text);

        result.VesselName = Path.GetFileNameWithoutExtension(craftFilePath);
        int partCount = 0;
        double totalMass = 0;

        foreach (var node in nodes)
        {
            if (node.Name.Equals("PART", StringComparison.OrdinalIgnoreCase))
            {
                partCount++;
                var partEntry = node.GetValue("part");
                if (!string.IsNullOrEmpty(partEntry))
                {
                    var baseName = partEntry.Contains('_') ? partEntry.Substring(0, partEntry.LastIndexOf('_')) : partEntry;
                    var partInfo = catalog.GetPart(baseName);
                    if (partInfo != null)
                    {
                        totalMass += partInfo.WetMass;
                        if (partInfo.IsCommand) result.HasCommand = true;
                        if (partInfo.Engine != null) result.HasEngines = true;
                        if (partInfo.HasKos) result.HasKos = true;
                    }
                }
            }
        }

        result.PartCount = partCount;
        result.TotalMass = Math.Round(totalMass, 2);
        if (partCount == 0) result.Errors.Add("No PART entries found in craft file.");
        if (!result.HasCommand) result.Warnings.Add("No command module recognized in craft file.");

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}
