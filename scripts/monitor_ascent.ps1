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

$writer.WriteLine('PRINT "ALT: " + ROUND(SHIP:ALTITUDE) + "m | APO: " + ROUND(SHIP:APOAPSIS) + "m | STAGE: " + STAGE:NUMBER + " | FUEL: " + ROUND(STAGE:LIQUIDFUEL) + " | THRUST: " + ROUND(MAXTHRUST) + "kN".')
Start-Sleep -Milliseconds 800

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$t.Close()
