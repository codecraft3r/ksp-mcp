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

    [Fact]
    public void BuildTyloMasterLander_GeneratesExtremeHeavyVesselWithLandingGear()
    {
        var catalog = new PartCatalog();
        var builder = new StageBuilder(catalog);

        var vessel = builder.BuildTyloMasterLander("Tylo_Test_Vessel");
        Assert.NotNull(vessel.RootPart);
        Assert.Equal(24, vessel.Parts.Count);

        var validation = CraftValidator.ValidateVessel(vessel);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        Assert.True(validation.HasCommand);
        Assert.True(validation.HasEngines);
        Assert.True(validation.TotalMass > 200.0, $"Expected mass > 200t, got {validation.TotalMass}t");

        // Verify landing gear
        var legs = vessel.Parts.Where(p => p.Part.Name == "landingLeg1-2").ToList();
        Assert.Equal(4, legs.Count);

        // Verify multi-stage staging order: Mammoth (8) > Rhino (6) > Poodle (4) > Terrier (2)
        var mammoth = vessel.Parts.First(p => p.Part.Name == "Size3EngineCluster");
        var rhino = vessel.Parts.First(p => p.Part.Name == "Size3AdvancedEngine");
        var poodle = vessel.Parts.First(p => p.Part.Name == "liquidEngine2-2_v2");
        var terrier = vessel.Parts.First(p => p.Part.Name == "liquidEngine3_v2");

        Assert.Equal(8, mammoth.IgnitionStage);
        Assert.Equal(6, rhino.IgnitionStage);
        Assert.Equal(4, poodle.IgnitionStage);
        Assert.Equal(2, terrier.IgnitionStage);

        var craftText = CraftWriter.GenerateCraftString(vessel);
        Assert.Contains("ship = Tylo_Test_Vessel", craftText);
        Assert.Contains("Size3EngineCluster", craftText);
        Assert.Contains("Size3AdvancedEngine", craftText);
        Assert.Contains("liquidEngine2-2.v2", craftText);
        Assert.Contains("landingLeg1-2", craftText);
    }
}
