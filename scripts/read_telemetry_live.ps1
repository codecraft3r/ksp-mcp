$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 300
$buf = New-Object byte[] 4096
$read = $stream.Read($buf, 0, $buf.Length)

# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$read = $stream.Read($buf, 0, $buf.Length)

# In kOS, we can send a single-line print and immediately read whatever is in buffer
$writer.WriteLine('PRINT "==TELEMETRY: ALT=" + ROUND(SHIP:ALTITUDE) + " APO=" + ROUND(SHIP:APOAPSIS) + " FUEL=" + ROUND(STAGE:LIQUIDFUEL) + "==".')
Start-Sleep -Milliseconds 600

$sb = New-Object System.Text.StringBuilder
while ($stream.DataAvailable) {
    $read = $stream.Read($buf, 0, $buf.Length)
    if ($read -gt 0) {
        $sb.Append([System.Text.Encoding]::ASCII.GetString($buf, 0, $read)) | Out-Null
    }
    Start-Sleep -Milliseconds 50
}
$text = $sb.ToString() -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host $text

$socket.Close()
