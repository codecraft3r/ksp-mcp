using KspMcp.Configuration;
using KspMcp.Kos;
using Xunit;

namespace KspMcp.Tests;

public class KosScriptTests
{
    [Fact]
    public void ValidateKerboScript_PassesValidSyntax()
    {
        var manager = new KosScriptManager(new KspConfig());
        var code = @"
GLOBAL FUNCTION MyAscent {
    PRINT ""Starting ascent..."".
    LOCK THROTTLE TO 1.0.
    LOCK STEERING TO HEADING(90, 90).
    STAGE.
    WAIT UNTIL SHIP:ALTITUDE > 10000.
    PRINT ""Done."".
}
";
        var result = manager.ValidateKerboScript(code);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateKerboScript_DetectsUnclosedBrace()
    {
        var manager = new KosScriptManager(new KspConfig());
        var code = @"
GLOBAL FUNCTION Broken {
    PRINT ""Missing closing brace"".
";
        var result = manager.ValidateKerboScript(code);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unclosed brace"));
    }

    [Fact]
    public void EmbeddedLibraries_HaveValidSyntax()
    {
        var manager = new KosScriptManager(new KspConfig());

        var ascentResult = manager.ValidateKerboScript(KosScriptManager.LibAscentContent);
        Assert.True(ascentResult.IsValid, string.Join("; ", ascentResult.Errors));

        var nodeResult = manager.ValidateKerboScript(KosScriptManager.LibNodeContent);
        Assert.True(nodeResult.IsValid, string.Join("; ", nodeResult.Errors));

        var orbitResult = manager.ValidateKerboScript(KosScriptManager.LibOrbitContent);
        Assert.True(orbitResult.IsValid, string.Join("; ", orbitResult.Errors));

        var mathResult = manager.ValidateKerboScript(KosScriptManager.LibMathContent);
        Assert.True(mathResult.IsValid, string.Join("; ", mathResult.Errors));
    }

    [Fact]
    public void MunMissionScript_HasValidSyntax()
    {
        var manager = new KosScriptManager(new KspConfig());
        var path = @"C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\Ships\Script\mun_mission.ks";
        if (System.IO.File.Exists(path))
        {
            var content = System.IO.File.ReadAllText(path);
            var result = manager.ValidateKerboScript(content);
            Assert.True(result.IsValid, string.Join("; ", result.Errors));
        }
    }
}
