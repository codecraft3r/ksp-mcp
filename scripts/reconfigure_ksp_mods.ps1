$kspDir = 'C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program'
$gameData = Join-Path $kspDir 'GameData'
$backupDir = Join-Path $kspDir 'GameData_Disabled'

if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
}

# Mods strictly needed for MCP
$keepList = @(
    'Squad',
    'SquadExpansion',
    'kOS',
    'XyphosAerospace',
    'ModuleManager.4.2.3.dll',
    'ModuleManager.Physics',
    'ModuleManager.TechTree'
)

Write-Host "Moving non-essential mods to GameData_Disabled..." -ForegroundColor Cyan

Get-ChildItem $gameData | ForEach-Object {
    if ($keepList -notcontains $_.Name) {
        Write-Host "  Disabling: $($_.Name)" -ForegroundColor DarkGray
        Move-Item -Path $_.FullName -Destination $backupDir -Force
    }
}

# Clean old module manager caches
Remove-Item -Path (Join-Path $gameData 'ModuleManager.ConfigCache') -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $gameData 'ModuleManager.ConfigSHA') -Force -ErrorAction SilentlyContinue

Write-Host "`nRemaining active GameData contents:" -ForegroundColor Green
Get-ChildItem $gameData | Select-Object Name
