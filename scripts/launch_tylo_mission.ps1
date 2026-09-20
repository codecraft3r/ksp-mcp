Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "     KSP AUTOPILOT: TYLO MASTER LANDER MISSION    " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Waiting for Tylo_Master_Lander rollout to the Launchpad..." -ForegroundColor Yellow
Write-Host "(Please enter VAB, load 'Tylo_Master_Lander', and click Launch)`n" -ForegroundColor DarkGray

$vesselReady = $false
$attempts = 0
$maxAttempts = 300 # 5 minutes

while (-not $vesselReady -and $attempts -lt $maxAttempts) {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
        $s = $tcp.GetStream()
        Start-Sleep -Milliseconds 500
        $buf = New-Object byte[] 4096
        if ($s.DataAvailable) {
            $readBytes = $s.Read($buf, 0, $buf.Length)
            $text = [System.Text.Encoding]::ASCII.GetString($buf, 0, $readBytes)
            if ($text.Contains("[1]") -or ($text.Contains("yes") -and -not $text.Contains("<NONE>"))) {
                $vesselReady = $true
                $tcp.Close()
                break
            }
        }
        $tcp.Close()
    } catch {
        # ignore
    }
    $attempts++
    Start-Sleep -Seconds 1
    if ($attempts % 5 -eq 0) {
        Write-Host "Still waiting for vessel on the pad ($attempts s)..." -ForegroundColor DarkGray
    }
}

if (-not $vesselReady) {
    Write-Host "`nTimeout waiting for vessel rollout. Ensure 'Tylo_Master_Lander' is launched to the pad." -ForegroundColor Red
    exit 1
}

Write-Host "`n[SUCCESS] Vessel detected on the Launchpad! Connecting to flight computer..." -ForegroundColor Green

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

# Read initial menu
$init = Read-TelnetOutput 800
Write-Host $init -ForegroundColor DarkGray

# 1. Select CPU 1
Write-Host "`n[AUTOPILOT] Selecting CPU 1 (Tylo_Master_Lander)..." -ForegroundColor Yellow
$writer.WriteLine("1")
$cpuOut = Read-TelnetOutput 800
Write-Host $cpuOut -ForegroundColor DarkGray

# 2. Switch to Archive (0:/)
Write-Host "[AUTOPILOT] Switching to Archive: SWITCH TO 0." -ForegroundColor Cyan
$writer.WriteLine("SWITCH TO 0.")
$swOut = Read-TelnetOutput 500
Write-Host $swOut -ForegroundColor DarkGray

# 3. Launch Tylo Mission
Write-Host "`n[AUTOPILOT] >>> IGNITING TYLO MISSION: RUNPATH(`"0:/tylo_mission.ks`"). <<<`n" -ForegroundColor Green
$writer.WriteLine('RUNPATH("0:/tylo_mission.ks").')

# 4. Stream mission logs in real-time
$startTime = [DateTime]::UtcNow
$maxSeconds = 900 # 15 minutes

$buf = New-Object byte[] 4096
$lineBuffer = ""

while (([DateTime]::UtcNow - $startTime).TotalSeconds -lt $maxSeconds) {
    if ($stream.DataAvailable) {
        $read = $stream.Read($buf, 0, $buf.Length)
        if ($read -gt 0) {
            $chunk = [System.Text.Encoding]::ASCII.GetString($buf, 0, $read)
            $chunk = $chunk -replace '\x1B\[[^@-~]*[@-~]', ''
            $lineBuffer += $chunk
            $lineBuffer = $lineBuffer -replace "`r`n", "`n" -replace "`r", "`n"
            
            while ($lineBuffer.Contains("`n")) {
                $idx = $lineBuffer.IndexOf("`n")
                $line = $lineBuffer.Substring(0, $idx).Trim()
                $lineBuffer = $lineBuffer.Substring($idx + 1)
                
                if ($line.Length -gt 0) {
                    if ($line.Contains("TOUCHDOWN") -or $line.Contains("ACCOMPLISHED") -or $line.Contains("SAFE") -or $line.Contains("COMPLETE")) {
                        Write-Host "[TYLO MISSION] $line" -ForegroundColor Green
                    } elseif ($line.Contains("PHASE") -or $line.Contains("T-0") -or $line.Contains("ENTERED") -or $line.Contains("Suicide Burn")) {
                        Write-Host "`n[TYLO MISSION] >>> $line <<<" -ForegroundColor Yellow
                    } elseif ($line.Contains("Error") -or $line.Contains("Exception")) {
                        Write-Host "[MISSION ERROR] $line" -ForegroundColor Red
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

Write-Host "`n[AUTOPILOT] Tylo mission monitoring complete." -ForegroundColor Cyan
$socket.Close()
