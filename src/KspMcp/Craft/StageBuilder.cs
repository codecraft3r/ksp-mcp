using System;
using System.Collections.Generic;
using System.Numerics;
using KspMcp.Parts;

namespace KspMcp.Craft;

public class LaunchVehicleConfig
{
    public string VesselName { get; set; } = "Ai_Orbiter_Mk1";
    public string PayloadType { get; set; } = "Crewed"; // "Crewed" or "Probe"
    public bool IncludeParachute { get; set; } = true;
    public bool IncludeHeatShield { get; set; } = true;
    public string UpperEngineName { get; set; } = "liquidEngine3_v2"; // Terrier
    public string UpperTankName { get; set; } = "fuelTank_long"; // FL-T800
    public int UpperTankCount { get; set; } = 1;
    public string BoosterEngineName { get; set; } = "liquidEngine_v2"; // Swivel
    public string BoosterTankName { get; set; } = "fuelTank_long"; // FL-T800
    public int BoosterTankCount { get; set; } = 2;
    public bool IncludeFins { get; set; } = true;
}

public class StageBuilder
{
    private readonly PartCatalog _catalog;
    private uint _nextUid = 4294000001;
    private uint _nextPersistentId = 100000001;

    public StageBuilder(PartCatalog catalog)
    {
        _catalog = catalog;
    }

