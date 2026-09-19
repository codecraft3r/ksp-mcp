using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace KspMcp.Craft;

public static class CraftWriter
{
    public static string GenerateCraftString(CraftVessel vessel)
    {
        var sb = new StringBuilder();
        var size = vessel.Size;

        sb.AppendLine($"ship = {vessel.Name}");
        sb.AppendLine($"version = {vessel.Version}");
        sb.AppendLine($"description = {vessel.Description}");
        sb.AppendLine($"type = {vessel.Type}");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "size = {0:F3},{1:F3},{2:F3}", size.X, size.Y, size.Z));
        sb.AppendLine("steamPublishedFileId = 0");
        sb.AppendLine($"persistentId = {vessel.PersistentId}");
        sb.AppendLine("rot = 0,0,0,0");
        sb.AppendLine("missionFlag = Squad/Flags/default");
        sb.AppendLine($"vesselType = {vessel.VesselType}");

        foreach (var p in vessel.Parts)
        {
            sb.AppendLine("PART");
            sb.AppendLine("{");
            sb.AppendLine($"\tpart = {p.FullId}");
            sb.AppendLine("\tpartName = Part");
            sb.AppendLine($"\tpersistentId = {p.PersistentId}");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "\tpos = {0:F5},{1:F5},{2:F5}", p.Position.X, p.Position.Y, p.Position.Z));
            sb.AppendLine("\tattPos = 0,0,0");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "\tattPos0 = {0:F5},{1:F5},{2:F5}", p.Position.X, p.Position.Y, p.Position.Z));
            sb.AppendLine("\trot = 0,0,0,1");
            sb.AppendLine("\tattRot = 0,0,0,1");
            sb.AppendLine("\tattRot0 = 0,0,0,1");
            sb.AppendLine("\tmir = 1,1,1");
            sb.AppendLine("\tsymMethod = Radial");
            sb.AppendLine("\tautostrutMode = Off");
            sb.AppendLine("\trigidAttachment = False");
            sb.AppendLine($"\tistg = {p.IgnitionStage}");
            sb.AppendLine("\tresPri = 0");
            sb.AppendLine($"\tdstg = {p.DecoupleStage}");
            sb.AppendLine($"\tsidx = {p.StageIndex}");
            sb.AppendLine($"\tsqor = {p.StageSequenceOrder}");
            sb.AppendLine($"\tsepI = {p.SeparationIndex}");
            sb.AppendLine("\tattm = 0");
            sb.AppendLine("\tmodCost = 0");
            sb.AppendLine("\tmodMass = 0");
            sb.AppendLine("\tmodSize = 0,0,0");

            // Links to children
            foreach (var child in p.Children)
            {
                sb.AppendLine($"\tlink = {child.FullId}");
            }

            // Node connections
            foreach (var kvp in p.NodeConnections)
            {
                var nodeId = kvp.Key;
                var otherPart = kvp.Value.OtherPart;
                var node = p.Part.AttachNodes.Find(n => n.Id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));
                var nx = node?.Position.X ?? 0f;
                var ny = node?.Position.Y ?? 0f;
                var nz = node?.Position.Z ?? 0f;
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "\tattN = {0},{1}_{2:G}|{3:G}|{4:G}", nodeId, otherPart.FullId, nx, ny, nz));
            }

            sb.AppendLine("\tEVENTS");
            sb.AppendLine("\t{");
            sb.AppendLine("\t}");
            sb.AppendLine("\tACTIONS");
            sb.AppendLine("\t{");
            sb.AppendLine("\t}");
            sb.AppendLine("\tPARTDATA");
            sb.AppendLine("\t{");
            sb.AppendLine("\t}");

            // Export resources
            foreach (var res in p.Part.Resources)
            {
                sb.AppendLine("\tRESOURCE");
                sb.AppendLine("\t{");
                sb.AppendLine($"\t\tname = {res.Name}");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "\t\tamount = {0:G}", res.Amount));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "\t\tmaxAmount = {0:G}", res.MaxAmount));
                sb.AppendLine("\t\tflowState = True");
                sb.AppendLine("\t\tisTweakable = True");
                sb.AppendLine("\t}");
            }

            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public static void SaveCraft(CraftVessel vessel, string filePath)
    {
        var content = GenerateCraftString(vessel);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, content);
    }
}
