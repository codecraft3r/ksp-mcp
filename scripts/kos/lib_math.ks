// lib_math.ks - KSP kOS Astrodynamics and Math Functions
@LAZYGLOBAL OFF.

GLOBAL FUNCTION CircularOrbitSpeed {
    PARAMETER altMeters.
    LOCAL radiusVal IS BODY:RADIUS + altMeters.
    RETURN SQRT(BODY:MU / radiusVal).
}

GLOBAL FUNCTION Clamp {
    PARAMETER val, minVal, maxVal.
    IF val < minVal RETURN minVal.
    IF val > maxVal RETURN maxVal.
    RETURN val.
}
