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

$writer.WriteLine('SET TARGET TO "Kerbin".')
$writer.WriteLine('PRINT "KERBIN DIST: " + ROUND(TARGET:DISTANCE / 1000) + " km".')
$writer.WriteLine('PRINT "REL SPEED:   " + ROUND((SHIP:VELOCITY:ORBIT - TARGET:VELOCITY:ORBIT):MAG) + " m/s".')
$writer.WriteLine('PRINT "ETA PERI:    " + ROUND(ETA:PERIAPSIS / 86400, 2) + " days".')
$writer.WriteLine('PRINT "SHIP PERI:   " + ROUND(SHIP:PERIAPSIS / 1000000) + "M km".')
$writer.WriteLine('PRINT "KERBIN RAD:  " + ROUND(TARGET:ORBIT:SEMIMAJORAXIS / 1000000) + "M km".')
Start-Sleep -Milliseconds 800

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$t.Close()
