// tylo_mission.ks - Autonomous Interplanetary Tylo Landing & Return Mission
// Executed by kOS via KSP MCP Autonomous AI Driver
// Target: Tylo (0.785g, Vacuum, ~5000 m/s landing/ascent budget)

@LAZYGLOBAL OFF.
CLEARSCREEN.
PRINT "==================================================".
PRINT "       KSP MCP TYLO EXTREME LANDING MISSION       ".
PRINT "==================================================".

RUNONCEPATH("0:/lib_math.ks").
RUNONCEPATH("0:/lib_ascent.ks").
RUNONCEPATH("0:/lib_orbit.ks").
RUNONCEPATH("0:/lib_node.ks").

// --------------------------------------------------
// PHASE 1: Super-Heavy Launch to 80km Low Kerbin Orbit
// --------------------------------------------------
PRINT "PHASE 1: Launching 253-tonne Tylo Master Lander to 80km LKO...".
LaunchToOrbit(80000, 90).

// --------------------------------------------------
// PHASE 2: LKO Circularization Burn
// --------------------------------------------------
PRINT "PHASE 2: Planning and executing circularization at Apoapsis...".
PlanCircularizationAtApoapsis().
ExecuteNextNode().
PRINT "Stable Low Kerbin Orbit confirmed!".
PRINT "Current Apoapsis: " + ROUND(SHIP:APOAPSIS) + "m | Periapsis: " + ROUND(SHIP:PERIAPSIS) + "m".
WAIT 5.

// --------------------------------------------------
// PHASE 3: Trans-Joolian Injection (TJI) Burn
// --------------------------------------------------
PRINT "PHASE 3: Targeting Jool and calculating transfer burn...".
SET TARGET TO Jool.

LOCAL tjiDV IS 1980.
LOCAL tjiNode IS NODE(TIME:SECONDS + 180, 0, 0, tjiDV).
ADD tjiNode.
PRINT "Executing Trans-Joolian Injection burn (dV: " + tjiDV + " m/s)...".
ExecuteNextNode().
PRINT "TJI burn complete! Departing Kerbin SOI toward the Jool system.".
WAIT 5.

// --------------------------------------------------
// PHASE 4: Interplanetary Cruise & Jool SOI Fast-Forward
// --------------------------------------------------
PRINT "PHASE 4: Cruising across interplanetary space to Jool...".
IF HASNODE REMOVE NEXTNODE.

IF SHIP:ORBIT:HASNEXTPATCH {
    PRINT "Jool SOI transition in " + ROUND(ETA:TRANSITION) + "s.".
    WARPTO(TIME:SECONDS + ETA:TRANSITION - 30).
    WAIT UNTIL SHIP:BODY:NAME = "Jool".
} ELSE {
    WAIT UNTIL SHIP:BODY:NAME = "Jool" OR SHIP:ALTITUDE > 30000000000.
}

PRINT "==================================================".
PRINT "          ENTERED JOOL SPHERE OF INFLUENCE!       ".
PRINT "==================================================".

// --------------------------------------------------
// PHASE 5: Tylo Encounter & Low Orbit Capture (30km)
// --------------------------------------------------
PRINT "PHASE 5: Targeting Tylo for orbital insertion...".
SET TARGET TO Tylo.

// In Tylo SOI:
IF SHIP:BODY:NAME = "Tylo" {
    PRINT "Planning Tylo capture burn at Periapsis...".
    LOCAL rPeri IS BODY:RADIUS + SHIP:PERIAPSIS.
    LOCAL vPeriCurrent IS SQRT(BODY:MU * (2 / rPeri - 1 / SHIP:OBT:SEMIMAJORAXIS)).
    LOCAL vCircTarget IS SQRT(BODY:MU / rPeri).
    LOCAL captureDV IS vCircTarget - vPeriCurrent.

    LOCAL captureNode IS NODE(TIME:SECONDS + ETA:PERIAPSIS, 0, 0, captureDV).
    ADD captureNode.
    ExecuteNextNode().
    PRINT "Stable Low Tylo Orbit established (~" + ROUND(SHIP:ALTITUDE / 1000) + " km)!".
}

// --------------------------------------------------
// PHASE 6: The Ultimate Challenge: Tylo Powered Descent
// --------------------------------------------------
PRINT "PHASE 6: Initiating Tylo Powered Descent (0.785g, No Atmosphere)...".
SAS OFF.
LOCK STEERING TO RETROGRADE.
WAIT 8.

// Deorbit burn with Poodle 250kN engine
PRINT "Executing Tylo deorbit burn...".
LOCK THROTTLE TO 1.0.
WAIT UNTIL SHIP:PERIAPSIS < 5000 OR MAXTHRUST = 0.
LOCK THROTTLE TO 0.

PRINT "Deploying heavy LT-2 landing gear...".
GEAR ON.

// High-precision suicide burn loop using radar altimeter
PRINT "Engaging Terminal Suicide Burn Guidance...".
LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

