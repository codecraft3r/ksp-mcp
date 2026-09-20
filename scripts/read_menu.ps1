$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
Start-Sleep -Milliseconds 600
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)
$text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
Write-Host "MENU OUTPUT:"
Write-Host $text
$t.Close()
