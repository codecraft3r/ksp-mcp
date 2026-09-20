// lib_warp.ks - Aggressive Time-Warp Autopilot Library
// Designed to minimize real-world mission duration to ~5 minutes IRL
@LAZYGLOBAL OFF.

GLOBAL FUNCTION SetMaxRailsWarp {
    PARAMETER warpIndex IS 7. // Rate 7 = 100,000x on rails
    IF SHIP:ALTITUDE > 70000 {
        SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
        IF KUNIVERSE:TIMEWARP:WARP < warpIndex {
            SET KUNIVERSE:TIMEWARP:WARP TO warpIndex.
        }
    }
}

GLOBAL FUNCTION DropWarpTo1x {
    SET KUNIVERSE:TIMEWARP:WARP TO 0.
    WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.
}

GLOBAL FUNCTION AggressiveWarpTo {
    PARAMETER targetUt, marginSec IS 15.
    LOCAL destTime IS targetUt - marginSec.
    IF destTime > TIME:SECONDS + 10 {
        WARPTO(destTime).
        WAIT UNTIL TIME:SECONDS >= destTime OR KUNIVERSE:TIMEWARP:RATE = 1.
        DropWarpTo1x().
    }
}

GLOBAL FUNCTION AggressiveWarpUntil {
    PARAMETER predicateFn, maxWarp IS 7.
    UNTIL predicateFn() {
        // If an SOI boundary is approaching in under 45s, drop warp to prevent physics glitch
        IF SHIP:ORBIT:HASNEXTPATCH AND ETA:TRANSITION < 45 {
            SET KUNIVERSE:TIMEWARP:WARP TO 1.
            WAIT UNTIL SHIP:ORBIT:HASNEXTPATCH = FALSE OR ETA:TRANSITION > 60.
        } ELSE IF KUNIVERSE:TIMEWARP:WARP < maxWarp {
            SetMaxRailsWarp(maxWarp).
        }
        WAIT 0.5.
    }
    DropWarpTo1x().
}
