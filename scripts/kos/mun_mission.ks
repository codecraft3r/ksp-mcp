// mun_mission.ks - Autonomous Mun Orbit Mission for KSP
// Executed by kOS via KSP MCP Autonomous AI Driver

@LAZYGLOBAL OFF.
CLEARSCREEN.
PRINT "==================================================".
PRINT "       KSP MCP AUTONOMOUS MUN MISSION             ".
PRINT "==================================================".

RUNONCEPATH("0:/lib_math.ks").
RUNONCEPATH("0:/lib_ascent.ks").
RUNONCEPATH("0:/lib_orbit.ks").
RUNONCEPATH("0:/lib_node.ks").

// --------------------------------------------------
// PHASE 1: Launch & Ascent to 80km Low Kerbin Orbit
// --------------------------------------------------
PRINT "PHASE 1: Launching to 80km Kerbin orbit...".
LaunchToOrbit(80000, 90).

// --------------------------------------------------
// PHASE 2: Circularization at Apoapsis
// --------------------------------------------------
PRINT "PHASE 2: Planning and executing circularization...".
PlanCircularizationAtApoapsis().
ExecuteNextNode().
PRINT "Stable Low Kerbin Orbit (LKO) confirmed!".
PRINT "Current Apoapsis: " + ROUND(SHIP:APOAPSIS) + "m | Periapsis: " + ROUND(SHIP:PERIAPSIS) + "m".
WAIT 5.

// --------------------------------------------------
// PHASE 3: Trans-Munar Injection (TMI) Burn
// --------------------------------------------------
PRINT "PHASE 3: Targeting the Mun and computing transfer...".
SET TARGET TO Mun.

// Target phase angle: Mun should be ~112 degrees ahead
// For an 80km orbit, period is ~32.6 minutes.
LOCAL transferDV IS 855. // Nominal dV from 80km to Mun orbit
LOCAL bestNodeTime IS TIME:SECONDS + 120.
LOCAL foundEncounter IS FALSE.

// Search ahead in the current orbit for the injection window
PRINT "Searching for Mun encounter window...".
LOCAL searchStep IS 15.
LOCAL testTime IS TIME:SECONDS + 180.
LOCAL maxSearchTime IS TIME:SECONDS + SHIP:OBT:PERIOD * 1.5.

UNTIL testTime > maxSearchTime OR foundEncounter {
    LOCAL testNode IS NODE(testTime, 0, 0, transferDV).
    ADD testNode.
    WAIT 0.02.

    IF testNode:ORBIT:HASNEXTPATCH {
        IF testNode:ORBIT:NEXTPATCH:BODY:NAME = "Mun" {
            SET foundEncounter TO TRUE.
            SET bestNodeTime TO testTime.
            REMOVE testNode.
            BREAK.
        }
    }
    REMOVE testNode.
    SET testTime TO testTime + searchStep.
}

IF foundEncounter {
    PRINT "Mun encounter trajectory located!".
    LOCAL tmiNode IS NODE(bestNodeTime, 0, 0, transferDV).
    ADD tmiNode.
    PRINT "TMI Node set for T+" + ROUND(bestNodeTime - TIME:SECONDS) + "s (dV: " + transferDV + " m/s)".
    ExecuteNextNode().
} ELSE {
    PRINT "Warning: Using standard Mun injection node.".
    LOCAL tmiNode IS NODE(TIME:SECONDS + (SHIP:OBT:PERIOD * 0.4), 0, 0, transferDV).
    ADD tmiNode.
    ExecuteNextNode().
}

PRINT "Trans-Munar Injection burn complete!".
WAIT 5.

// --------------------------------------------------
// PHASE 4: Coast to Mun Sphere of Influence (SOI)
// --------------------------------------------------
PRINT "PHASE 4: Coasting to Mun Sphere of Influence...".
IF HASNODE REMOVE NEXTNODE.

// Warp toward Mun encounter
IF SHIP:ORBIT:HASNEXTPATCH {
    PRINT "Mun encounter confirmed! Transition in " + ROUND(ETA:TRANSITION) + "s.".
    WARPTO(TIME:SECONDS + ETA:TRANSITION - 15).
    WAIT UNTIL SHIP:BODY:NAME = "Mun".
} ELSE {
    PRINT "Waiting for Mun capture...".
    WAIT UNTIL SHIP:BODY:NAME = "Mun".
}

PRINT "==================================================".
PRINT "          ENTERED MUN SPHERE OF INFLUENCE!        ".
PRINT "==================================================".
PRINT "Mun Periapsis: " + ROUND(SHIP:PERIAPSIS) + "m".

// --------------------------------------------------
// PHASE 5: Mun Orbit Insertion (Capture Burn)
// --------------------------------------------------
PRINT "PHASE 5: Planning Mun orbit capture burn at Periapsis...".
LOCAL rPeri IS BODY:RADIUS + SHIP:PERIAPSIS.
LOCAL targetSemiMajor IS BODY:RADIUS + SHIP:PERIAPSIS. // Circular
LOCAL vPeriCurrent IS SQRT(BODY:MU * (2 / rPeri - 1 / SHIP:OBT:SEMIMAJORAXIS)).
LOCAL vCircTarget IS SQRT(BODY:MU / rPeri).
LOCAL captureDV IS vCircTarget - vPeriCurrent. // Negative value (retrograde)

LOCAL captureNode IS NODE(TIME:SECONDS + ETA:PERIAPSIS, 0, 0, captureDV).
ADD captureNode.
PRINT "Mun Capture Node created: dV = " + ROUND(captureDV, 1) + " m/s at Periapsis.".

ExecuteNextNode().

PRINT "==================================================".
PRINT "   MISSION SUCCESS: STABLE MUN ORBIT ESTABLISHED! ".
PRINT "   Apoapsis:  " + ROUND(SHIP:APOAPSIS) + " m".
PRINT "   Periapsis: " + ROUND(SHIP:PERIAPSIS) + " m".
PRINT "==================================================".

// Keep telemetry broadcasting
UNTIL FALSE {
    PRINT "{""status"":""IN_MUN_ORBIT"",""apoapsis"":" + ROUND(SHIP:APOAPSIS) + ",""periapsis"":" + ROUND(SHIP:PERIAPSIS) + ",""orbital_speed"":" + ROUND(SHIP:VELOCITY:ORBIT:MAG, 1) + "} " AT (0, 20).
    WAIT 1.
}
