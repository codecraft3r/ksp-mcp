using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using KspMcp.Configuration;

namespace KspMcp.Kos;

public class KosScriptInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}

public class ScriptValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}

public class KosScriptManager
{
    private readonly KspConfig _config;

    public KosScriptManager(KspConfig config)
    {
        _config = config;
    }

    public void DeployHelperLibraries()
    {
        var scriptDir = _config.ShipsScriptPath;
        if (!Directory.Exists(scriptDir))
        {
            Directory.CreateDirectory(scriptDir);
        }

        File.WriteAllText(Path.Combine(scriptDir, "lib_math.ks"), LibMathContent);
        File.WriteAllText(Path.Combine(scriptDir, "lib_ascent.ks"), LibAscentContent);
        File.WriteAllText(Path.Combine(scriptDir, "lib_node.ks"), LibNodeContent);
        File.WriteAllText(Path.Combine(scriptDir, "lib_orbit.ks"), LibOrbitContent);
        File.WriteAllText(Path.Combine(scriptDir, "lib_telemetry.ks"), LibTelemetryContent);
    }

    public IEnumerable<KosScriptInfo> ListScripts()
    {
        var scriptDir = _config.ShipsScriptPath;
        if (!Directory.Exists(scriptDir))
            return Enumerable.Empty<KosScriptInfo>();

        return Directory.EnumerateFiles(scriptDir, "*.ks", SearchOption.AllDirectories)
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new KosScriptInfo
                {
                    Name = Path.GetRelativePath(scriptDir, f),
                    Path = f,
                    SizeBytes = fi.Length,
                    LastModified = fi.LastWriteTimeUtc
                };
            });
    }

    public string? ReadScript(string scriptName)
    {
        var scriptPath = ResolvePath(scriptName);
        if (!File.Exists(scriptPath))
            return null;

        return File.ReadAllText(scriptPath);
    }

    public ScriptValidationResult WriteScript(string scriptName, string content)
    {
        var validation = ValidateKerboScript(content);
        if (!validation.IsValid)
            return validation;

        var scriptPath = ResolvePath(scriptName);
        var dir = Path.GetDirectoryName(scriptPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(scriptPath, content);
        return validation;
    }

    public ScriptValidationResult ValidateKerboScript(string code)
    {
        var result = new ScriptValidationResult();

        // 1. Bracket and parenthesis balancing
        int braceDepth = 0;
        int parenDepth = 0;
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var line = StripComments(raw).Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            foreach (var ch in line)
            {
                if (ch == '{') braceDepth++;
                else if (ch == '}') braceDepth--;
                else if (ch == '(') parenDepth++;
                else if (ch == ')') parenDepth--;

                if (braceDepth < 0)
                {
                    result.Errors.Add($"Line {i + 1}: Unexpected closing brace '}}'");
                    braceDepth = 0;
                }
                if (parenDepth < 0)
                {
                    result.Errors.Add($"Line {i + 1}: Unexpected closing parenthesis ')'");
                    parenDepth = 0;
                }
            }

            // KerboScript statements typically must end with a period '.', except blocks ending in '{' or '}'
            if (!line.EndsWith('.') && !line.EndsWith('{') && !line.EndsWith('}') && !line.EndsWith(',') && !line.StartsWith("//"))
            {
                // Soft warning for missing period
                result.Warnings.Add($"Line {i + 1}: Statement may be missing terminating period ('.'): \"{line}\"");
            }
        }

        if (braceDepth > 0)
        {
            result.Errors.Add($"Unclosed brace '{ braceDepth }' at end of script.");
        }
        if (parenDepth > 0)
        {
            result.Errors.Add($"Unclosed parenthesis '{ parenDepth }' at end of script.");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private string ResolvePath(string scriptName)
    {
        if (!scriptName.EndsWith(".ks", StringComparison.OrdinalIgnoreCase))
            scriptName += ".ks";

        return Path.Combine(_config.ShipsScriptPath, scriptName);
    }

    private static string StripComments(string line)
    {
        var idx = line.IndexOf("//", StringComparison.Ordinal);
        return idx >= 0 ? line.Substring(0, idx) : line;
    }

    public const string LibMathContent = @"// lib_math.ks - KSP kOS Astrodynamics and Math Functions
@LAZYGLOBAL OFF.

GLOBAL FUNCTION CircularOrbitSpeed {
    PARAMETER altMeters.
    LOCAL radiusVal IS BODY:RADIUS + altMeters.
    RETURN SQRT(BODY:MU / radiusVal).
}

GLOBAL FUNCTION Clamp {
    PARAMETER val, minVal, maxVal.
    IF val < minVal RETURN minVal.
    IF val > maxVal RETURN maxVal.
    RETURN val.
}
";

    public const string LibAscentContent = @"// lib_ascent.ks - KSP Autonomous Ascent & Gravity Turn Guidance
@LAZYGLOBAL OFF.

GLOBAL FUNCTION LaunchToOrbit {
    PARAMETER targetApoapsis, targetHeading IS 90.

    SAS OFF.
    RCS OFF.
    LOCAL turnStartAlt IS 1000.
    LOCAL turnEndAlt IS 55000.

    LOCK THROTTLE TO 1.0.
    LOCK STEERING TO HEADING(targetHeading, 90).

    PRINT ""T-0: Ignition and Lift-off!"".
    STAGE.

    // Auto-staging trigger
    WHEN STAGE:NUMBER > 2 AND MAXTHRUST = 0 THEN {
        PRINT ""Stage flameout detected. Staging!"".
        STAGE.
        PRESERVE.
    }

    // Ascent gravity turn loop
    UNTIL SHIP:APOAPSIS >= targetApoapsis {
        IF SHIP:ALTITUDE > turnStartAlt {
            LOCAL frac IS (SHIP:ALTITUDE - turnStartAlt) / (turnEndAlt - turnStartAlt).
            IF frac > 1.0 SET frac TO 1.0.
            LOCAL targetPitch IS 90 - (frac * 85).
            LOCK STEERING TO HEADING(targetHeading, targetPitch).
        }
        WAIT 0.1.
    }

    PRINT ""Target apoapsis reached: "" + ROUND(SHIP:APOAPSIS) + ""m. MECO!"".
    LOCK THROTTLE TO 0.
    SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.

    // Coast out of atmosphere
    IF SHIP:ALTITUDE < 70000 {
        PRINT ""Coasting out of atmosphere (70km)..."".
        WAIT UNTIL SHIP:ALTITUDE > 70000.
    }
    PRINT ""Outside atmosphere. Ready for circularization."".
}
";

    public const string LibOrbitContent = @"// lib_orbit.ks - Orbital Maneuver Planning
@LAZYGLOBAL OFF.

GLOBAL FUNCTION PlanCircularizationAtApoapsis {
    LOCAL rApo IS BODY:RADIUS + SHIP:APOAPSIS.
    LOCAL vApoCurrent IS SQRT(BODY:MU * (2 / rApo - 1 / SHIP:OBT:SEMIMAJORAXIS)).
    LOCAL vCirc IS SQRT(BODY:MU / rApo).
    LOCAL dV IS vCirc - vApoCurrent.

    LOCAL circNode IS NODE(TIME:SECONDS + ETA:APOAPSIS, 0, 0, dV).
    ADD circNode.
    PRINT ""Circularization node created at Apoapsis: dV = "" + ROUND(dV, 1) + "" m/s."".
    RETURN circNode.
}
";

    public const string LibNodeContent = @"// lib_node.ks - Maneuver Node Execution
@LAZYGLOBAL OFF.

GLOBAL FUNCTION ExecuteNextNode {
    IF NOT HASNODE {
        PRINT ""No maneuver node found to execute."".
        RETURN.
    }

    LOCAL nd IS NEXTNODE.
    LOCAL dV0 IS nd:DELTAV:MAG.
    LOCAL v0 IS nd:DELTAV.

    // Estimate burn time: t = dV * m / F
    LOCAL thrustVal IS MAXTHRUST.
    IF thrustVal <= 0 SET thrustVal TO 60.
    LOCAL burnDuration IS (dV0 * SHIP:MASS) / thrustVal.

    PRINT ""Node dV: "" + ROUND(dV0, 1) + "" m/s. Est Burn Time: "" + ROUND(burnDuration, 1) + ""s."".

    // Orient toward burn vector
    LOCK STEERING TO nd:DELTAV.
    PRINT ""Aligning to maneuver vector..."".
    WAIT 5.

    // Warp to node minus half burn time
    LOCAL burnStart IS nd:TIME - (burnDuration / 2).
    IF burnStart > TIME:SECONDS + 15 {
        WARPTO(burnStart - 10).
    }

    WAIT UNTIL TIME:SECONDS >= burnStart.

    PRINT ""Executing burn!"".
    WHEN STAGE:NUMBER > 2 AND MAXTHRUST = 0 THEN {
        STAGE.
        PRESERVE.
    }

    UNTIL VDOT(v0, nd:DELTAV) <= 0.5 OR nd:DELTAV:MAG < 0.2 {
        LOCAL throttleVal IS 1.0.
        IF nd:DELTAV:MAG < 10 {
            SET throttleVal TO MAX(0.05, nd:DELTAV:MAG / 10).
        }
        LOCK THROTTLE TO throttleVal.
        WAIT 0.05.
    }

    LOCK THROTTLE TO 0.
    SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.
    UNLOCK STEERING.
    REMOVE nd.
    PRINT ""Maneuver burn complete!"".
}
";

    public const string LibTelemetryContent = @"// lib_telemetry.ks - Real-time JSON Telemetry Reporter
@LAZYGLOBAL OFF.

GLOBAL FUNCTION PrintTelemetryJson {
    LOCAL apo IS ROUND(SHIP:APOAPSIS).
    LOCAL peri IS ROUND(SHIP:PERIAPSIS).
    LOCAL alt IS ROUND(SHIP:ALTITUDE).
    LOCAL speed IS ROUND(SHIP:VELOCITY:ORBIT:MAG, 1).
    LOCAL throt IS ROUND(THROTTLE, 2).

    PRINT ""{""""altitude"""":"" + alt + "", """"apoapsis"""":"" + apo + "", """"periapsis"""":"" + peri + "", """"orbital_speed"""":"" + speed + "", """"throttle"""":"" + throt + "", """"mass"""":"" + ROUND(SHIP:MASS, 2) + ""}"" AT(0, 0).
}
";
}
