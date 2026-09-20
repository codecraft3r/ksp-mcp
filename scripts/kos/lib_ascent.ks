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
    STAGE.

    // Auto-staging trigger
    WHEN STAGE:NUMBER > 2 AND MAXTHRUST = 0 THEN {
        PRINT "Stage flameout detected. Staging!".
        STAGE.
        PRESERVE.
    }

    // Ascent gravity turn loop
    UNTIL SHIP:APOAPSIS >= targetApoapsis {
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
        WAIT UNTIL SHIP:ALTITUDE > 70000.
    }
    PRINT "Outside atmosphere. Ready for circularization.".
}