    public CraftVessel BuildLaunchVehicle(LaunchVehicleConfig config)
    {
        var vessel = new CraftVessel
        {
            Name = config.VesselName,
            Description = $"Autonomous launch vehicle designed for orbital insertion: {config.PayloadType} payload.",
            Type = "VAB",
            VesselType = config.PayloadType.Equals("Probe", StringComparison.OrdinalIgnoreCase) ? "Probe" : "Ship"
        };

        // 1. Root Command Module
        var rootPartInfo = config.PayloadType.Equals("Probe", StringComparison.OrdinalIgnoreCase)
            ? _catalog.GetPart("probeCoreOcto_v2") ?? _catalog.GetPart("probeCoreOcto")
            : _catalog.GetPart("mk1pod_v2") ?? _catalog.GetPart("mk1pod");

        if (rootPartInfo == null)
            throw new InvalidOperationException("Could not find command module in parts catalog.");

        var root = CreateInstance(rootPartInfo, new Vector3(0, 15f, 0));
        vessel.RootPart = root;
        vessel.Parts.Add(root);

        CraftPartInstance? chute = null;
        if (config.IncludeParachute)
        {
            var chuteInfo = _catalog.GetPart("parachuteSingle");
            if (chuteInfo != null && root.Part.TopNode != null && chuteInfo.BottomNode != null)
            {
                chute = CreateInstance(chuteInfo);
                AttachStack(root, root.Part.TopNode.Id, chute, chuteInfo.BottomNode.Id);
                vessel.Parts.Add(chute);
            }
        }

        // Bottom of payload: Heat shield or directly to Decoupler
        CraftPartInstance payloadBottom = root;
        if (config.IncludeHeatShield && !config.PayloadType.Equals("Probe", StringComparison.OrdinalIgnoreCase))
        {
            var hsInfo = _catalog.GetPart("HeatShield1");
            if (hsInfo != null && payloadBottom.Part.BottomNode != null && hsInfo.TopNode != null)
            {
                var hs = CreateInstance(hsInfo);
                AttachStack(payloadBottom, payloadBottom.Part.BottomNode.Id, hs, hsInfo.TopNode.Id);
                vessel.Parts.Add(hs);
                payloadBottom = hs;
            }
        }

        // Payload Decoupler
        var decInfo1 = _catalog.GetPart("Decoupler_1");
        if (decInfo1 == null)
            throw new InvalidOperationException("Could not find Decoupler_1 in parts catalog.");

        var dec1 = CreateInstance(decInfo1);
        AttachStack(payloadBottom, payloadBottom.Part.BottomNode!.Id, dec1, decInfo1.TopNode!.Id);
        vessel.Parts.Add(dec1);

        // Upper Stage: Fuel Tanks
        var upperTankInfo = _catalog.GetPart(config.UpperTankName) ?? _catalog.GetPart("fuelTank_long");
        if (upperTankInfo == null)
            throw new InvalidOperationException($"Could not find upper tank {config.UpperTankName}.");

        CraftPartInstance currentStack = dec1;
        for (int i = 0; i < Math.Max(1, config.UpperTankCount); i++)
        {
            var tank = CreateInstance(upperTankInfo);
            AttachStack(currentStack, currentStack.Part.BottomNode!.Id, tank, upperTankInfo.TopNode!.Id);
            vessel.Parts.Add(tank);
            currentStack = tank;
        }

        // Upper Stage: Engine
        var upperEngineInfo = _catalog.GetPart(config.UpperEngineName) ?? _catalog.GetPart("liquidEngine3_v2");
        if (upperEngineInfo == null)
            throw new InvalidOperationException($"Could not find upper engine {config.UpperEngineName}.");

        var upperEngine = CreateInstance(upperEngineInfo);
        AttachStack(currentStack, currentStack.Part.BottomNode!.Id, upperEngine, upperEngineInfo.TopNode!.Id);
        vessel.Parts.Add(upperEngine);
        currentStack = upperEngine;

        // Booster Decoupler
        var dec2 = CreateInstance(decInfo1);
        AttachStack(currentStack, currentStack.Part.BottomNode!.Id, dec2, decInfo1.TopNode!.Id);
        vessel.Parts.Add(dec2);
        currentStack = dec2;

        // Booster Stage: Tanks
        var boosterTankInfo = _catalog.GetPart(config.BoosterTankName) ?? _catalog.GetPart("fuelTank_long");
        if (boosterTankInfo == null)
            throw new InvalidOperationException($"Could not find booster tank {config.BoosterTankName}.");

        CraftPartInstance? firstBoosterTank = null;
        for (int i = 0; i < Math.Max(1, config.BoosterTankCount); i++)
        {
            var tank = CreateInstance(boosterTankInfo);
            AttachStack(currentStack, currentStack.Part.BottomNode!.Id, tank, boosterTankInfo.TopNode!.Id);
            vessel.Parts.Add(tank);
            currentStack = tank;
            if (firstBoosterTank == null) firstBoosterTank = tank;
        }

        // Booster Stage: Engine
        var boosterEngineInfo = _catalog.GetPart(config.BoosterEngineName) ?? _catalog.GetPart("liquidEngine_v2");
        if (boosterEngineInfo == null)
            throw new InvalidOperationException($"Could not find booster engine {config.BoosterEngineName}.");

        var boosterEngine = CreateInstance(boosterEngineInfo);
        AttachStack(currentStack, currentStack.Part.BottomNode!.Id, boosterEngine, boosterEngineInfo.TopNode!.Id);
        vessel.Parts.Add(boosterEngine);

        // Aerodynamics: Fins
        if (config.IncludeFins && firstBoosterTank != null)
        {
            var finInfo = _catalog.GetPart("R8winglet");
            if (finInfo != null)
            {
                // Attach 4 fins around the bottom of the booster stack
                var angles = new[] { 0.0, 90.0, 180.0, 270.0 };
                var radius = 0.65f;
                var finY = currentStack.Position.Y + 0.5f;

                foreach (var deg in angles)
                {
                    var rad = deg * (Math.PI / 180.0);
                    var fx = (float)(Math.Cos(rad) * radius);
                    var fz = (float)(Math.Sin(rad) * radius);
                    var fin = CreateInstance(finInfo, new Vector3(fx, finY, fz));
                    firstBoosterTank.Children.Add(fin);
                    fin.Parent = firstBoosterTank;
                    vessel.Parts.Add(fin);
                }
            }
        }

        // Staging Setup (Descending index: Highest is ignited first)
        // Stage 4: Booster Engine
        // Stage 3: Booster Decoupler
        // Stage 2: Upper Engine
        // Stage 1: Payload Decoupler
        // Stage 0: Parachute
        int currentStage = 0;
        if (chute != null)
        {
            chute.IgnitionStage = currentStage;
            chute.StageIndex = 0;
        }

        currentStage++;
        dec1.IgnitionStage = currentStage;
        dec1.DecoupleStage = currentStage;
        dec1.StageIndex = 0;

        currentStage++;
        upperEngine.IgnitionStage = currentStage;
        upperEngine.StageIndex = 0;

        currentStage++;
        dec2.IgnitionStage = currentStage;
        dec2.DecoupleStage = currentStage;
        dec2.StageIndex = 0;

        currentStage++;
        boosterEngine.IgnitionStage = currentStage;
        boosterEngine.StageIndex = 0;

        return vessel;
    }

