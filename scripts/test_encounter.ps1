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

# Check Kerbin SOI patch with test node
$writer.WriteLine('SET TARGET TO "Kerbin".')
$writer.WriteLine('IF HASNODE REMOVE NEXTNODE.')
$writer.WriteLine('LOCAL n IS NODE(TIME:SECONDS + ETA:PERIAPSIS, 0, 0, -200).')
$writer.WriteLine('ADD n.')
Start-Sleep -Milliseconds 500
$writer.WriteLine('PRINT "HASPATCH: " + SHIP:ORBIT:HASNEXTPATCH.')
$writer.WriteLine('IF SHIP:ORBIT:HASNEXTPATCH PRINT "PATCH BODY: " + SHIP:ORBIT:NEXTPATCH:BODY:NAME.')
$writer.WriteLine('REMOVE n.')
Start-Sleep -Milliseconds 600

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$t.Close()
