// tylo_mission.ks - Autonomous Interplanetary Tylo Landing & Kerbin Return Mission
// 6-Stage Clean Hierarchy:
// Stage 5: Mammoth Liftoff
// Stage 4: Booster Decoupler + Rhino Cruiser Ignition
// Stage 3: Transfer Decoupler + Poodle Descent Ignition
// Stage 2: Ascent Decoupler + Terrier Ascent Ignition
// Stage 1: Payload Decoupler
// Stage 0: Recovery Parachute

@LAZYGLOBAL OFF.
CLEARSCREEN.
PRINT "==================================================".
PRINT "       KSP MCP TYLO EXTREME LANDING MISSION       ".
PRINT "==================================================".

RUNONCEPATH("0:/lib_math.ks").
RUNONCEPATH("0:/lib_ascent.ks").
RUNONCEPATH("0:/lib_orbit.ks").
RUNONCEPATH("0:/lib_node.ks").
RUNONCEPATH("0:/lib_warp.ks").

// --------------------------------------------------
// PHASE 1: Super-Heavy Launch to 80km Low Kerbin Orbit
// --------------------------------------------------
IF SHIP:STATUS = "PRELAUNCH" OR SHIP:ALTITUDE < 70000 {
    PRINT "PHASE 1: Launching 253-tonne Tylo Master Lander to 80km LKO...".
    LaunchToOrbit(80000, 90).
}

// --------------------------------------------------
// PHASE 2: LKO Circularization Burn
// --------------------------------------------------
IF SHIP:BODY:NAME = "Kerbin" AND SHIP:PERIAPSIS < 70000 {
    PRINT "PHASE 2: Planning and executing circularization at Apoapsis...".
    PlanCircularizationAtApoapsis().
    ExecuteNextNode().
    PRINT "Stable Low Kerbin Orbit confirmed!".
    PRINT "Current Apoapsis: " + ROUND(SHIP:APOAPSIS) + "m | Periapsis: " + ROUND(SHIP:PERIAPSIS) + "m".
    WAIT 3.
}

// --------------------------------------------------
// PHASE 3: Trans-Joolian Injection (TJI) Burn
// --------------------------------------------------
IF SHIP:BODY:NAME = "Kerbin" {
    PRINT "PHASE 3: Targeting Jool and calculating transfer burn...".
    SET TARGET TO "Jool".

    LOCAL tjiDV IS 2750.
    LOCAL tjiNode IS NODE(TIME:SECONDS + 150, 0, 0, tjiDV).
    ADD tjiNode.
    PRINT "Executing Trans-Joolian Injection burn (dV: " + tjiDV + " m/s)...".
    ExecuteNextNode().
    PRINT "TJI burn complete! Departing Kerbin SOI toward the Jool system.".
    WAIT 3.
}

// --------------------------------------------------
// PHASE 4: Interplanetary Cruise & Jool Fast-Forward
// --------------------------------------------------
IF SHIP:BODY:NAME <> "Jool" AND SHIP:BODY:NAME <> "Tylo" {
    PRINT "PHASE 4: Cruising across interplanetary space to Jool (100,000x warp)...".
    IF HASNODE REMOVE NEXTNODE.

    SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
    UNTIL SHIP:BODY:NAME = "Jool" OR SHIP:BODY:NAME = "Tylo" {
        IF SHIP:ORBIT:HASNEXTPATCH AND ETA:TRANSITION < 60 {
            SET KUNIVERSE:TIMEWARP:WARP TO 0.
            WAIT UNTIL SHIP:ORBIT:HASNEXTPATCH = FALSE OR ETA:TRANSITION > 100.
            WAIT 5.
        } ELSE IF KUNIVERSE:TIMEWARP:WARP < 7 {
            SET KUNIVERSE:TIMEWARP:WARP TO 7.
        }
        WAIT 0.5.
    }

    SET KUNIVERSE:TIMEWARP:WARP TO 0.
    WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.
    PRINT "==================================================".
    PRINT "          ENTERED JOOL SPHERE OF INFLUENCE!       ".
    PRINT "==================================================".
}

