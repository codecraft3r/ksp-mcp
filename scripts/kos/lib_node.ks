// lib_node.ks - Maneuver Node Execution
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
    IF thrustVal <= 0 SET thrustVal TO 60.
    LOCAL burnDuration IS (dV0 * SHIP:MASS) / thrustVal.

    PRINT "Node dV: " + ROUND(dV0, 1) + " m/s. Est Burn Time: " + ROUND(burnDuration, 1) + "s.".

    // Orient toward burn vector
    LOCK STEERING TO nd:DELTAV.
    PRINT "Aligning to maneuver vector...".
    WAIT 5.

    // Warp to node minus half burn time
    LOCAL burnStart IS nd:TIME - (burnDuration / 2).
    IF burnStart > TIME:SECONDS + 15 {
        WARPTO(burnStart - 10).
    }

    WAIT UNTIL TIME:SECONDS >= burnStart.

    PRINT "Executing burn!".
    WHEN STAGE:NUMBER > 2 AND MAXTHRUST = 0 THEN {
        STAGE.
        PRESERVE.
    }

    UNTIL VDOT(v0, nd:DELTAV) <= 0.5 OR nd:DELTAV:MAG < 0.2 {
        LOCAL throttleVal IS 1.0.
        IF nd:DELTAV:MAG < 10 {
            SET throttleVal TO MAX(0.05, nd:DELTAV:MAG / 10).
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
