Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinInfo {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
}
"@

$procs = Get-Process -Name 'KSP_x64' -ErrorAction SilentlyContinue
if ($procs) {
    Write-Host "KSP PID: $($procs.Id)"
    # Find window by process ID
    Get-Process | Where-Object { $_.MainWindowTitle -like '*Kerbal*' } | ForEach-Object {
        $rect = New-Object WinInfo+RECT
        [WinInfo]::GetWindowRect($_.MainWindowHandle, [ref]$rect) | Out-Null
        Write-Host "Found Window: $($_.MainWindowTitle) Handle: $($_.MainWindowHandle)"
        Write-Host "Rect: Left=$($rect.Left), Top=$($rect.Top), Right=$($rect.Right), Bottom=$($rect.Bottom)"
    }
}
