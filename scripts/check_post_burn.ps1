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

$writer.WriteLine('UNLOCK STEERING.')
$writer.WriteLine('LOCK THROTTLE TO 0.')
$writer.WriteLine('PRINT "STATUS: " + SHIP:STATUS + " | BODY: " + SHIP:BODY:NAME.')
$writer.WriteLine('PRINT "LF REM: " + ROUND(STAGE:LIQUIDFUEL) + " | AVAIL_THRUST: " + ROUND(AVAILABLETHRUST) + "kN".')
$writer.WriteLine('PRINT "APO: " + ROUND(SHIP:APOAPSIS / 1000000) + "M km | PERI: " + ROUND(SHIP:PERIAPSIS / 1000000) + "M km".')
$writer.WriteLine('SET TARGET TO "Kerbin".')
$writer.WriteLine('PRINT "KERBIN DIST: " + ROUND(TARGET:DISTANCE / 1000000) + "M km".')
Start-Sleep -Milliseconds 800

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "Post-Burn Status:"
Write-Host $text

$t.Close()
