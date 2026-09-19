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
