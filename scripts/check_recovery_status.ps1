$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
Start-Sleep -Milliseconds 400
$buf = New-Object byte[] 8192
if ($s.DataAvailable) {
    $r = $s.Read($buf, 0, $buf.Length)
    $text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
    Write-Host "--- TELNET SCREEN ---"
    Write-Host $text
}
$t.Close()
