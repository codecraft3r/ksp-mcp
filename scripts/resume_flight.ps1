$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 300
$buf = New-Object byte[] 8192
$read = $stream.Read($buf, 0, $buf.Length)

# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$read = $stream.Read($buf, 0, $buf.Length)

$writer.WriteLine("SWITCH TO 0.")
Start-Sleep -Milliseconds 300

Write-Host ">>> IGNITING FLIGHT COMPUTER: RUNPATH('0:/tylo_mission.ks'). <<<"
$writer.WriteLine('RUNPATH("0:/tylo_mission.ks").')
Start-Sleep -Milliseconds 1500

$socket.Close()
Write-Host "Flight computer running."
