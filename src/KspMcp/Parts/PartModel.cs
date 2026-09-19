using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace KspMcp.Parts;

public class AttachNode
{
    public string Id { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public Vector3 Orientation { get; set; }
    public int Size { get; set; } = 1;

    public static AttachNode? Parse(string line, string? explicitId = null)
    {
        // Example: node_stack_top = 0.0, 0.6423756, 0.0, 0.0, 1.0, 0.0, 0
        var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 6)
            return null;

        if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y) ||
            !float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z) ||
            !float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ox) ||
            !float.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var oy) ||
            !float.TryParse(parts[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var oz))
        {
            return null;
        }

        int size = 1;
        if (parts.Length >= 7 && int.TryParse(parts[6], out var s))
        {
            size = s;
        }

        return new AttachNode
        {
            Id = explicitId ?? "stack",
            Position = new Vector3(x, y, z),
            Orientation = new Vector3(ox, oy, oz),
            Size = size
        };
    }
}

public class PartResource
{
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double MaxAmount { get; set; }

    public double Density => Name switch
    {
        "LiquidFuel" => 0.005,
        "Oxidizer" => 0.005,
        "SolidFuel" => 0.0075,
        "MonoPropellant" or "Monopropellant" => 0.004,
        "XenonGas" => 0.0001,
        "Ore" => 0.01,
        _ => 0.0
    };

    public double Mass => MaxAmount * Density;
}

public class EngineInfo
{
    public double MaxThrust { get; set; }
    public double MinThrust { get; set; }
    public double IspVac { get; set; }
    public double IspAsl { get; set; }
    public List<string> Propellants { get; set; } = new();
}

public class PartInfo
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double DryMass { get; set; }
    public double Cost { get; set; }
    public List<string> BulkheadProfiles { get; set; } = new();
    public List<AttachNode> AttachNodes { get; set; } = new();
    public List<PartResource> Resources { get; set; } = new();
    public EngineInfo? Engine { get; set; }
    public bool IsCommand { get; set; }
    public bool IsDecoupler { get; set; }
    public bool IsParachute { get; set; }
    public bool HasKos { get; set; }

    public double WetMass => DryMass + Resources.Sum(r => r.Mass);

    public AttachNode? TopNode => AttachNodes.FirstOrDefault(n => n.Id.Equals("top", StringComparison.OrdinalIgnoreCase) || n.Id.Equals("node_stack_top", StringComparison.OrdinalIgnoreCase));
    public AttachNode? BottomNode => AttachNodes.FirstOrDefault(n => n.Id.Equals("bottom", StringComparison.OrdinalIgnoreCase) || n.Id.Equals("node_stack_bottom", StringComparison.OrdinalIgnoreCase));
}
