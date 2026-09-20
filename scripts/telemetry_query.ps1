$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 400
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)

# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$r = $s.Read($buf, 0, $buf.Length)

$writer.WriteLine('PRINT "UT: " + ROUND(TIME:SECONDS) + " | BODY: " + SHIP:BODY:NAME + " | ALT: " + ROUND(SHIP:ALTITUDE / 1000000) + "M km | ETA_APO: " + ROUND(ETA:APOAPSIS / 86400, 1) + " days | WARP: " + KUNIVERSE:TIMEWARP:WARP + " (" + ROUND(KUNIVERSE:TIMEWARP:RATE) + "x)".')
Start-Sleep -Milliseconds 600

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$t.Close()
