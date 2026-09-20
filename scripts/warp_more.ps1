$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 600
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)

# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 600
$r = $s.Read($buf, 0, $buf.Length)

# Send Rails warp command
$writer.WriteLine('SET KUNIVERSE:TIMEWARP:MODE TO "RAILS".')
Start-Sleep -Milliseconds 300
$writer.WriteLine('SET KUNIVERSE:TIMEWARP:WARP TO 7.')
Start-Sleep -Milliseconds 300
$writer.WriteLine('PRINT "TIMEWARP BOOSTED: Rate=" + KUNIVERSE:TIMEWARP:RATE + "x, WarpIndex=" + KUNIVERSE:TIMEWARP:WARP + ", Body=" + SHIP:BODY:NAME + ", Alt=" + ROUND(SHIP:ALTITUDE) + "m".')
Start-Sleep -Milliseconds 1000

if ($s.DataAvailable) {
    $r = $s.Read($buf, 0, $buf.Length)
    $text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
    Write-Host $text
}

$t.Close()
