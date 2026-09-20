$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 300
$buf = New-Object byte[] 8192
$read = $stream.Read($buf, 0, $buf.Length)

# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$read = $stream.Read($buf, 0, $buf.Length)

$writer.WriteLine('PRINT "ALT: " + ROUND(SHIP:ALTITUDE) + "m | APO: " + ROUND(SHIP:APOAPSIS) + "m | PERI: " + ROUND(SHIP:PERIAPSIS) + "m | FUEL: " + ROUND(STAGE:LIQUIDFUEL) + " LF | SPEED: " + ROUND(SHIP:VELOCITY:ORBIT:MAG) + " m/s".')
Start-Sleep -Milliseconds 800

$read = $stream.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $read) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "FLIGHT TELEMETRY:"
Write-Host $text

$socket.Close()
