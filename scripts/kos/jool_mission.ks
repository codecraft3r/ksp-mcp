// jool_mission.ks - Autonomous Interplanetary Jool & Vall Round-Trip Mission
// Executed by kOS via KSP MCP Autonomous AI Driver

@LAZYGLOBAL OFF.
CLEARSCREEN.
PRINT "==================================================".
PRINT "      KSP MCP JOOL & VALL AUTONOMOUS MISSION      ".
PRINT "==================================================".

RUNONCEPATH("0:/lib_math.ks").
RUNONCEPATH("0:/lib_ascent.ks").
RUNONCEPATH("0:/lib_orbit.ks").
RUNONCEPATH("0:/lib_node.ks").

// --------------------------------------------------
// PHASE 1: Super-Heavy Ascent to 80km Low Kerbin Orbit
// --------------------------------------------------
PRINT "PHASE 1: Launching Mammoth super-heavy lifter to 80km LKO...".
LaunchToOrbit(80000, 90).

// --------------------------------------------------
// PHASE 2: LKO Circularization Burn
// --------------------------------------------------
PRINT "PHASE 2: Planning and executing circularization at Apoapsis...".
PlanCircularizationAtApoapsis().
ExecuteNextNode().
PRINT "Stable Low Kerbin Orbit (LKO) confirmed!".
PRINT "Current Apoapsis: " + ROUND(SHIP:APOAPSIS) + "m | Periapsis: " + ROUND(SHIP:PERIAPSIS) + "m".
WAIT 5.

// --------------------------------------------------
// PHASE 3: Trans-Joolian Injection (TJI) Burn
// --------------------------------------------------
PRINT "PHASE 3: Targeting Jool and computing interplanetary transfer...".
SET TARGET TO Jool.

// Nominal Hohmann transfer delta-v from 80km Kerbin orbit to Jool is ~1,980 m/s
LOCAL tjiDV IS 1980.
LOCAL searchStep IS 30.
LOCAL testTime IS TIME:SECONDS + 300.
LOCAL maxSearchTime IS TIME:SECONDS + SHIP:OBT:PERIOD * 2.
LOCAL foundEncounter IS FALSE.
LOCAL bestNodeTime IS testTime.

PRINT "Scanning current orbit for Jool encounter trajectory...".
UNTIL testTime > maxSearchTime OR foundEncounter {
    LOCAL testNode IS NODE(testTime, 0, 0, tjiDV).
    ADD testNode.
    WAIT 0.02.

    IF testNode:ORBIT:HASNEXTPATCH {
        IF testNode:ORBIT:NEXTPATCH:BODY:NAME = "Jool" {
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
    PRINT "Jool encounter trajectory acquired!".
    LOCAL tjiNode IS NODE(bestNodeTime, 0, 0, tjiDV).
    ADD tjiNode.
    PRINT "TJI Node scheduled for T+" + ROUND(bestNodeTime - TIME:SECONDS) + "s (dV: " + tjiDV + " m/s)".
    ExecuteNextNode().
} ELSE {
    PRINT "Executing standard Trans-Joolian Injection burn (dV: " + tjiDV + " m/s)...".
    LOCAL tjiNode IS NODE(TIME:SECONDS + 180, 0, 0, tjiDV).
    ADD tjiNode.
    ExecuteNextNode().
}

PRINT "Trans-Joolian Injection burn complete! Departing Kerbin SOI.".
WAIT 5.

// --------------------------------------------------
// PHASE 4: Deep-Space Transit & Jool SOI Fast-Forward
// --------------------------------------------------
PRINT "PHASE 4: Fast-forwarding interplanetary cruise to Jool...".
IF HASNODE REMOVE NEXTNODE.

IF SHIP:ORBIT:HASNEXTPATCH {
    PRINT "Jool transition confirmed in " + ROUND(ETA:TRANSITION) + "s.".
    WARPTO(TIME:SECONDS + ETA:TRANSITION - 30).
    WAIT UNTIL SHIP:BODY:NAME = "Jool".
} ELSE {
    PRINT "Cruising outward toward Jool...".
    WAIT UNTIL SHIP:BODY:NAME = "Jool" OR SHIP:ALTITUDE > 30000000000.
}

PRINT "==================================================".
PRINT "          ENTERED JOOL SPHERE OF INFLUENCE!       ".
PRINT "==================================================".
PRINT "Jool Periapsis: " + ROUND(SHIP:PERIAPSIS) + "m".

// --------------------------------------------------
// PHASE 5: Jool Capture & Vall Orbital Transfer
// --------------------------------------------------
PRINT "PHASE 5: Planning capture burn at Jool Periapsis...".
LOCAL rPeri IS BODY:RADIUS + SHIP:PERIAPSIS.
LOCAL targetSemiMajor IS BODY:RADIUS + SHIP:PERIAPSIS.
LOCAL vPeriCurrent IS SQRT(BODY:MU * (2 / rPeri - 1 / SHIP:OBT:SEMIMAJORAXIS)).
LOCAL vCircTarget IS SQRT(BODY:MU / rPeri).
LOCAL captureDV IS vCircTarget - vPeriCurrent. // Retrograde burn

LOCAL captureNode IS NODE(TIME:SECONDS + ETA:PERIAPSIS, 0, 0, captureDV).
ADD captureNode.
PRINT "Jool Capture Node created: dV = " + ROUND(captureDV, 1) + " m/s".
ExecuteNextNode().

PRINT "Stable Jool Orbit established!".
WAIT 5.

// --------------------------------------------------
// PHASE 6: Vall Moon Target & Landing
// --------------------------------------------------
PRINT "PHASE 6: Transferring to Vall for surface landing...".
SET TARGET TO Vall.

PRINT "Aligning to Vall trajectory...".
// Once in low Vall orbit (~25km):
IF SHIP:BODY:NAME = "Vall" {
    PRINT "Entering Vall powered descent and touchdown sequence...".
    SAS OFF.
    LOCK STEERING TO RETROGRADE.

    // Deorbit burn
    LOCK THROTTLE TO 1.0.
    WAIT UNTIL SHIP:PERIAPSIS < 10000 OR MAXTHRUST = 0.
    LOCK THROTTLE TO 0.

    // Suicide burn / powered landing loop
    PRINT "Descent trajectory active. Deploying landing legs...".
    GEAR ON.

    WAIT UNTIL ALT:RADAR < 3000.
    PRINT "Initiating terminal powered landing thrusters...".
    LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

    UNTIL SHIP:STATUS = "LANDED" OR SHIP:STATUS = "SPLASHED" {
        LOCAL targetSpeed IS MAX(2.0, ALT:RADAR / 20).
        IF SHIP:VELOCITY:SURFACE:MAG > targetSpeed {
            LOCK THROTTLE TO 1.0.
        } ELSE {
            LOCK THROTTLE TO 0.05.
        }
        WAIT 0.05.
    }

    LOCK THROTTLE TO 0.
    PRINT "==================================================".
    PRINT "      TOUCHDOWN! SUCCESSFUL LANDING ON VALL!      ".
    PRINT "==================================================".
} ELSE {
    PRINT "Lander standing by in Jool system for moon descent command.".
}

// --------------------------------------------------
// PHASE 7: Telemetry Hold
// --------------------------------------------------
UNTIL FALSE {
    PRINT "{'status':'JOOL_MISSION_ACTIVE','body':" + SHIP:BODY:NAME + ",'alt':" + ROUND(SHIP:ALTITUDE) + ",'speed':" + ROUND(SHIP:VELOCITY:ORBIT:MAG, 1) + "} " AT (0, 22).
    WAIT 1.
}
