// mun_crash.ks - Mun Kinetic Impactor Deorbit Script
// Autonomous targeted crash into the Mun surface

@LAZYGLOBAL OFF.
CLEARSCREEN.
PRINT "==================================================".
PRINT "       KSP MCP MUN KINETIC IMPACT DEORBIT         ".
PRINT "==================================================".

PRINT "1. Orienting RETROGRADE for deorbit burn...".
SAS OFF.
LOCK STEERING TO RETROGRADE.
WAIT 8.

PRINT "2. Initiating deorbit burn - burning all remaining propellant!".
LOCK THROTTLE TO 1.0.

UNTIL SHIP:PERIAPSIS < -100000 OR MAXTHRUST = 0 {
    PRINT "Current Periapsis: " + ROUND(SHIP:PERIAPSIS) + " m   " AT (0, 6).
    PRINT "Current Altitude:  " + ROUND(SHIP:ALTITUDE) + " m   " AT (0, 7).
    WAIT 0.1.
}

PRINT "Deorbit trajectory locked! Target: Sub-surface collision course.".
LOCK THROTTLE TO 0.
SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.

PRINT "3. Aligning to Surface Velocity Vector for direct impact...".
LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

// Warp toward impact until 15km above terrain
IF SHIP:ALTITUDE > 25000 {
    PRINT "Warping towards Mun surface...".
    WARPTO(TIME:SECONDS + ETA:PERIAPSIS - 30).
}

WAIT UNTIL ALT:RADAR < 15000.

PRINT "==================================================".
PRINT "          TERMINAL IMPACT DIVE INITIATED          ".
PRINT "==================================================".

// Final dive telemetry loop until catastrophic impact
UNTIL FALSE {
    LOCAL rAlt IS ROUND(ALT:RADAR).
    LOCAL surfSpd IS ROUND(SHIP:VELOCITY:SURFACE:MAG, 1).
    PRINT "{""event"":""IMPACT_TRAJECTORY"",""radar_alt"":" + rAlt + ",""speed"":" + surfSpd + "}   " AT (0, 14).
    WAIT 0.1.
}
