# KSP Autonomous AI Driver & MCP Server (C# / .NET 10)

An autonomous aerospace engineering and flight operations server for **Kerbal Space Program (KSP)** implementing the **Model Context Protocol (MCP)** using the official [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK and [`kOS`](https://github.com/KSP-KOS/KOS).

It provides AI assistants (Claude, Antigravity, Cursor, etc.) the end-to-end capability to:
1. **Search and inspect parts** across stock KSP and installed mods (`GameData/`).
2. **Size rocket stages** using accurate Tsiolkovsky delta-v ($\Delta v$) and Thrust-to-Weight Ratio ($TWR$) rocket equations.
3. **Assemble flyable multi-stage rockets** with automatic node snapping, parent-child links, and sequential staging directly into `Ships/VAB/`.
4. **Deploy reusable flight libraries and mission plans** (`lib_ascent.ks`, `lib_node.ks`, `lib_orbit.ks`, `mun_mission.ks`, `mun_crash.ks`) to the kOS Archive (`Ships/Script/`).
5. **Command and monitor live missions** via an asynchronous RFC 854 kOS Telnet client (default port `5410`).
6. **Automate flight operations with on-rails time-warp** through launch, circularization, Trans-Munar Injection (TMI), interplanetary coast, orbital capture, and surface impact.

---

## Proven Flight Verification: Autonomous Mission to the Mun

This MCP system was verified in a real, live autonomous flight from Kerbin launchpad to the Mun:
* **Vessel**: `Mun_Explorer_I` (2-stage rocket, ~5,400 m/s $\Delta v$, Mk1 Command Pod payload).
* **Launch & Ascent**: 100% autonomous countdown, lift-off, and gravity turn to 80 km parking orbit.
* **Trans-Munar Injection (TMI)**: Patched-conics window search, generating an 855 m/s burn node and automated execution.
* **Interplanetary Fast-Forward**: Automated `WARPTO` traversing 61,640 seconds of deep-space transit to Mun SOI.
* **Mun Orbit Insertion**: Hyperbolic trajectory capture burn at periapsis (-227.4 m/s), establishing a stable circular Mun orbit (Apoapsis: 750,437 m, Periapsis: 748,463 m).
* **Kinetic Impactor Deorbit**: Full-throttle retrograde deorbit burn plunging the vessel into a sub-surface collision trajectory with the Mun.

---

## Quick Start

### 1. Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
* Kerbal Space Program 1.12.5 (auto-detected from Steam or via `KSP_ROOT` environment variable)
* [kOS](https://github.com/KSP-KOS/KOS) installed in KSP (e.g. via CKAN)
* *Optional*: `kOSforAll` to ensure all command pods include a flight computer

### 2. Build & Test
```bash
# Run automated xUnit tests (12 tests)
dotnet test

# Test scan KSP GameData parts
dotnet run --project src/KspMcp -- --test-scan

# Assemble and validate a test rocket in Ships/VAB
dotnet run --project src/KspMcp -- --test-build

# Deploy flight helper libraries and mission scripts to Ships/Script
dotnet run --project src/KspMcp -- --deploy-libs
```

### 3. Running an Autonomous Mission
1. Launch KSP and load/create a save game (e.g. Sandbox mode).
2. Ensure kOS Telnet is enabled on port 5410 (`GameData/kOS/Plugins/PluginData/kOS/config.xml` with `telnet_enabled = True`).
3. In the VAB, load the AI-generated craft (e.g. `Mun_Explorer_I`) and click **Launch**.
4. Run the launch automation script:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/wait_and_launch.ps1
```
5. To deorbit or crash the vessel into the celestial body:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/execute_crash.ps1
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
| `kos_deploy_helper_libraries` | *none* | Deploys reusable navigation, flight helper libraries, and mission plans to `Ships/Script/`. |
| `kos_list_scripts` | *none* | Lists all flight scripts in the kOS Archive folder (`Ships/Script/`). |
| `kos_read_script` | `scriptName` | Reads the contents of a KerboScript (`.ks`) file from the Archive. |
| `kos_write_script` | `scriptName`, `content` | Validates syntax (braces, parens, statements) and saves a `.ks` file into the kOS Archive. |
| `kos_execute_command` | `command`, `timeoutSeconds` | Sends a command or initiates a script over kOS Telnet (port `5410`) to the active vessel. |
| `kos_get_terminal_output` | `waitMilliseconds` | Reads the terminal output buffer from the active kOS Telnet session. |
| `kos_get_telemetry` | *none* | Queries real-time vessel telemetry (altitude, apoapsis, periapsis, orbital speed, mass). |

---

## Autonomous Flight Scripts (`scripts/kos/` & `Ships/Script/`)

The following flight programs are automatically installed into the kOS Archive (`Ships/Script/`):

* **`lib_math.ks`**: Celestial math functions (orbital velocity calculations, clamping utilities).
* **`lib_ascent.ks`**: `LaunchToOrbit(targetApoapsis, targetHeading)`:
  * Full launch countdown and staging.
  * Smooth gravity turn pitch schedule ($90^\circ \to 5^\circ$) dynamically calculated from altitude.
  * Staging guard (`STAGE:NUMBER > 2`) to prevent accidental capsule decoupling upon flameout.
  * Automatic MECO at target apoapsis and coasting outside atmosphere ($70\text{km}$).
* **`lib_orbit.ks`**: `PlanCircularizationAtApoapsis()`:
  * Calculates vis-viva velocity shortfall at apoapsis.
  * Creates an exact prograde maneuver node at apoapsis.
* **`lib_node.ks`**: `ExecuteNextNode()`:
  * Calculates required burn duration using vessel mass and engine thrust ($t = \frac{\Delta v \cdot m}{F}$).
  * Aligns vessel to the maneuver vector.
  * Fast-forwards on-rails time-warp to the ignition window ($T - t_{\text{burn}}/2$).
  * Burns with dynamic throttle ramp-down and detects burn completion via vector dot product (`VDOT(v0, nd:DELTAV) <= 0.5`).
* **`lib_telemetry.ks`**: Real-time structured JSON telemetry broadcaster.
* **`mun_mission.ks`**: 5-phase autonomous mission to the Mun:
  1. Ascent to 80 km Kerbin orbit.
  2. Circularization burn.
  3. Patched-conics injection window search and Trans-Munar Injection (TMI) burn.
  4. On-rails time-warp across 60,000+ seconds of deep-space transit to Mun SOI.
  5. Retrograde Mun Orbit Insertion (MOI) capture burn at periapsis.
* **`mun_crash.ks`**: Kinetic impactor deorbit script that aligns retrograde, burns remaining propellant to drop periapsis below ground, and locks to surface velocity vector into impact.

---

## Key Technical Learnings & Bug Fixes

During the end-to-end integration and flight testing, several critical KSP and kOS quirks were resolved:

1. **Part Name Formatting in `.craft`**:
   * KSP splits part lines on the *first* underscore (`_`). Naming a part `Decoupler_1_4294000004` caused KSP to search for a non-existent part named `Decoupler`.
   * **Fix**: Use dot-separated part names (`Decoupler.1_4294000004`, `mk1pod.v2_4294000001`, `liquidEngine.v2_4294000010`).
2. **Explicit `MODULE` Blocks in `.craft`**:
   * Even when part `.cfg` files define modules, KSP's staging sequencer requires explicit `MODULE { name = ModuleDecouple ... }` and `MODULE { name = ModuleEngines ... }` blocks with `stagingEnabled = True` in the `.craft` file to populate the VAB staging stack.
3. **kOS Variable Clobbering**:
   * `r` is a reserved built-in function in kOS (`r(pitch, yaw, roll)`). Declaring `LOCAL r IS ...` throws a fatal syntax error (`Not allowed to SET a name that will clobber or hide the BUILTIN_FUNCTION called 'r'`).
   * **Fix**: Use descriptive variable names (`radiusVal`, `thrustVal`).
4. **Staging Numbering & Safeguards**:
   * In KSP, Stage 0 is the final stage (capsule parachute), Stage 1 is the capsule decoupler, and higher numbers are lower rocket stages. Unrestricted `WHEN MAXTHRUST = 0 THEN { STAGE. }` triggers can decouple the command pod and deploy parachutes in space when the upper stage burns out.
   * **Fix**: Guard triggers with `STAGE:NUMBER > 2`.
5. **Maneuver Burn Completion**:
   * Comparing `VDOT(nd:DELTAV, nd:BURNVECTOR)` compares `DELTAV` with itself (always positive).
   * **Fix**: Cache the initial burn vector `LOCAL v0 IS nd:DELTAV.` and test `VDOT(v0, nd:DELTAV) <= 0.5`.
6. **Time-Warp & Pause Behavior**:
   * kOS scripts execute inside the KSP in-game simulation clock. Bringing up the Escape/Pause menu suspends all kOS script execution. The game must be unpaused for autopilot scripts to run.

---

## License

MIT License. Kerbal Space Program is a trademark of Take-Two Interactive Software.
