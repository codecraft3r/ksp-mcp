// lib_node.ks - Maneuver Node Execution with Aggressive Warp
@LAZYGLOBAL OFF.

GLOBAL FUNCTION ExecuteNextNode {
    IF NOT HASNODE {
        PRINT "No maneuver node found to execute.".
        RETURN.
    }

    LOCAL nd IS NEXTNODE.
    LOCAL dV0 IS nd:DELTAV:MAG.
    LOCAL v0 IS nd:DELTAV.

    // Estimate burn time: t = dV * m / F
    LOCAL thrustVal IS MAXTHRUST.
    IF thrustVal <= 0 SET thrustVal TO 250.
    LOCAL burnDuration IS (dV0 * SHIP:MASS) / thrustVal.

    PRINT "Node dV: " + ROUND(dV0, 1) + " m/s. Est Burn Time: " + ROUND(burnDuration, 1) + "s.".

    // Orient toward burn vector
    LOCK STEERING TO nd:DELTAV.
    PRINT "Aligning to maneuver vector...".
    WAIT 5.

    // Aggressive Warp to node minus half burn time
    LOCAL burnStart IS nd:TIME - (burnDuration / 2).
    IF burnStart > TIME:SECONDS + 25 {
        SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".
        WARPTO(burnStart - 10).
    }

    WAIT UNTIL TIME:SECONDS >= burnStart.
    SET KUNIVERSE:TIMEWARP:WARP TO 0.
    WAIT UNTIL KUNIVERSE:TIMEWARP:RATE = 1.

    PRINT "Executing burn!".

    UNTIL VDOT(v0, nd:DELTAV) <= 0.5 OR nd:DELTAV:MAG < 0.2 {
        // Stage monitor during burn
        IF THROTTLE > 0 AND MAXTHRUST > 0 AND STAGE:LIQUIDFUEL < 1 {
            IF STAGE:NUMBER > 4 { // Only drop stages 7/6 or 5/4, never touch lander ascent stage
                PRINT "Stage burnout during burn. Staging!".
                STAGE.
                WAIT 0.8.
                STAGE.
            }
        }

        LOCAL throttleVal IS 1.0.
        IF nd:DELTAV:MAG < 15 {
            SET throttleVal TO MAX(0.05, nd:DELTAV:MAG / 15).
        }
        LOCK THROTTLE TO throttleVal.
        WAIT 0.05.
    }

    LOCK THROTTLE TO 0.
    SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.
    UNLOCK STEERING.
    REMOVE nd.
    PRINT "Maneuver burn complete!".
}
