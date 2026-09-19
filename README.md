# KSP Autonomous AI Driver & MCP Server (C# / .NET 10)

An autonomous aerospace engineering and flight operations server for **Kerbal Space Program (KSP)** implementing the **Model Context Protocol (MCP)** using the official [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK and [`kOS`](https://github.com/KSP-KOS/KOS).

It gives AI assistants (Claude, Antigravity, Cursor, etc.) the capability to:
1. **Search and inspect parts** across stock KSP and installed mods (`GameData/`).
2. **Size rocket stages** using accurate Tsiolkovsky delta-v ($\Delta v$) and Thrust-to-Weight Ratio ($TWR$) equations.
3. **Assemble flyable multi-stage rockets** with automatic node snapping, parent-child links, and sequential staging directly into `Ships/VAB/`.
4. **Deploy reusable flight libraries** (`lib_ascent.ks`, `lib_node.ks`, `lib_orbit.ks`, `lib_telemetry.ks`) to the kOS Archive (`Ships/Script/`).
5. **Author and validate custom KerboScript flight plans**.
6. **Command and monitor live missions** via an asynchronous RFC 854 kOS Telnet client (default port `5410`).

---

## Quick Start

### 1. Requirements
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
* Kerbal Space Program (automatically detected from Steam standard paths or configurable via `KSP_ROOT` environment variable)
* [kOS](https://github.com/KSP-KOS/KOS) installed in KSP (Installed via CKAN)

### 2. Build & Verify
```bash
# Run automated xUnit tests
dotnet test

# Test scan KSP GameData parts
dotnet run --project src/KspMcp -- --test-scan

# Assemble and validate a test rocket in Ships/VAB
dotnet run --project src/KspMcp -- --test-build

# Deploy flight helper libraries to Ships/Script
dotnet run --project src/KspMcp -- --deploy-libs
```

---

## MCP Server Configuration

To connect the server to Claude Desktop, Antigravity, or other MCP clients, add the following to your MCP settings file (e.g. `claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "ksp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\Users\\nmitc\\Documents\\ksp-mcp\\src\\KspMcp\\KspMcp.csproj",
        "-c",
        "Release"
      ],
      "env": {
        "KSP_ROOT": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Kerbal Space Program"
      }
    }
  }
}
```

---

## Exposed MCP Tools

| Tool Name | Parameters | Description |
|-----------|------------|-------------|
| `ksp_search_parts` | `category`, `searchTerm`, `size` | Search the parts catalog by category (`Pods`, `Engines`, `FuelTank`, `Coupling`, `Aero`, `Utility`), bulkhead profile (`size0`, `size1`, `size2`), or keyword. |
| `ksp_get_part_details` | `partName` | Retrieve dry mass, wet mass, resources, attachment nodes, engine thrust, and vacuum/ASL Isp. |
| `ksp_calculate_stage_deltav` | `engineName`, `tankName`, `tankCount`, `payloadMass` | Calculate stage dry/wet mass, propellant mass, vacuum and ASL $\Delta v$, Kerbin TWR, and burn time. |
| `ksp_build_launch_vehicle` | `vesselName`, `payloadType`, `includeParachute`, `includeHeatShield`, `upperEngineName`, `upperTankName`, `upperTankCount`, `boosterEngineName`, `boosterTankName`, `boosterTankCount`, `includeFins` | Synthesizes a multi-stage rocket `.craft` directly to `Ships/VAB` with node snapping and descending staging sequence. |
| `ksp_validate_craft` | `craftName` | Inspects a `.craft` file for part graph validity, command module, engine presence, and total mass. |
| `kos_deploy_helper_libraries` | *none* | Deploys reusable navigation and flight helper libraries to `Ships/Script/`. |
| `kos_list_scripts` | *none* | Lists all flight scripts in the kOS Archive folder (`Ships/Script/`). |
| `kos_read_script` | `scriptName` | Reads the contents of a KerboScript (`.ks`) file from the Archive. |
| `kos_write_script` | `scriptName`, `content` | Validates syntax (braces, parens, statements) and saves a `.ks` file into the kOS Archive. |
| `kos_execute_command` | `command`, `timeoutSeconds` | Sends a command or initiates a script over kOS Telnet (port `5410`) to the active vessel. |
| `kos_get_terminal_output` | `waitMilliseconds` | Reads the terminal output buffer from the active kOS Telnet session. |
| `kos_get_telemetry` | *none* | Queries real-time vessel telemetry (altitude, apoapsis, periapsis, orbital speed, mass). |

---

## Reusable kOS Flight Libraries (`Ships/Script/`)

The server automatically installs the following libraries into the kOS Archive:

* **`lib_math.ks`**: Math utilities (circular orbital speed calculations, clamping functions).
* **`lib_ascent.ks`**: `LaunchToOrbit(targetApoapsis, targetHeading)`:
  * Full launch countdown & staging.
  * Smooth gravity turn pitch schedule ($90^\circ \to 5^\circ$) based on altitude.
  * Automated staging on fuel flameout (`MAXTHRUST = 0`).
  * Engine cutoff (MECO) at target apoapsis and coasting outside atmosphere ($70\text{km}$).
* **`lib_orbit.ks`**: `PlanCircularizationAtApoapsis()`:
  * Calculates vis-viva velocity shortfall at apoapsis.
  * Creates an exact prograde maneuver node at apoapsis.
* **`lib_node.ks`**: `ExecuteNextNode()`:
  * Calculates required burn duration using vessel mass and engine thrust.
  * Aligns vessel with node burn vector.
  * Autowarps to burn ignition window ($T - t_{\text{burn}}/2$).
  * Executes burn with dynamic throttle ramp-down and removes node when complete.
* **`lib_telemetry.ks`**: Real-time structured telemetry stream.

---

## Mission Workflow Example

1. **AI sizes the vehicle**:
   * AI calls `ksp_calculate_stage_deltav` to size upper stage ($\approx 1500\text{ m/s}$) and booster stage ($\approx 2000\text{ m/s}$).
2. **AI generates the rocket**:
   * AI calls `ksp_build_launch_vehicle("Kerbal_Orbiter_I", "Crewed", true, true, ...)` $\to$ `.craft` is saved to `Ships/VAB/Kerbal_Orbiter_I.craft`.
3. **Player launches craft to pad**:
   * Player opens VAB, loads `Kerbal_Orbiter_I`, and clicks **Launch**.
4. **AI authors mission script**:
   * AI calls `kos_write_script("mission_orbit.ks", ...)`:
     ```kerboscript
     RUNONCEPATH("0:/lib_ascent.ks").
     RUNONCEPATH("0:/lib_orbit.ks").
     RUNONCEPATH("0:/lib_node.ks").

     PRINT "Initiating autonomous orbital insertion...".
     LaunchToOrbit(80000, 90).
     PlanCircularizationAtApoapsis().
     ExecuteNextNode().
     PRINT "Orbit established!".
     ```
5. **AI commands execution & monitors**:
   * AI calls `kos_execute_command("RUNPATH(\"0:/mission_orbit.ks\").")`.
   * AI periodically calls `kos_get_telemetry` to monitor altitude, speed, and orbit status.
