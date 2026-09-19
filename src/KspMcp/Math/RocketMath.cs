using System;
using System.Collections.Generic;
using System.Linq;
using KspMcp.Parts;

namespace KspMcp.Maths;

public class StageAnalysis
{
    public double PayloadMass { get; set; }
    public double DryMass { get; set; }
    public double WetMass { get; set; }
    public double PropellantMass { get; set; }
    public double DeltaVVac { get; set; }
    public double DeltaVAsl { get; set; }
    public double TwrVac { get; set; }
    public double TwrAsl { get; set; }
    public double BurnTime { get; set; }
    public double TotalThrust { get; set; }
}

public static class RocketMath
{
    public const double StandardGravity = 9.80665;
    public const double KerbinMu = 3.5316000e12; // m^3 / s^2
    public const double KerbinRadius = 600000.0; // 600 km

    /// <summary>
    /// Calculates Tsiolkovsky rocket equation delta-v in m/s.
    /// </summary>
    public static double CalculateDeltaV(double isp, double initialMass, double dryMass)
    {
        if (dryMass <= 0 || initialMass <= 0 || initialMass < dryMass || isp <= 0)
            return 0.0;

        return isp * StandardGravity * Math.Log(initialMass / dryMass);
    }

    /// <summary>
    /// Calculates Thrust-to-Weight Ratio on Kerbin surface.
    /// </summary>
    public static double CalculateTwr(double thrustKn, double massTonnes)
    {
        if (massTonnes <= 0)
            return 0.0;

        var weightKn = massTonnes * StandardGravity;
        return thrustKn / weightKn;
    }

    /// <summary>
    /// Computes circular orbital speed at given altitude above Kerbin (in meters).
    /// </summary>
    public static double CircularOrbitVelocity(double altitudeMeters)
    {
        var r = KerbinRadius + altitudeMeters;
        return Math.Sqrt(KerbinMu / r);
    }

    /// <summary>
    /// Evaluates a proposed rocket stage given payload, fuel tanks, and engine.
    /// </summary>
    public static StageAnalysis AnalyzeStage(PartInfo engine, IEnumerable<PartInfo> fuelTanks, double payloadMass)
    {
        var tanksList = fuelTanks.ToList();
        double tanksDryMass = tanksList.Sum(t => t.DryMass);
        double tanksWetMass = tanksList.Sum(t => t.WetMass);
        double propellantMass = tanksWetMass - tanksDryMass;

        double engineMass = engine.DryMass;
        double engineThrust = engine.Engine?.MaxThrust ?? 0.0;
        double ispVac = engine.Engine?.IspVac ?? 300.0;
        double ispAsl = engine.Engine?.IspAsl ?? (ispVac * 0.85);

        double totalDryMass = payloadMass + engineMass + tanksDryMass;
        double totalWetMass = payloadMass + engineMass + tanksWetMass;

        double deltaVVac = CalculateDeltaV(ispVac, totalWetMass, totalDryMass);
        double deltaVAsl = CalculateDeltaV(ispAsl, totalWetMass, totalDryMass);

        double twrAsl = CalculateTwr(engineThrust, totalWetMass);
        double twrVac = CalculateTwr(engineThrust, totalWetMass);

        double burnTime = 0.0;
        if (engineThrust > 0 && ispVac > 0 && propellantMass > 0)
        {
            // mass flow rate = F / (Isp * g0) in tonnes/sec
            double massFlowRate = (engineThrust) / (ispVac * StandardGravity);
            if (massFlowRate > 0)
            {
                burnTime = propellantMass / massFlowRate;
            }
        }

        return new StageAnalysis
        {
            PayloadMass = payloadMass,
            DryMass = totalDryMass,
            WetMass = totalWetMass,
            PropellantMass = propellantMass,
            DeltaVVac = Math.Round(deltaVVac, 1),
            DeltaVAsl = Math.Round(deltaVAsl, 1),
            TwrVac = Math.Round(twrVac, 2),
            TwrAsl = Math.Round(twrAsl, 2),
            BurnTime = Math.Round(burnTime, 1),
            TotalThrust = engineThrust
        };
    }
}
