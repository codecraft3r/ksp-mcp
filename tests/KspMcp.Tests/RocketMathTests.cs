using System;
using KspMcp.Maths;
using KspMcp.Parts;
using Xunit;

namespace KspMcp.Tests;

public class RocketMathTests
{
    [Fact]
    public void CalculateDeltaV_MatchesTsiolkovskyFormula()
    {
        // Isp = 300s, m0 = 10t, mf = 5t
        // dV = 300 * 9.80665 * ln(2) = 2039.29 m/s
        var dv = RocketMath.CalculateDeltaV(300, 10, 5);
        Assert.InRange(dv, 2039.0, 2040.0);
    }

    [Fact]
    public void CalculateTwr_ComputesCorrectRatio()
    {
        // 200 kN thrust, 10 tonnes mass
        // weight = 10 * 9.80665 = 98.0665 kN -> TWR = 200 / 98.0665 = 2.0394
        var twr = RocketMath.CalculateTwr(200, 10);
        Assert.InRange(twr, 2.03, 2.05);
    }

    [Fact]
    public void CircularOrbitVelocity_ComputesCorrectSpeedAt80km()
    {
        // Kerbin r = 600km + 80km = 680,000 m
        // mu = 3.5316e12
        // v = sqrt(3.5316e12 / 680000) = 2278.9 m/s
        var v = RocketMath.CircularOrbitVelocity(80000);
        Assert.InRange(v, 2278.0, 2280.0);
    }

    [Fact]
    public void AnalyzeStage_ComputesCompleteStageMetrics()
    {
        var catalog = new PartCatalog();
        var engine = catalog.GetPart("liquidEngine_v2");
        var tank = catalog.GetPart("fuelTank_long");

        Assert.NotNull(engine);
        Assert.NotNull(tank);

        var stage = RocketMath.AnalyzeStage(engine, new[] { tank }, 1.0);
        Assert.True(stage.DeltaVVac > 1500, $"Expected dV > 1500, got {stage.DeltaVVac}");
        Assert.True(stage.TwrAsl > 1.5, $"Expected TWR > 1.5, got {stage.TwrAsl}");
        Assert.True(stage.BurnTime > 20, $"Expected burn time > 20s, got {stage.BurnTime}");
    }
}