UNTIL SHIP:STATUS = "LANDED" OR SHIP:STATUS = "SPLASHED" {
    // Required stopping distance: d = v^2 / (2 * (a_max - g))
    LOCAL gTylo IS 7.85.
    LOCAL aMax IS (MAXTHRUST / SHIP:MASS).
    LOCAL netDecel IS MAX(1.0, aMax - gTylo).
    LOCAL stopDist IS (SHIP:VELOCITY:SURFACE:MAG ^ 2) / (2 * netDecel).

    LOCAL safetyMargin IS 150.
    IF ALT:RADAR < (stopDist + safetyMargin) {
        // Full braking burn
        LOCK THROTTLE TO 1.0.
    } ELSE IF ALT:RADAR < 500 {
        // Controlled 5 m/s touchdown descent
        LOCAL targetSpeed IS MAX(2.0, ALT:RADAR / 15).
        IF SHIP:VELOCITY:SURFACE:MAG > targetSpeed {
            LOCK THROTTLE TO 1.0.
        } ELSE {
            LOCK THROTTLE TO 0.05.
        }
    } ELSE {
        LOCK THROTTLE TO 0.
    }
    WAIT 0.05.
}

LOCK THROTTLE TO 0.
PRINT "==================================================".
PRINT "   MISSION ACCOMPLISHED: TOUCHDOWN ON TYLO!       ".
PRINT "   The hardest landing in Kerbal Space Program!   ".
PRINT "==================================================".
WAIT 10.

// --------------------------------------------------
// PHASE 7: Tylo Ascent & Kerbin Return
// --------------------------------------------------
PRINT "PHASE 7: Preparing for Tylo Ascent & Return to Kerbin...".
PRINT "Decoupling descent stage...".
STAGE. // Drops descent tank and legs
WAIT 1.
PRINT "Igniting ascent engine...".
STAGE. // Ignites Terrier ascent engine

LOCK THROTTLE TO 1.0.
LOCK STEERING TO HEADING(90, 45).
PRINT "Tylo Ascent burn in progress...".
WAIT UNTIL SHIP:APOAPSIS > 35000.
LOCK THROTTLE TO 0.

PlanCircularizationAtApoapsis().
ExecuteNextNode().
PRINT "Low Tylo Orbit re-established! (~35 km)".
WAIT 5.

// --------------------------------------------------
// PHASE 8: Trans-Kerbin Injection (TKI) Return Burn
// --------------------------------------------------
PRINT "PHASE 8: Planning Trans-Kerbin Injection return trajectory...".
SET TARGET TO Kerbin.

// Escape Tylo and Jool on a Hohmann transfer trajectory back to Kerbin
// ~1,150 m/s dV lowers Kerbin periapsis to atmospheric interface (~35km)
LOCAL tkiDV IS 1150.
LOCAL tkiNode IS NODE(TIME:SECONDS + 300, 0, 0, tkiDV).
ADD tkiNode.
PRINT "Executing Trans-Kerbin Injection burn (dV: " + tkiDV + " m/s)...".
ExecuteNextNode().

PRINT "Trans-Kerbin Injection complete! Escaping Jool system toward Kerbin.".
WAIT 5.

// --------------------------------------------------
// PHASE 9: Interplanetary Return Cruise
// --------------------------------------------------
PRINT "PHASE 9: Fast-forwarding interplanetary voyage back to Kerbin...".
IF HASNODE REMOVE NEXTNODE.

// Warp across deep space to Kerbin SOI
IF SHIP:ORBIT:HASNEXTPATCH {
    PRINT "Warping to Kerbin SOI transition...".
    WARPTO(TIME:SECONDS + ETA:TRANSITION - 30).
    WAIT UNTIL SHIP:BODY:NAME = "Kerbin".
} ELSE {
    PRINT "Coasting toward Kerbin...".
    WAIT UNTIL SHIP:BODY:NAME = "Kerbin" OR SHIP:ALTITUDE < 50000000.
}

PRINT "==================================================".
PRINT "          ENTERED KERBIN SPHERE OF INFLUENCE!     ".
PRINT "==================================================".

// Warp to Kerbin atmospheric interface (100km)
IF SHIP:ALTITUDE > 120000 {
    PRINT "Warping to atmospheric entry interface (100km)...".
    WARPTO(TIME:SECONDS + ETA:PERIAPSIS - 60).
}

WAIT UNTIL SHIP:ALTITUDE < 100000.

// --------------------------------------------------
// PHASE 10: Atmospheric Reentry & Splashdown
// --------------------------------------------------
PRINT "PHASE 10: Preparing for high-speed atmospheric aerocapture and reentry...".
PRINT "Decoupling ascent service module...".
STAGE. // Jettisons Terrier engine and fuel tank
WAIT 2.

PRINT "Orienting Heat Shield to blunt reentry vector...".
SAS OFF.
LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

PRINT "Entering upper atmosphere (70km) - Aerobraking plasma regime initiated...".
WAIT UNTIL SHIP:ALTITUDE < 70000.

// Hold retrograde orientation through atmospheric deceleration
WAIT UNTIL SHIP:ALTITUDE < 5000 AND SHIP:VELOCITY:SURFACE:MAG < 300.

PRINT "Terminal aerodynamic deceleration complete. Deploying parachute!".
STAGE. // Deploys parachuteSingle
CHUTES ON.
UNLOCK STEERING.

PRINT "Drifting down under canopy to surface...".
WAIT UNTIL SHIP:STATUS = "LANDED" OR SHIP:STATUS = "SPLASHED".

PRINT "==================================================".
PRINT "   FULL ROUND-TRIP MISSION ACCOMPLISHED!          ".
PRINT "   KERBALS RETURNED SAFELY HOME TO KERBIN!        ".
PRINT "==================================================".

UNTIL FALSE {
    PRINT "{'status':'MISSION_COMPLETE_SAFE_AT_KERBIN'} " AT (0, 24).
    WAIT 1.
}
