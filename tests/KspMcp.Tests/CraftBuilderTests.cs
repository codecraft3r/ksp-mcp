using System.Linq;
using KspMcp.Craft;
using KspMcp.Parts;
using Xunit;

namespace KspMcp.Tests;

public class CraftBuilderTests
{
    [Fact]
    public void BuildLaunchVehicle_GeneratesValidVesselGraph()
    {
        var catalog = new PartCatalog();
        var builder = new StageBuilder(catalog);

        var config = new LaunchVehicleConfig
        {
            VesselName = "Unit_Test_Rocket",
            PayloadType = "Crewed",
            IncludeParachute = true,
            IncludeHeatShield = true,
            BoosterTankCount = 2,
            IncludeFins = true
        };

        var vessel = builder.BuildLaunchVehicle(config);
        Assert.NotNull(vessel.RootPart);
        Assert.Equal("mk1pod_v2", vessel.RootPart.Part.Name);
        Assert.True(vessel.Parts.Count >= 10, $"Expected >= 10 parts, got {vessel.Parts.Count}");

        // Validate node connectivity
        var validation = CraftValidator.ValidateVessel(vessel);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.True(validation.HasCommand);
        Assert.True(validation.HasEngines);
        Assert.True(validation.TotalMass > 10.0);

        // Verify Staging Order: Staging indices should be assigned
        var stagedParts = vessel.Parts.Where(p => p.IgnitionStage >= 0).ToList();
        Assert.NotEmpty(stagedParts);

        // Booster engine should have higher stage index than upper engine
        var boosterEngine = vessel.Parts.First(p => p.Part.Name == "liquidEngine_v2");
        var upperEngine = vessel.Parts.First(p => p.Part.Name == "liquidEngine3_v2");
        Assert.True(boosterEngine.IgnitionStage > upperEngine.IgnitionStage,
            $"Booster stage {boosterEngine.IgnitionStage} should be > upper stage {upperEngine.IgnitionStage}");
    }

    [Fact]
    public void CraftWriter_GeneratesSyntacticallyValidCraftString()
    {
        var catalog = new PartCatalog();
        var builder = new StageBuilder(catalog);

        var vessel = builder.BuildLaunchVehicle(new LaunchVehicleConfig
        {
            VesselName = "String_Test_Rocket",
            PayloadType = "Probe",
            IncludeParachute = false,
            IncludeHeatShield = false
        });

        var craftText = CraftWriter.GenerateCraftString(vessel);
        Assert.Contains("ship = String_Test_Rocket", craftText);
        Assert.Contains("type = VAB", craftText);
        Assert.Contains("PART", craftText);
        Assert.Contains("attN = ", craftText);
        Assert.Contains("link = ", craftText);

        var parsedNodes = ConfigNode.ParseString(craftText);
        var partNodes = parsedNodes.Where(n => n.Name == "PART").ToList();
        Assert.Equal(vessel.Parts.Count, partNodes.Count);
    }
}