// --------------------------------------------------
// PHASE 5: Tylo Encounter & Low Orbit Capture (30km)
// --------------------------------------------------
PRINT "PHASE 5: Targeting Tylo for orbital insertion...".
SET TARGET TO "Tylo".

// In Jool SOI, warp to Tylo encounter
IF SHIP:BODY:NAME = "Jool" {
    IF SHIP:ORBIT:HASNEXTPATCH AND SHIP:ORBIT:NEXTPATCH:BODY:NAME = "Tylo" {
        PRINT "Tylo encounter confirmed in " + ROUND(ETA:TRANSITION) + "s. Warping...".
        SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
        UNTIL SHIP:BODY:NAME = "Tylo" {
            IF ETA:TRANSITION < 60 SET KUNIVERSE:TIMEWARP:WARP TO 0.
            ELSE IF KUNIVERSE:TIMEWARP:WARP < 7 SET KUNIVERSE:TIMEWARP:WARP TO 7.
            WAIT 0.5.
        }
        SET KUNIVERSE:TIMEWARP:WARP TO 0.
        WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.
    }
}

// In Tylo SOI: Stage 3 drops Rhino and ignites Poodle
IF SHIP:BODY:NAME = "Tylo" AND STAGE:NUMBER > 3 {
    PRINT "In Tylo SOI. Staging to Poodle Descent Lander...".
    STAGE. // Stage 3: Decouples Rhino cruiser AND ignites Poodle 250kN engine!
    WAIT 2.

    PRINT "Planning Tylo capture burn at Periapsis...".
    LOCAL rPeri IS BODY:RADIUS + 30000.
    LOCAL vPeriCurrent IS SQRT(BODY:MU * (2 / rPeri - 1 / SHIP:OBT:SEMIMAJORAXIS)).
    LOCAL vCircTarget IS SQRT(BODY:MU / rPeri).
    LOCAL captureDV IS vCircTarget - vPeriCurrent.

    LOCAL captureNode IS NODE(TIME:SECONDS + ETA:PERIAPSIS, 0, 0, captureDV).
    ADD captureNode.
    ExecuteNextNode().
    PRINT "Stable Low Tylo Orbit established (~" + ROUND(SHIP:ALTITUDE / 1000) + " km)!".
    WAIT 3.
}

