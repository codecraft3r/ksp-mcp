using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KspMcp.Parts;

public class ConfigNode
{
    public string Name { get; set; } = string.Empty;
    public List<KeyValuePair<string, string>> Values { get; } = new();
    public List<ConfigNode> Nodes { get; } = new();

    public ConfigNode() { }

    public ConfigNode(string name)
    {
        Name = name;
    }

    public void AddValue(string key, string value)
    {
        Values.Add(new KeyValuePair<string, string>(key, value));
    }

    public void SetValue(string key, string value)
    {
        for (int i = 0; i < Values.Count; i++)
        {
            if (string.Equals(Values[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                Values[i] = new KeyValuePair<string, string>(key, value);
                return;
            }
        }
        AddValue(key, value);
    }

    public string? GetValue(string key)
    {
        foreach (var kvp in Values)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        return null;
    }

    public IEnumerable<string> GetValues(string key)
    {
        return Values
            .Where(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value);
    }

    public ConfigNode? GetNode(string name)
    {
        return Nodes.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<ConfigNode> GetNodes(string name)
    {
        return Nodes.Where(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static List<ConfigNode> ParseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return ParseString(content);
    }

    public static List<ConfigNode> ParseString(string text)
    {
        var rootNodes = new List<ConfigNode>();
        var stack = new Stack<ConfigNode>();

        using var reader = new StringReader(text);
        string? rawLine;
        string? pendingNodeName = null;

        while ((rawLine = reader.ReadLine()) != null)
        {
            var line = StripComments(rawLine).Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line == "{")
            {
                var newNode = new ConfigNode(pendingNodeName ?? "NODE");
                pendingNodeName = null;

                if (stack.Count > 0)
                {
                    stack.Peek().Nodes.Add(newNode);
                }
                else
                {
                    rootNodes.Add(newNode);
                }
                stack.Push(newNode);
            }
            else if (line == "}")
            {
                if (stack.Count > 0)
                {
                    stack.Pop();
                }
            }
            else if (line.Contains('='))
            {
                var eqIdx = line.IndexOf('=');
                var key = line.Substring(0, eqIdx).Trim();
                var val = line.Substring(eqIdx + 1).Trim();

                if (stack.Count > 0)
                {
                    stack.Peek().AddValue(key, val);
                }
                else
                {
                    // Root-level key-value
                    var rootWrapper = new ConfigNode("ROOT");
                    rootWrapper.AddValue(key, val);
                    rootNodes.Add(rootWrapper);
                }
            }
            else
            {
                // Likely a node header name, e.g. "PART" or "MODULE"
                pendingNodeName = line;
            }
        }

        return rootNodes;
    }

    private static string StripComments(string line)
    {
        var commentIdx = line.IndexOf("//", StringComparison.Ordinal);
        if (commentIdx >= 0)
        {
            return line.Substring(0, commentIdx);
        }
        return line;
    }

    public string Format(int indentLevel = 0)
    {
        var sb = new StringBuilder();
        FormatInternal(sb, indentLevel);
        return sb.ToString();
    }

    private void FormatInternal(StringBuilder sb, int indentLevel)
    {
        var indent = new string('\t', indentLevel);
        sb.AppendLine($"{indent}{Name}");
        sb.AppendLine($"{indent}{{");

        foreach (var kvp in Values)
        {
            sb.AppendLine($"{indent}\t{kvp.Key} = {kvp.Value}");
        }

        foreach (var child in Nodes)
        {
            child.FormatInternal(sb, indentLevel + 1);
        }

        sb.AppendLine($"{indent}}}");
    }
}
