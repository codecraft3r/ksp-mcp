// lib_telemetry.ks - Real-time JSON Telemetry Reporter
@LAZYGLOBAL OFF.

GLOBAL FUNCTION PrintTelemetryJson {
    LOCAL apo IS ROUND(SHIP:APOAPSIS).
    LOCAL peri IS ROUND(SHIP:PERIAPSIS).
    LOCAL alt IS ROUND(SHIP:ALTITUDE).
    LOCAL speed IS ROUND(SHIP:VELOCITY:ORBIT:MAG, 1).
    LOCAL throt IS ROUND(THROTTLE, 2).

    PRINT "{""altitude"":" + alt + ", ""apoapsis"":" + apo + ", ""periapsis"":" + peri + ", ""orbital_speed"":" + speed + ", ""throttle"":" + throt + ", ""mass"":" + ROUND(SHIP:MASS, 2) + "}" AT(0, 0).
}
