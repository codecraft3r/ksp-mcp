$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

function Read-Stream {
    Start-Sleep -Milliseconds 400
    $buf = New-Object byte[] 4096
    $sb = New-Object System.Text.StringBuilder
    while ($s.DataAvailable) {
        $read = $s.Read($buf, 0, $buf.Length)
        if ($read -gt 0) {
            $sb.Append([System.Text.Encoding]::ASCII.GetString($buf, 0, $read)) | Out-Null
        }
        Start-Sleep -Milliseconds 50
    }
    return $sb.ToString() -replace '\x1B\[[^@-~]*[@-~]', ''
}

$menu = Read-Stream
$writer.WriteLine("1")
$cpu = Read-Stream

$writer.WriteLine('PRINT "STG_NUM: " + STAGE:NUMBER + " | AVAIL_THRUST: " + ROUND(AVAILABLETHRUST) + "kN | ALT: " + ROUND(SHIP:ALTITUDE) + "m | MASS: " + ROUND(SHIP:MASS, 1) + "t".')
$out = Read-Stream
Write-Host "STAGE INFO:"
Write-Host $out

$t.Close()
