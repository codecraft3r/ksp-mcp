$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
Start-Sleep -Milliseconds 600
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "--- kOS Menu ---"
Write-Host $text

# Select 1
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true
$writer.WriteLine("1")
Start-Sleep -Milliseconds 600

$r = $s.Read($buf, 0, $buf.Length)
$text2 = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "--- CPU Output ---"
Write-Host $text2

# Query altitude and orbit
$writer.WriteLine("PRINT SHIP:BODY:NAME + ' | Alt: ' + ROUND(SHIP:ALTITUDE) + ' | Apo: ' + ROUND(SHIP:APOAPSIS) + ' | Peri: ' + ROUND(SHIP:PERIAPSIS).")
Start-Sleep -Milliseconds 600
$r = $s.Read($buf, 0, $buf.Length)
$text3 = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "--- Vessel Telemetry ---"
Write-Host $text3

$t.Close()
