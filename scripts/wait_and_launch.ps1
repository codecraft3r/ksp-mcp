Write-Host "Waiting for kOS Telnet server to open on 127.0.0.1:5410 (roll vessel out to launchpad)..." -ForegroundColor Cyan

$connected = $false
$attempts = 0
$maxAttempts = 120 # 2 minutes

while (-not $connected -and $attempts -lt $maxAttempts) {
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $iar = $tcp.BeginConnect("127.0.0.1", 5410, $null, $null)
        $wait = $iar.AsyncWaitHandle.WaitOne(1000)
        if ($wait -and $tcp.Connected) {
            $tcp.EndConnect($iar)
            $tcp.Close()
            $connected = $true
            break
        }
        $tcp.Close()
    } catch {
        # ignore
    }
    $attempts++
    Start-Sleep -Seconds 1
    if ($attempts % 5 -eq 0) {
        Write-Host "Still waiting for launchpad rollout ($attempts s)..." -ForegroundColor DarkGray
    }
}

if (-not $connected) {
    Write-Host "Timeout waiting for kOS Telnet. Ensure vessel is on the pad and kOS Telnet is enabled." -ForegroundColor Yellow
    exit 1
}

Write-Host "`n[SUCCESS] kOS Telnet port 5410 is OPEN! Connecting to flight computer..." -ForegroundColor Green

# Connect to kOS Telnet
$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$reader = New-Object System.IO.StreamReader($stream)
$writer = New-Object System.IO.StreamWriter($stream)
$writer.AutoFlush = $true

Start-Sleep -Milliseconds 800

# Send commands
Write-Host "[AUTOPILOT] Switching to Archive (0:/)..." -ForegroundColor Cyan
$writer.WriteLine("SWITCH TO 0.")
Start-Sleep -Milliseconds 500

Write-Host "[AUTOPILOT] Initiating Mun Mission: RUNPATH(`"0:/mun_mission.ks`")..." -ForegroundColor Green
$writer.WriteLine('RUNPATH("0:/mun_mission.ks").')

# Read terminal stream for 45 seconds to monitor lift-off and initial ascent
$startTime = [DateTime]::UtcNow
while (([DateTime]::UtcNow - $startTime).TotalSeconds -lt 45) {
    if ($stream.DataAvailable) {
        $line = $reader.ReadLine()
        if ($line) {
            # Strip ANSI codes
            $clean = $line -replace '\x1B\[[^@-~]*[@-~]', ''
            if ($clean.Trim()) {
                Write-Host "[kOS] $clean" -ForegroundColor White
            }
        }
    } else {
        Start-Sleep -Milliseconds 100
    }
}

Write-Host "`n[AUTOPILOT] Flight stream active and mission underway!" -ForegroundColor Cyan
