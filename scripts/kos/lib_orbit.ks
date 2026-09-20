// lib_orbit.ks - Orbital Maneuver Planning
@LAZYGLOBAL OFF.

GLOBAL FUNCTION PlanCircularizationAtApoapsis {
    LOCAL rApo IS BODY:RADIUS + SHIP:APOAPSIS.
    LOCAL vApoCurrent IS SQRT(BODY:MU * (2 / rApo - 1 / SHIP:OBT:SEMIMAJORAXIS)).
    LOCAL vCirc IS SQRT(BODY:MU / rApo).
    LOCAL dV IS vCirc - vApoCurrent.

    LOCAL circNode IS NODE(TIME:SECONDS + ETA:APOAPSIS, 0, 0, dV).
    ADD circNode.
    PRINT "Circularization node created at Apoapsis: dV = " + ROUND(dV, 1) + " m/s.".
    RETURN circNode.
}