// --------------------------------------------------
// PHASE 6: The Ultimate Challenge: Tylo Powered Descent
// --------------------------------------------------
IF SHIP:BODY:NAME = "Tylo" AND SHIP:STATUS = "ORBITING" {
    PRINT "PHASE 6: Initiating Tylo Powered Descent (0.785g, No Atmosphere)...".
    SAS OFF.
    LOCK STEERING TO RETROGRADE.
    WAIT 5.

    PRINT "Executing Tylo deorbit burn...".
    LOCK THROTTLE TO 1.0.
    WAIT UNTIL SHIP:PERIAPSIS < 4000 OR (STAGE:NUMBER > 2 AND AVAILABLETHRUST < 1).
    LOCK THROTTLE TO 0.

    PRINT "Deploying heavy LT-2 landing gear...".
    GEAR ON.

    PRINT "Engaging Terminal Suicide Burn Guidance...".
    LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

    UNTIL SHIP:STATUS = "LANDED" OR SHIP:STATUS = "SPLASHED" {
        LOCAL gTylo IS 7.85.
        LOCAL aMax IS (MAXTHRUST / SHIP:MASS).
        LOCAL netDecel IS MAX(1.0, aMax - gTylo).
        LOCAL stopDist IS (SHIP:VELOCITY:SURFACE:MAG ^ 2) / (2 * netDecel).

        LOCAL safetyMargin IS 120.
        IF ALT:RADAR < (stopDist + safetyMargin) {
            LOCK THROTTLE TO 1.0.
        } ELSE IF ALT:RADAR < 400 {
            LOCAL targetSpeed IS MAX(2.0, ALT:RADAR / 15).
            IF SHIP:VELOCITY:SURFACE:MAG > targetSpeed {
                LOCK THROTTLE TO 1.0.
            } ELSE {
                LOCK THROTTLE TO 0.08.
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
    WAIT 8.
}

// --------------------------------------------------
// PHASE 7: Tylo Ascent to Orbit
// --------------------------------------------------
IF SHIP:BODY:NAME = "Tylo" AND (SHIP:STATUS = "LANDED" OR SHIP:STATUS = "SPLASHED") {
    PRINT "PHASE 7: Preparing for Tylo Ascent...".
    PRINT "Staging: Decoupling descent stage & igniting Terrier ascent engine...".
    STAGE. // Stage 2: Drops Poodle stage/legs AND ignites Terrier engine!
    WAIT 1.

    LOCK THROTTLE TO 1.0.
    LOCK STEERING TO HEADING(90, 45).
    PRINT "Tylo Ascent burn in progress...".
    WAIT UNTIL SHIP:APOAPSIS > 35000.
    LOCK THROTTLE TO 0.

    PlanCircularizationAtApoapsis().
    ExecuteNextNode().
    PRINT "Low Tylo Orbit re-established! (~35 km)".
    WAIT 3.
}

// --------------------------------------------------
// PHASE 8: Trans-Kerbin Injection (TKI) Return Burn
// --------------------------------------------------
IF SHIP:BODY:NAME = "Tylo" {
    PRINT "PHASE 8: Planning Trans-Kerbin Injection return trajectory...".
    SET TARGET TO "Kerbin".

    LOCAL tkiDV IS 1150.
    LOCAL tkiNode IS NODE(TIME:SECONDS + 200, 0, 0, tkiDV).
    ADD tkiNode.
    PRINT "Executing Trans-Kerbin Injection burn (dV: " + tkiDV + " m/s)...".
    ExecuteNextNode().
    PRINT "Trans-Kerbin Injection complete! Escaping Jool system toward Kerbin.".
    WAIT 3.
}

// --------------------------------------------------
// PHASE 9: Interplanetary Return Cruise (100,000x Warp)
// --------------------------------------------------
IF SHIP:BODY:NAME <> "Kerbin" {
    PRINT "PHASE 9: Fast-forwarding interplanetary voyage back to Kerbin...".
    IF HASNODE REMOVE NEXTNODE.

    SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
    UNTIL SHIP:BODY:NAME = "Kerbin" {
        IF SHIP:ORBIT:HASNEXTPATCH AND ETA:TRANSITION < 60 {
            SET KUNIVERSE:TIMEWARP:WARP TO 0.
            WAIT UNTIL SHIP:ORBIT:HASNEXTPATCH = FALSE OR ETA:TRANSITION > 100.
            WAIT 5.
        } ELSE IF KUNIVERSE:TIMEWARP:WARP < 7 {
            SET KUNIVERSE:TIMEWARP:WARP TO 7.
        }
        WAIT 0.5.
    }

    SET KUNIVERSE:TIMEWARP:WARP TO 0.
    WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.
    PRINT "==================================================".
    PRINT "          ENTERED KERBIN SPHERE OF INFLUENCE!     ".
    PRINT "==================================================".
}

// --------------------------------------------------
// PHASE 10: Reentry, Parachute & Ocean Splashdown
// --------------------------------------------------
PRINT "PHASE 10: Preparing for atmospheric reentry and recovery...".
IF SHIP:ALTITUDE > 100000 {
    PRINT "Warping to entry interface (100km)...".
    WARPTO(TIME:SECONDS + ETA:PERIAPSIS - 90).
    WAIT UNTIL SHIP:ALTITUDE < 95000.
}

PRINT "Decoupling ascent stage...".
STAGE. // Stage 1: Drops Terrier engine and tank, exposing heat shield!
WAIT 2.

PRINT "Orienting Heat Shield to blunt reentry vector...".
SAS OFF.
LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

PRINT "Entering upper atmosphere (70km) - Aerobraking initiated!".
WAIT UNTIL SHIP:ALTITUDE < 70000.

WAIT UNTIL SHIP:ALTITUDE < 5000 AND SHIP:VELOCITY:SURFACE:MAG < 300.

PRINT "Terminal aerodynamic deceleration complete. Deploying parachute!".
STAGE. // Stage 0: Deploys parachuteSingle!
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