    public CraftVessel BuildJoolRoundTripLander(string vesselName = "Jool_Explorer_I")
    {
        var vessel = new CraftVessel
        {
            Name = vesselName,
            Description = "Autonomous interplanetary heavy vehicle designed for Jool arrival, Vall/moon landing, and Kerbin return.",
            Type = "VAB",
            VesselType = "Ship"
        };

        // 1. Reentry Capsule
        var podInfo = _catalog.GetPart("mk1pod_v2") ?? _catalog.GetPart("mk1pod")
            ?? throw new InvalidOperationException("Command pod not found.");
        var root = CreateInstance(podInfo, new Vector3(0, 25f, 0));
        vessel.RootPart = root;
        vessel.Parts.Add(root);

        // Parachute
        var chuteInfo = _catalog.GetPart("parachuteSingle");
        CraftPartInstance? chute = null;
        if (chuteInfo != null && root.Part.TopNode != null && chuteInfo.BottomNode != null)
        {
            chute = CreateInstance(chuteInfo);
            AttachStack(root, root.Part.TopNode.Id, chute, chuteInfo.BottomNode.Id);
            vessel.Parts.Add(chute);
        }

        // Heat Shield
        var hsInfo = _catalog.GetPart("HeatShield1")
            ?? throw new InvalidOperationException("HeatShield1 not found.");
        var hs = CreateInstance(hsInfo);
        AttachStack(root, root.Part.BottomNode!.Id, hs, hsInfo.TopNode!.Id);
        vessel.Parts.Add(hs);

        // Payload Decoupler
        var decInfo1 = _catalog.GetPart("Decoupler_1")
            ?? throw new InvalidOperationException("Decoupler_1 not found.");
        var decPayload = CreateInstance(decInfo1);
        AttachStack(hs, hs.Part.BottomNode!.Id, decPayload, decInfo1.TopNode!.Id);
        vessel.Parts.Add(decPayload);

        // 2. Moon Lander Stage (Vall / Low-gravity lander)
        var landerTankInfo = _catalog.GetPart("fuelTank_long")
            ?? throw new InvalidOperationException("fuelTank_long not found.");
        var landerTank = CreateInstance(landerTankInfo);
        AttachStack(decPayload, decPayload.Part.BottomNode!.Id, landerTank, landerTankInfo.TopNode!.Id);
        vessel.Parts.Add(landerTank);

        var landerEngineInfo = _catalog.GetPart("liquidEngine3_v2")
            ?? throw new InvalidOperationException("liquidEngine3_v2 not found.");
        var landerEngine = CreateInstance(landerEngineInfo);
        AttachStack(landerTank, landerTank.Part.BottomNode!.Id, landerEngine, landerEngineInfo.TopNode!.Id);
        vessel.Parts.Add(landerEngine);

        // 4x Heavy Landing Legs attached radially to the lander tank
        var legInfo = _catalog.GetPart("landingLeg1-2") ?? _catalog.GetPart("landingLeg1");
        if (legInfo != null)
        {
            var angles = new[] { 0.0, 90.0, 180.0, 270.0 };
            var legRadius = 0.65f;
            var legY = landerTank.Position.Y - 0.5f;

            foreach (var deg in angles)
            {
                var rad = deg * (Math.PI / 180.0);
                var lx = (float)(Math.Cos(rad) * legRadius);
                var lz = (float)(Math.Sin(rad) * legRadius);
                var leg = CreateInstance(legInfo, new Vector3(lx, legY, lz));
                landerTank.Children.Add(leg);
                leg.Parent = landerTank;
                vessel.Parts.Add(leg);
            }
        }

        // Lander Decoupler
        var decLander = CreateInstance(decInfo1);
        AttachStack(landerEngine, landerEngine.Part.BottomNode!.Id, decLander, decInfo1.TopNode!.Id);
        vessel.Parts.Add(decLander);

        // 3. Jool Interplanetary Transfer Stage (3.75m S3-7200 + Rhino 2000kN)
        var joolTankInfo = _catalog.GetPart("Size3MediumTank")
            ?? throw new InvalidOperationException("Size3MediumTank not found.");
        var joolTank = CreateInstance(joolTankInfo);
        AttachStack(decLander, decLander.Part.BottomNode!.Id, joolTank, joolTankInfo.TopNode!.Id);
        vessel.Parts.Add(joolTank);

        var joolEngineInfo = _catalog.GetPart("Size3AdvancedEngine")
            ?? throw new InvalidOperationException("Size3AdvancedEngine not found.");
        var joolEngine = CreateInstance(joolEngineInfo);
        AttachStack(joolTank, joolTank.Part.BottomNode!.Id, joolEngine, joolEngineInfo.TopNode!.Id);
        vessel.Parts.Add(joolEngine);

        // Heavy Booster Decoupler (3.75m TD-37)
        var decInfo3 = _catalog.GetPart("Decoupler_3") ?? _catalog.GetPart("Decoupler_2") ?? decInfo1;
        var decBooster = CreateInstance(decInfo3);
        AttachStack(joolEngine, joolEngine.Part.BottomNode!.Id, decBooster, decInfo3.TopNode!.Id);
        vessel.Parts.Add(decBooster);

        // 4. Kerbin Heavy Booster Stage (3.75m S3-14400 + Mammoth 3200-4000kN)
        var boosterTankInfo = _catalog.GetPart("Size3LargeTank")
            ?? throw new InvalidOperationException("Size3LargeTank not found.");
        var boosterTank = CreateInstance(boosterTankInfo);
        AttachStack(decBooster, decBooster.Part.BottomNode!.Id, boosterTank, boosterTankInfo.TopNode!.Id);
        vessel.Parts.Add(boosterTank);

        var mammothInfo = _catalog.GetPart("Size3EngineCluster")
            ?? throw new InvalidOperationException("Size3EngineCluster not found.");
        var mammoth = CreateInstance(mammothInfo);
        AttachStack(boosterTank, boosterTank.Part.BottomNode!.Id, mammoth, mammothInfo.TopNode!.Id);
        vessel.Parts.Add(mammoth);

        // 4x Aerodynamic Fins around 3.75m base
        var finInfo = _catalog.GetPart("R8winglet");
        if (finInfo != null)
        {
            var angles = new[] { 0.0, 90.0, 180.0, 270.0 };
            var finRadius = 1.95f;
            var finY = boosterTank.Position.Y - 2.5f;

            foreach (var deg in angles)
            {
                var rad = deg * (Math.PI / 180.0);
                var fx = (float)(Math.Cos(rad) * finRadius);
                var fz = (float)(Math.Sin(rad) * finRadius);
                var fin = CreateInstance(finInfo, new Vector3(fx, finY, fz));
                boosterTank.Children.Add(fin);
                fin.Parent = boosterTank;
                vessel.Parts.Add(fin);
            }
        }

        // Staging Setup (Descending index: Highest is ignited first)
        // Stage 6: Mammoth Booster Engine
        // Stage 5: Booster Decoupler (TD-37)
        // Stage 4: Jool Transfer Engine (Rhino)
        // Stage 3: Lander Decoupler (TD-12)
        // Stage 2: Lander Engine (Terrier)
        // Stage 1: Capsule Decoupler (TD-12)
        // Stage 0: Reentry Parachute
        int stage = 0;
        if (chute != null)
        {
            chute.IgnitionStage = stage;
            chute.StageIndex = 0;
        }

        stage++;
        decPayload.IgnitionStage = stage;
        decPayload.DecoupleStage = stage;
        decPayload.StageIndex = 0;

        stage++;
        landerEngine.IgnitionStage = stage;
        landerEngine.StageIndex = 0;

        stage++;
        decLander.IgnitionStage = stage;
        decLander.DecoupleStage = stage;
        decLander.StageIndex = 0;

        stage++;
        joolEngine.IgnitionStage = stage;
        joolEngine.StageIndex = 0;

        stage++;
        decBooster.IgnitionStage = stage;
        decBooster.DecoupleStage = stage;
        decBooster.StageIndex = 0;

        stage++;
        mammoth.IgnitionStage = stage;
        mammoth.StageIndex = 0;

        return vessel;
    }

