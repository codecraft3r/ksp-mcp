$socket = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5410)
$stream = $socket.GetStream()
$writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::ASCII)
$writer.AutoFlush = $true

function Read-Output([int]$waitMs = 500) {
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

$init = Read-Output 500
# Select CPU 1
$writer.WriteLine("1")
Start-Sleep -Milliseconds 400
$init2 = Read-Output 400

# Switch to archive
$writer.WriteLine("SWITCH TO 0.")
Start-Sleep -Milliseconds 300

# Run recovery script
Write-Host ">>> STARTING AUTONOMOUS KERBIN RECOVERY MISSION <<<" -ForegroundColor Green
$writer.WriteLine('RUNPATH("0:/kerbin_recovery.ks").')

$startTime = [DateTime]::UtcNow
$maxSeconds = 600 # 10 minutes max
$buf = New-Object byte[] 4096
$lineBuffer = ""

while (([DateTime]::UtcNow - $startTime).TotalSeconds -lt $maxSeconds) {
    if ($stream.DataAvailable) {
        $read = $stream.Read($buf, 0, $buf.Length)
        if ($read -gt 0) {
            $chunk = [System.Text.Encoding]::ASCII.GetString($buf, 0, $read) -replace '\x1B\[[^@-~]*[@-~]', ''
            $lineBuffer += $chunk
            
            while ($lineBuffer.Contains("`n")) {
                $idx = $lineBuffer.IndexOf("`n")
                $line = $lineBuffer.Substring(0, $idx).Trim("`r", "`n")
                $lineBuffer = $lineBuffer.Substring($idx + 1)
                
                if ($line.Trim().Length -gt 0) {
                    if ($line.Contains("RECOVERED") -or $line.Contains("HOME SAFE") -or $line.Contains("COMPLETE")) {
                        Write-Host "[RECOVERY] $line" -ForegroundColor Green
                    } elseif ($line.Contains("STEP") -or $line.Contains("PREPARING") -or $line.Contains("Fast-forwarding")) {
                        Write-Host "`n[RECOVERY] >>> $line <<<" -ForegroundColor Yellow
                    } elseif ($line.Contains("Error") -or $line.Contains("Exception")) {
                        Write-Host "[ERROR] $line" -ForegroundColor Red
                    } else {
                        Write-Host "[kOS] $line" -ForegroundColor Cyan
                    }
                }
            }
        }
    } else {
        Start-Sleep -Milliseconds 100
    }
}

$socket.Close()
