$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 300
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)

$writer.WriteLine("1")
Start-Sleep -Milliseconds 300
$r = $s.Read($buf, 0, $buf.Length)

$writer.WriteLine('PRINT "=== VESSEL REPORT ===".')
$writer.WriteLine('PRINT "STATUS:   " + SHIP:STATUS.')
$writer.WriteLine('PRINT "BODY:     " + SHIP:BODY:NAME.')
$writer.WriteLine('PRINT "ALTITUDE: " + ROUND(SHIP:ALTITUDE / 1000000) + " million km".')
$writer.WriteLine('PRINT "ORB SPD:  " + ROUND(SHIP:VELOCITY:ORBIT:MAG) + " m/s".')
$writer.WriteLine('PRINT "APOAPSIS: " + ROUND(SHIP:APOAPSIS / 1000000) + " million km (in " + ROUND(ETA:APOAPSIS / 86400, 1) + " days)".')
$writer.WriteLine('PRINT "PERIAPSIS:" + ROUND(SHIP:PERIAPSIS / 1000000) + " million km (in " + ROUND(ETA:PERIAPSIS / 86400, 1) + " days)".')
$writer.WriteLine('PRINT "FUEL REM: " + ROUND(STAGE:LIQUIDFUEL) + " LF".')
$writer.WriteLine('PRINT "MASS:     " + ROUND(SHIP:MASS, 2) + " t".')
$writer.WriteLine('PRINT "WARP:     " + KUNIVERSE:TIMEWARP:WARP + " (" + ROUND(KUNIVERSE:TIMEWARP:RATE) + "x)".')
$writer.WriteLine('PRINT "PATCH:    " + SHIP:ORBIT:HASNEXTPATCH.')
$writer.WriteLine('PRINT "=====================".')
Start-Sleep -Milliseconds 800

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$t.Close()