    public CraftVessel BuildTyloMasterLander(string vesselName = "Tylo_Master_Lander")
    {
        var vessel = new CraftVessel
        {
            Name = vesselName,
            Description = "Extreme-gravity interplanetary heavy lander designed for the hardest destination: Tylo (0.785g, 0 atmosphere, ~5000 m/s landing/ascent) and Kerbin return.",
            Type = "VAB",
            VesselType = "Ship"
        };

        // 1. Reentry Capsule (Stage 0 & 1)
        var podInfo = _catalog.GetPart("mk1pod_v2") ?? _catalog.GetPart("mk1pod")
            ?? throw new InvalidOperationException("mk1pod not found.");
        var root = CreateInstance(podInfo, new Vector3(0, 32f, 0));
        vessel.RootPart = root;
        vessel.Parts.Add(root);

        // Parachute
        var chuteInfo = _catalog.GetPart("parachuteSingle");
        CraftPartInstance? chute = null;
        if (chuteInfo != null && root.Part.TopNode != null && chuteInfo.BottomNode != null)
        {
            chute = CreateInstance(chuteInfo);
            AttachStack(root, root.Part.TopNode.Id, chute, chuteInfo.BottomNode.Id);
            vessel.Parts.Add(chute);
        }

        // Heat Shield (Kerbin atmospheric reentry from interplanetary velocity)
        var hsInfo = _catalog.GetPart("HeatShield1")
            ?? throw new InvalidOperationException("HeatShield1 not found.");
        var hs = CreateInstance(hsInfo);
        AttachStack(root, root.Part.BottomNode!.Id, hs, hsInfo.TopNode!.Id);
        vessel.Parts.Add(hs);

        // Payload Decoupler
        var decInfo1 = _catalog.GetPart("Decoupler_1")
            ?? throw new InvalidOperationException("Decoupler_1 not found.");
        var decPayload = CreateInstance(decInfo1);
        AttachStack(hs, hs.Part.BottomNode!.Id, decPayload, decInfo1.TopNode!.Id);
        vessel.Parts.Add(decPayload);

        // 2. Tylo Ascent & Kerbin Return Stage (Stage 2)
        var ascentTankInfo = _catalog.GetPart("fuelTank_long")
            ?? throw new InvalidOperationException("fuelTank_long not found.");
        var ascentTank = CreateInstance(ascentTankInfo);
        AttachStack(decPayload, decPayload.Part.BottomNode!.Id, ascentTank, ascentTankInfo.TopNode!.Id);
        vessel.Parts.Add(ascentTank);

        var ascentEngineInfo = _catalog.GetPart("liquidEngine3_v2")
            ?? throw new InvalidOperationException("liquidEngine3_v2 not found.");
        var ascentEngine = CreateInstance(ascentEngineInfo);
        AttachStack(ascentTank, ascentTank.Part.BottomNode!.Id, ascentEngine, ascentEngineInfo.TopNode!.Id);
        vessel.Parts.Add(ascentEngine);

        // Decoupler between Tylo Ascent and Descent Stages
        var decAscent = CreateInstance(decInfo1);
        AttachStack(ascentEngine, ascentEngine.Part.BottomNode!.Id, decAscent, decInfo1.TopNode!.Id);
        vessel.Parts.Add(decAscent);

        // 3. Tylo Heavy Descent / Braking Stage (Stage 4)
        var descentTankInfo = _catalog.GetPart("Rockomax32_BW")
            ?? throw new InvalidOperationException("Rockomax32_BW not found.");
        var descentTank = CreateInstance(descentTankInfo);
        AttachStack(decAscent, decAscent.Part.BottomNode!.Id, descentTank, descentTankInfo.TopNode!.Id);
        vessel.Parts.Add(descentTank);

        var descentEngineInfo = _catalog.GetPart("liquidEngine2-2_v2")
            ?? throw new InvalidOperationException("liquidEngine2-2_v2 not found.");
        var descentEngine = CreateInstance(descentEngineInfo);
        AttachStack(descentTank, descentTank.Part.BottomNode!.Id, descentEngine, descentEngineInfo.TopNode!.Id);
        vessel.Parts.Add(descentEngine);

        // 4x Heavy Landing Legs mounted radially on the 2.5m descent tank
        var legInfo = _catalog.GetPart("landingLeg1-2") ?? _catalog.GetPart("landingLeg1");
        if (legInfo != null)
        {
            var angles = new[] { 0.0, 90.0, 180.0, 270.0 };
            var legRadius = 1.35f;
            var legY = descentTank.Position.Y - 1.0f;

            foreach (var deg in angles)
            {
                var rad = deg * (Math.PI / 180.0);
                var lx = (float)(Math.Cos(rad) * legRadius);
                var lz = (float)(Math.Sin(rad) * legRadius);
                var leg = CreateInstance(legInfo, new Vector3(lx, legY, lz));
                descentTank.Children.Add(leg);
                leg.Parent = descentTank;
                vessel.Parts.Add(leg);
            }
        }

        // Decoupler between Tylo Lander and Interplanetary Transfer Stage (2.5m TD-25)
        var decInfo2 = _catalog.GetPart("Decoupler_2") ?? decInfo1;
        var decTransfer = CreateInstance(decInfo2);
        AttachStack(descentEngine, descentEngine.Part.BottomNode!.Id, decTransfer, decInfo2.TopNode!.Id);
        vessel.Parts.Add(decTransfer);

        // 4. Jool & Tylo Interplanetary Cruiser Stage (3.75m S3-14400 + Rhino 2000kN) (Stage 6)
        var cruiserTankInfo = _catalog.GetPart("Size3LargeTank")
            ?? throw new InvalidOperationException("Size3LargeTank not found.");
        var cruiserTank = CreateInstance(cruiserTankInfo);
        AttachStack(decTransfer, decTransfer.Part.BottomNode!.Id, cruiserTank, cruiserTankInfo.TopNode!.Id);
        vessel.Parts.Add(cruiserTank);

        var cruiserEngineInfo = _catalog.GetPart("Size3AdvancedEngine")
            ?? throw new InvalidOperationException("Size3AdvancedEngine not found.");
        var cruiserEngine = CreateInstance(cruiserEngineInfo);
        AttachStack(cruiserTank, cruiserTank.Part.BottomNode!.Id, cruiserEngine, cruiserEngineInfo.TopNode!.Id);
        vessel.Parts.Add(cruiserEngine);

        // Decoupler between Interplanetary Stage and Liftoff Booster (3.75m TD-37)
        var decInfo3 = _catalog.GetPart("Decoupler_3") ?? decInfo2;
        var decBooster = CreateInstance(decInfo3);
        AttachStack(cruiserEngine, cruiserEngine.Part.BottomNode!.Id, decBooster, decInfo3.TopNode!.Id);
        vessel.Parts.Add(decBooster);

        // 5. Kerbin Super-Heavy Booster Stage (3.75m S3-14400 + S3-7200 + Mammoth 3200-4000kN) (Stage 8)
        var boosterTank1 = CreateInstance(cruiserTankInfo);
        AttachStack(decBooster, decBooster.Part.BottomNode!.Id, boosterTank1, cruiserTankInfo.TopNode!.Id);
        vessel.Parts.Add(boosterTank1);

        var boosterTank2Info = _catalog.GetPart("Size3MediumTank") ?? cruiserTankInfo;
        var boosterTank2 = CreateInstance(boosterTank2Info);
        AttachStack(boosterTank1, boosterTank1.Part.BottomNode!.Id, boosterTank2, boosterTank2Info.TopNode!.Id);
        vessel.Parts.Add(boosterTank2);

        var mammothInfo = _catalog.GetPart("Size3EngineCluster")
            ?? throw new InvalidOperationException("Size3EngineCluster not found.");
        var mammoth = CreateInstance(mammothInfo);
        AttachStack(boosterTank2, boosterTank2.Part.BottomNode!.Id, mammoth, mammothInfo.TopNode!.Id);
        vessel.Parts.Add(mammoth);

        // 4x Aerodynamic Fins around 3.75m booster base
        var finInfo = _catalog.GetPart("R8winglet");
        if (finInfo != null)
        {
            var angles = new[] { 0.0, 90.0, 180.0, 270.0 };
            var finRadius = 1.95f;
            var finY = boosterTank2.Position.Y - 1.5f;

            foreach (var deg in angles)
            {
                var rad = deg * (Math.PI / 180.0);
                var fx = (float)(Math.Cos(rad) * finRadius);
                var fz = (float)(Math.Sin(rad) * finRadius);
                var fin = CreateInstance(finInfo, new Vector3(fx, finY, fz));
                boosterTank2.Children.Add(fin);
                fin.Parent = boosterTank2;
                vessel.Parts.Add(fin);
            }
        }

        // 6-Stage Clean KSP Staging Hierarchy:
        // Stage 5: Mammoth Liftoff Booster Engine
        // Stage 4: TD-37 Booster Decoupler + Rhino Interplanetary Cruiser Engine
        // Stage 3: TD-25 Transfer Decoupler + Poodle Tylo Descent Engine
        // Stage 2: TD-12 Tylo Ascent Decoupler + Terrier Tylo Ascent & Return Engine
        // Stage 1: TD-12 Reentry Capsule Decoupler
        // Stage 0: Reentry Parachute

        // Stage 0: Parachute
        if (chute != null)
        {
            chute.IgnitionStage = 0;
            chute.DecoupleStage = 0;
            chute.StageIndex = 0;
        }
        root.DecoupleStage = 0;
        hs.DecoupleStage = 0;

        // Stage 1: Reentry Capsule Decoupler
        decPayload.IgnitionStage = 1;
        decPayload.DecoupleStage = 1;
        decPayload.StageIndex = 0;

        // Stage 2: Tylo Ascent Stage (Decouple descent stage + ignite Terrier)
        decAscent.IgnitionStage = 2;
        decAscent.DecoupleStage = 2;
        decAscent.StageIndex = 1;

        ascentEngine.IgnitionStage = 2;
        ascentEngine.DecoupleStage = 1;
        ascentEngine.StageIndex = 0;

        ascentTank.DecoupleStage = 1;

        // Stage 3: Tylo Descent Stage (Decouple Rhino cruiser + ignite Poodle)
        decTransfer.IgnitionStage = 3;
        decTransfer.DecoupleStage = 3;
        decTransfer.StageIndex = 1;

        descentEngine.IgnitionStage = 3;
        descentEngine.DecoupleStage = 2;
        descentEngine.StageIndex = 0;

        descentTank.DecoupleStage = 2;
        foreach (var leg in descentTank.Children)
        {
            leg.DecoupleStage = 2;
        }

        // Stage 4: Interplanetary Cruiser Stage (Decouple Mammoth booster + ignite Rhino)
        decBooster.IgnitionStage = 4;
        decBooster.DecoupleStage = 4;
        decBooster.StageIndex = 1;

        cruiserEngine.IgnitionStage = 4;
        cruiserEngine.DecoupleStage = 3;
        cruiserEngine.StageIndex = 0;

        cruiserTank.DecoupleStage = 3;

        // Stage 5: Mammoth Liftoff Booster Engine
        mammoth.IgnitionStage = 5;
        mammoth.DecoupleStage = 4;
        mammoth.StageIndex = 0;

        boosterTank1.DecoupleStage = 4;
        boosterTank2.DecoupleStage = 4;
        foreach (var fin in boosterTank2.Children)
        {
            fin.DecoupleStage = 4;
        }

        return vessel;
    }

    private void AttachStack(CraftPartInstance parent, string parentNodeId, CraftPartInstance child, string childNodeId)
    {
        var pNode = parent.Part.AttachNodes.Find(n => n.Id.Equals(parentNodeId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Node {parentNodeId} not found on {parent.Part.Name}");
        var cNode = child.Part.AttachNodes.Find(n => n.Id.Equals(childNodeId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Node {childNodeId} not found on {child.Part.Name}");

        // Child position = parent position + (parent node offset - child node offset)
        var offset = pNode.Position - cNode.Position;
        child.Position = parent.Position + offset;

        parent.Children.Add(child);
        child.Parent = parent;

        parent.NodeConnections[parentNodeId] = (child, childNodeId);
        child.NodeConnections[childNodeId] = (parent, parentNodeId);
    }

    private CraftPartInstance CreateInstance(PartInfo part, Vector3? initialPos = null)
    {
        var inst = new CraftPartInstance(part, (_nextUid++).ToString(), _nextPersistentId++)
        {
            Position = initialPos ?? Vector3.Zero
        };
        return inst;
    }
}
