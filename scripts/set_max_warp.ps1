$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 400
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)

$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$r = $s.Read($buf, 0, $buf.Length)

# Set Rails Warp to 7 (100,000x)
$writer.WriteLine('SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".')
Start-Sleep -Milliseconds 200
$writer.WriteLine('SET KUNIVERSE:TIMEWARP:WARP TO 7.')
Start-Sleep -Milliseconds 500

$writer.WriteLine('PRINT "WARP CONFIRMED: Rate=" + KUNIVERSE:TIMEWARP:RATE + "x | UT=" + ROUND(TIME:SECONDS) + " | ALT=" + ROUND(SHIP:ALTITUDE).')
Start-Sleep -Milliseconds 800

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$t.Close()
