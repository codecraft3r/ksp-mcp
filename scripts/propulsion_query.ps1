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

$writer.WriteLine('PRINT "FUEL: " + ROUND(STAGE:LIQUIDFUEL) + " | MASS: " + ROUND(SHIP:MASS, 1) + "t | MAXTHRUST: " + ROUND(MAXTHRUST) + "kN".')
Start-Sleep -Milliseconds 600

$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "Vessel Propulsion:"
Write-Host $text

$t.Close()
