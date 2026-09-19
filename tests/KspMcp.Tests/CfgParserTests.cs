using System.Linq;
using System.Numerics;
using KspMcp.Parts;
using Xunit;

namespace KspMcp.Tests;

public class CfgParserTests
{
    [Fact]
    public void ParseString_ParsesNestedConfigNodesCorrectly()
    {
        var raw = @"
PART
{
    name = testPod
    module = Part
    mass = 1.2
    cost = 800
    category = Pods
    node_stack_top = 0.0, 1.0, 0.0, 0.0, 1.0, 0.0, 1
    node_stack_bottom = 0.0, -1.0, 0.0, 0.0, -1.0, 0.0, 1

    RESOURCE
    {
        name = ElectricCharge
        amount = 100
        maxAmount = 100
    }

    MODULE
    {
        name = ModuleCommand
        minimumCrew = 1
    }
}";

        var nodes = ConfigNode.ParseString(raw);
        Assert.Single(nodes);
        Assert.Equal("PART", nodes[0].Name);

        var part = CfgParser.ParsePartNode(nodes[0]);
        Assert.NotNull(part);
        Assert.Equal("testPod", part.Name);
        Assert.Equal(1.2, part.DryMass);
        Assert.Equal(800, part.Cost);
        Assert.True(part.IsCommand);
        Assert.Equal(2, part.AttachNodes.Count);
        Assert.Single(part.Resources);
        Assert.Equal("ElectricCharge", part.Resources[0].Name);
        Assert.Equal(100, part.Resources[0].Amount);

        var top = part.TopNode;
        Assert.NotNull(top);
        Assert.Equal(new Vector3(0, 1, 0), top.Position);

        var bottom = part.BottomNode;
        Assert.NotNull(bottom);
        Assert.Equal(new Vector3(0, -1, 0), bottom.Position);
    }

    [Fact]
    public void ParsePartNode_ParsesEngineModuleStats()
    {
        var raw = @"
PART
{
    name = testEngine
    mass = 1.5
    cost = 1200
    category = Engines
    node_stack_top = 0, 0.5, 0, 0, 1, 0, 1
    node_stack_bottom = 0, -0.5, 0, 0, -1, 0, 1

    MODULE
    {
        name = ModuleEngines
        maxThrust = 215
        minThrust = 0
        atmosphereCurve
        {
            key = 0 320
            key = 1 250
        }
        PROPELLANT
        {
            name = LiquidFuel
            ratio = 0.9
        }
        PROPELLANT
        {
            name = Oxidizer
            ratio = 1.1
        }
    }
}";

        var nodes = ConfigNode.ParseString(raw);
        var part = CfgParser.ParsePartNode(nodes[0]);
        Assert.NotNull(part);
        Assert.NotNull(part.Engine);
        Assert.Equal(215, part.Engine.MaxThrust);
        Assert.Equal(320, part.Engine.IspVac);
        Assert.Equal(250, part.Engine.IspAsl);
        Assert.Equal(2, part.Engine.Propellants.Count);
    }
}
