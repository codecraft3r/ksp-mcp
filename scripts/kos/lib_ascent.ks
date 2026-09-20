// lib_ascent.ks - KSP Autonomous Ascent & Gravity Turn Guidance
@LAZYGLOBAL OFF.

GLOBAL FUNCTION LaunchToOrbit {
    PARAMETER targetApoapsis, targetHeading IS 90.

    SAS OFF.
    RCS OFF.
    LOCAL turnStartAlt IS 1000.
    LOCAL turnEndAlt IS 55000.

    LOCK THROTTLE TO 1.0.
    LOCK STEERING TO HEADING(targetHeading, 90).

    PRINT "T-0: Ignition and Lift-off!".
    STAGE. // Ignites Mammoth Booster (Stage 8)

    LOCAL stagedBooster IS FALSE.

    // Ascent gravity turn loop
    UNTIL SHIP:APOAPSIS >= targetApoapsis {
        // If Mammoth burns out during ascent, drop it and ignite Rhino
        IF NOT stagedBooster AND THROTTLE > 0 AND MAXTHRUST > 0 AND STAGE:LIQUIDFUEL < 1 {
            PRINT "Mammoth booster burnout. Jettisoning booster!".
            STAGE. // Decouple Mammoth (Stage 7)
            WAIT 0.8.
            STAGE. // Ignite Rhino (Stage 6)
            SET stagedBooster TO TRUE.
            PRINT "Rhino interplanetary cruiser engine ignited!".
        }

        IF SHIP:ALTITUDE > turnStartAlt {
            LOCAL frac IS (SHIP:ALTITUDE - turnStartAlt) / (turnEndAlt - turnStartAlt).
            IF frac > 1.0 SET frac TO 1.0.
            LOCAL targetPitch IS 90 - (frac * 85).
            LOCK STEERING TO HEADING(targetHeading, targetPitch).
        }
        WAIT 0.1.
    }

    PRINT "Target apoapsis reached: " + ROUND(SHIP:APOAPSIS) + "m. MECO!".
    LOCK THROTTLE TO 0.
    SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.

    // Coast out of atmosphere
    IF SHIP:ALTITUDE < 70000 {
        PRINT "Coasting out of atmosphere (70km)...".
        IF SHIP:ALTITUDE > 35000 {
            SET KUNIVERSE:TIMEWARP:MODE TO "PHYSICS".
            SET KUNIVERSE:TIMEWARP:WARP TO 3.
        }
        WAIT UNTIL SHIP:ALTITUDE > 70000.
        SET KUNIVERSE:TIMEWARP:WARP TO 0.
        WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.
    }
    PRINT "Outside atmosphere. Ready for circularization.".
}
