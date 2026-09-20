// kerbin_recovery.ks - Autonomous Emergency Kerbin Return & Splashdown
@LAZYGLOBAL OFF.
CLEARSCREEN.
PRINT "==================================================".
PRINT "    EMERGENCY RECOVERY: BRINGING VESSEL HOME      ".
PRINT "==================================================".

SET TARGET TO "Kerbin".
SAS OFF.
RCS OFF.

// --------------------------------------------------
// STEP 1: Direct Ballistic Intercept Burn
// --------------------------------------------------
PRINT "Targeting Kerbin for direct interplanetary intercept...".
PRINT "Current distance: " + ROUND(TARGET:DISTANCE / 1000000, 1) + " million km.".

// Target closing speed: 1,200 m/s toward Kerbin
LOCAL vClose IS 1200.
LOCAL targetDir IS TARGET:POSITION:NORMALIZED.
LOCAL desiredVel IS TARGET:VELOCITY:ORBIT + (targetDir * vClose).
LOCAL dVVector IS desiredVel - SHIP:VELOCITY:ORBIT.

PRINT "Aligning to intercept burn vector (dV: " + ROUND(dVVector:MAG) + " m/s)...".
LOCK STEERING TO dVVector.
WAIT 8.

PRINT "Executing intercept burn!".
LOCK THROTTLE TO 1.0.

UNTIL VDOT(dVVector, desiredVel - SHIP:VELOCITY:ORBIT) <= 50 OR (STAGE:NUMBER > 1 AND MAXTHRUST = 0) {
    IF (desiredVel - SHIP:VELOCITY:ORBIT):MAG < 100 {
        LOCK THROTTLE TO 0.2.
    }
    WAIT 0.05.
}
LOCK THROTTLE TO 0.
UNLOCK STEERING.
PRINT "Intercept burn complete!".
WAIT 3.

// --------------------------------------------------
// STEP 2: Fine-tune Trajectory for Kerbin SOI Entry
// --------------------------------------------------
PRINT "Fine-tuning trajectory toward Kerbin center...".
LOCK STEERING TO TARGET:POSITION.
WAIT 5.

LOCAL burnLimit IS TIME:SECONDS + 20.
LOCK THROTTLE TO 0.1.
UNTIL (SHIP:ORBIT:HASNEXTPATCH AND SHIP:ORBIT:NEXTPATCH:BODY:NAME = "Kerbin") OR TIME:SECONDS > burnLimit {
    WAIT 0.1.
}
LOCK THROTTLE TO 0.
UNLOCK STEERING.

// --------------------------------------------------
// STEP 3: Hyper-Warp to Kerbin SOI (100,000x Rails Warp)
// --------------------------------------------------
PRINT "==================================================".
PRINT "Fast-forwarding cruise back to Kerbin (100,000x)...".
PRINT "==================================================".

SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
UNTIL SHIP:BODY:NAME = "Kerbin" OR TARGET:DISTANCE < 80000000 {
    IF KUNIVERSE:TIMEWARP:WARP < 7 {
        SET KUNIVERSE:TIMEWARP:WARP TO 7.
    }
    PRINT "Closing distance: " + ROUND(TARGET:DISTANCE / 1000000, 1) + "M km | Warp: " + KUNIVERSE:TIMEWARP:WARP AT (0, 14).
    WAIT 1.
}

SET KUNIVERSE:TIMEWARP:WARP TO 0.
WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.
PRINT "KERBIN VICINITY REACHED!".

// --------------------------------------------------
// STEP 4: Set Atmospheric Entry Periapsis (35km)
// --------------------------------------------------
IF SHIP:BODY:NAME = "Kerbin" {
    PRINT "Inside Kerbin SOI! Adjusting periapsis to 35km...".
    SAS OFF.
    IF SHIP:PERIAPSIS > 40000 {
        LOCK STEERING TO RETROGRADE.
        WAIT 5.
        LOCK THROTTLE TO 0.2.
        WAIT UNTIL SHIP:PERIAPSIS <= 35000 OR MAXTHRUST = 0.
        LOCK THROTTLE TO 0.
    }
    UNLOCK STEERING.
}

// --------------------------------------------------
// STEP 5: Warp to Atmospheric Interface (90km)
// --------------------------------------------------
PRINT "Warping to atmospheric entry interface (90km)...".
IF SHIP:ALTITUDE > 100000 {
    WARPTO(TIME:SECONDS + ETA:PERIAPSIS - 180).
    WAIT UNTIL SHIP:ALTITUDE < 90000.
}

// --------------------------------------------------
// STEP 6: Service Module Jettison & Heat Shield Entry
// --------------------------------------------------
PRINT "==================================================".
PRINT "        PREPARING FOR ATMOSPHERIC ENTRY           ".
PRINT "==================================================".
PRINT "Decoupling service module...".
STAGE. // Drops fuel tank and engine
WAIT 2.

PRINT "Locking Heat Shield to Blunt Reentry Vector...".
SAS OFF.
LOCK STEERING TO -SHIP:VELOCITY:SURFACE.

PRINT "Entering atmosphere (70km) - Aerobraking initiated!".
WAIT UNTIL SHIP:ALTITUDE < 70000.

PRINT "Plasma ionization regime active. Holding retrograde...".
WAIT UNTIL SHIP:ALTITUDE < 5000 AND SHIP:VELOCITY:SURFACE:MAG < 300.

// --------------------------------------------------
// STEP 7: Parachute Deploy & Splashdown
// --------------------------------------------------
PRINT "Aerodynamic deceleration complete! Deploying parachutes!".
STAGE. // Deploys parachuteSingle
CHUTES ON.
UNLOCK STEERING.

PRINT "Descending safely under canopy...".
WAIT UNTIL SHIP:STATUS = "SPLASHED" OR SHIP:STATUS = "LANDED".

PRINT "==================================================".
PRINT "      RECOVERY COMPLETE: CRAFT IS HOME SAFE!      ".
PRINT "==================================================".
UNTIL FALSE {
    PRINT "{'status':'RECOVERED_SAFE_ON_KERBIN'} " AT (0, 24).
    WAIT 1.
}
