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

# Send Ctrl-C (0x03)
Write-Host "Sending Ctrl-C to interrupt wait..."
$ctrlC = [char]3
$writer.Write($ctrlC)
Start-Sleep -Milliseconds 800

if ($s.DataAvailable) {
    $r = $s.Read($buf, 0, $buf.Length)
    $text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $r) -replace '\x1B\[[^@-~]*[@-~]', ''
    Write-Host "Response after Ctrl-C:"
    Write-Host $text
}

$t.Close()
