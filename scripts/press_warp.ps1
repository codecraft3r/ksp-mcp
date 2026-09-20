Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinUtil {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

Add-Type -AssemblyName System.Windows.Forms

$ksp = Get-Process -Name 'KSP_x64' -ErrorAction SilentlyContinue
if ($ksp) {
    [WinUtil]::SetForegroundWindow($ksp.MainWindowHandle)
    Start-Sleep -Milliseconds 200
    # Send '.' 7 times to reach max 100,000x rails warp
    for ($i = 0; $i -lt 7; $i++) {
        [System.Windows.Forms.SendKeys]::SendWait(".")
        Start-Sleep -Milliseconds 150
    }
    Write-Host "Sent 7 timewarp increase keys (.) to KSP window."
} else {
    Write-Host "KSP_x64 not found."
}
