Write-Host "Connecting to kOS Telnet to initiate Mun Deorbit / Impact..." -ForegroundColor Red

$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

function Read-TelnetOutput([int]$waitMs = 500) {
    Start-Sleep -Milliseconds $waitMs
    $sb = New-Object System.Text.StringBuilder
    $buf = New-Object byte[] 4096
    while ($stream.DataAvailable) {
        $read = $stream.Read($buf, 0, $buf.Length)
        if ($read -gt 0) {
            $text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $read)
            $clean = $text -replace '\x1B\[[^@-~]*[@-~]', ''
            $sb.Append($clean) | Out-Null
        }
        Start-Sleep -Milliseconds 50
    }
    return $sb.ToString()
}

# 1. Attach to CPU 1
$init = Read-TelnetOutput 500
Write-Host "[kOS Menu] Selecting CPU 1..." -ForegroundColor Yellow
$writer.WriteLine("1")
Start-Sleep -Milliseconds 600

# 2. Send Ctrl-C to break the telemetry loop
Write-Host "[AUTOPILOT] Sending Ctrl-C to stop orbital telemetry..." -ForegroundColor Yellow
$ctrlC = [byte[]]@(0x03)
$stream.Write($ctrlC, 0, 1)
$stream.Flush()
Start-Sleep -Milliseconds 800

$out = Read-TelnetOutput 500
Write-Host $out -ForegroundColor DarkGray

# 3. Switch to archive and run mun_crash.ks
Write-Host "[AUTOPILOT] Switching to 0:/ and executing mun_crash.ks..." -ForegroundColor Red
$writer.WriteLine("SWITCH TO 0.")
Start-Sleep -Milliseconds 400
$writer.WriteLine('RUNPATH("0:/mun_crash.ks").')

# 4. Stream real-time impact logs
$startTime = [DateTime]::UtcNow
$maxSeconds = 300 # 5 minutes

$buf = New-Object byte[] 4096
$lineBuffer = ""

while (([DateTime]::UtcNow - $startTime).TotalSeconds -lt $maxSeconds) {
    if ($stream.DataAvailable) {
        $read = $stream.Read($buf, 0, $buf.Length)
        if ($read -gt 0) {
            $chunk = [System.Text.Encoding]::ASCII.GetString($buf, 0, $read)
            $chunk = $chunk -replace '\x1B\[[^@-~]*[@-~]', ''
            $lineBuffer += $chunk
            
            while ($lineBuffer.Contains("`n")) {
                $idx = $lineBuffer.IndexOf("`n")
                $line = $lineBuffer.Substring(0, $idx).Trim("`r", "`n")
                $lineBuffer = $lineBuffer.Substring($idx + 1)
                
                if ($line.Trim().Length -gt 0) {
                    if ($line.Contains("IMPACT") -or $line.Contains("TERMINAL") -or $line.Contains("collision")) {
                        Write-Host "[CRASH TELEMETRY] $line" -ForegroundColor Red
                    } elseif ($line.Contains("Deorbit") -or $line.Contains("Periapsis") -or $line.Contains("Altitude")) {
                        Write-Host "[DEORBIT] $line" -ForegroundColor Yellow
                    } else {
                        Write-Host "[kOS] $line" -ForegroundColor White
                    }
                }
            }
        }
    } else {
        Start-Sleep -Milliseconds 100
    }
}

Write-Host "`n[AUTOPILOT] Impact sequence complete." -ForegroundColor Red
$socket.Close()
