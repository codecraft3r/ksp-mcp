$t = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$s = $t.GetStream()
$writer = New-Object System.IO.StreamWriter($s, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 300
$buf = New-Object byte[] 4096
$r = $s.Read($buf, 0, $buf.Length)

# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$r = $s.Read($buf, 0, $buf.Length)

Write-Host "Sending KUNIVERSE:REVERTTOLAUNCH()..."
$writer.WriteLine("KUNIVERSE:REVERTTOLAUNCH().")
Start-Sleep -Milliseconds 1500

$t.Close()
Write-Host "Revert command sent."
