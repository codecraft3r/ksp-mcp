Write-Host "Connecting to kOS Telnet server on 127.0.0.1:5410..." -ForegroundColor Cyan

# Connect to kOS Telnet
$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$reader = New-Object System.IO.StreamReader($stream)
$writer = New-Object System.IO.StreamWriter($stream)
$writer.AutoFlush = $true

# Read initial menu
Start-Sleep -Milliseconds 600
while ($stream.DataAvailable) {
    $line = $reader.ReadLine()
    if ($line) {
        $clean = $line -replace '\x1B\[[^@-~]*[@-~]', ''
        Write-Host "[MENU] $clean" -ForegroundColor DarkGray
    }
}

# 1. Select CPU 1
Write-Host "`n[AUTOPILOT] Selecting CPU 1 (Mun_Explorer_I)..." -ForegroundColor Yellow
$writer.WriteLine("1")
Start-Sleep -Milliseconds 800

while ($stream.DataAvailable) {
    $line = $reader.ReadLine()
    if ($line) {
        $clean = $line -replace '\x1B\[[^@-~]*[@-~]', ''
        Write-Host "[kOS] $clean" -ForegroundColor White
    }
}

# 2. Switch to Archive (0:/)
Write-Host "[AUTOPILOT] Switching to Archive: SWITCH TO 0." -ForegroundColor Cyan
$writer.WriteLine("SWITCH TO 0.")
Start-Sleep -Milliseconds 500

# 3. Launch Mun Mission
Write-Host "[AUTOPILOT] Executing Mun Mission: RUNPATH(`"0:/mun_mission.ks`")..." -ForegroundColor Green
$writer.WriteLine('RUNPATH("0:/mun_mission.ks").')

# Read terminal stream for 180 seconds to monitor launch, gravity turn, and orbit
$startTime = [DateTime]::UtcNow
while (([DateTime]::UtcNow - $startTime).TotalSeconds -lt 180) {
    if ($stream.DataAvailable) {
        $line = $reader.ReadLine()
        if ($line) {
            $clean = $line -replace '\x1B\[[^@-~]*[@-~]', ''
            if ($clean.Trim()) {
                Write-Host "[MISSION] $clean" -ForegroundColor Green
            }
        }
    } else {
        Start-Sleep -Milliseconds 100
    }
}

Write-Host "`n[AUTOPILOT] Monitoring period ended." -ForegroundColor Cyan
