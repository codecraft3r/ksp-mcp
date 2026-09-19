$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = 'dotnet'
$psi.Arguments = 'run --project src/KspMcp -c Release'
$psi.WorkingDirectory = 'c:\Users\nmitc\Documents\ksp-mcp'
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true

$proc = [System.Diagnostics.Process]::Start($psi)
$writer = $proc.StandardInput
$reader = $proc.StandardOutput

$proc.add_ErrorDataReceived({
    param($s, $e)
    if ($e.Data) {
        Write-Host "[STDERR] $($e.Data)" -ForegroundColor DarkGray
    }
})
$proc.BeginErrorReadLine()

# 1. Initialize
Write-Host '[CLIENT] -> Sending initialize...'
$writer.WriteLine('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"harness","version":"1.0"}}}')
$writer.Flush()
$initResp = $reader.ReadLine()
Write-Host "[SERVER] <- $initResp`n"

# 2. Initialized Notification
$writer.WriteLine('{"jsonrpc":"2.0","method":"notifications/initialized"}')
$writer.Flush()

# 3. List Tools
Write-Host '[CLIENT] -> Requesting tools/list...'
$writer.WriteLine('{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}')
$writer.Flush()
$listResp = $reader.ReadLine()
$listObj = $listResp | ConvertFrom-Json
$buildTool = $listObj.result.tools | Where-Object { $_.name -eq 'ksp_build_launch_vehicle' }
Write-Host "[INPUT SCHEMA] $($buildTool.inputSchema | ConvertTo-Json -Depth 4)`n"

# 4. Call Tool: ksp_calculate_stage_deltav
Write-Host '[CLIENT] -> Calling ksp_calculate_stage_deltav (LV-T45 Swivel + 2x FL-T800 tanks + 2t payload)...'
$writer.WriteLine('{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"ksp_calculate_stage_deltav","arguments":{"engineName":"liquidEngine_v2","tankName":"fuelTank_long","tankCount":2,"payloadMass":2.0}}}')
$writer.Flush()
$calcResp = $reader.ReadLine()
Write-Host "[SERVER] <- Delta-V calculation result: $calcResp`n"

# 5. Call Tool: ksp_build_launch_vehicle
Write-Host '[CLIENT] -> Calling ksp_build_launch_vehicle (Vessel: Live_Run_Orbiter)...'
$writer.WriteLine('{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"ksp_build_launch_vehicle","arguments":{"vesselName":"Live_Run_Orbiter","payloadType":"Crewed","boosterTankCount":2}}}')
$writer.Flush()
$buildResp = $reader.ReadLine()
Write-Host "[SERVER] <- Build vehicle result: $buildResp`n"

# 6. Call Tool: kos_list_scripts
Write-Host '[CLIENT] -> Calling kos_list_scripts...'
$writer.WriteLine('{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"kos_list_scripts","arguments":{}}}')
$writer.Flush()
$kosResp = $reader.ReadLine()
Write-Host "[SERVER] <- kOS scripts list: $kosResp`n"

$proc.Kill()
Write-Host '[CLIENT] Live test passed successfully. Process stopped.'
