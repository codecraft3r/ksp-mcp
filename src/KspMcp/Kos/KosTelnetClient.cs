using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrimS.Telnet;

namespace KspMcp.Kos;

public class VesselTelemetry
{
    public bool Connected { get; set; }
    public double Altitude { get; set; }
    public double Apoapsis { get; set; }
    public double Periapsis { get; set; }
    public double OrbitalSpeed { get; set; }
    public double Mass { get; set; }
    public string RawOutput { get; set; } = string.Empty;
}

public class KosTelnetClient : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger? _logger;
    private Client? _client;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly Regex AnsiRegex = new(@"\x1B\[[^@-~]*[@-~]", RegexOptions.Compiled);

    public KosTelnetClient(string host = "127.0.0.1", int port = 5410, ILogger? logger = null)
    {
        _host = host;
        _port = port;
        _logger = logger;
    }

    public bool IsConnected => _client != null && _client.IsConnected;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
                return true;

            _logger?.LogInformation("Connecting to kOS Telnet server at {Host}:{Port}...", _host, _port);
            var stream = new TcpByteStream(_host, _port);
            _client = new Client(stream, CancellationToken.None);
            
            // Allow initial banner and prompt to arrive
            await Task.Delay(500, cancellationToken);
            var initial = await _client.ReadAsync(TimeSpan.FromSeconds(1));
            _logger?.LogInformation("Connected to kOS Telnet. Banner: {Banner}", StripAnsi(initial).Trim());
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to connect to kOS Telnet at {Host}:{Port}: {Message}", _host, _port, ex.Message);
            _client?.Dispose();
            _client = null;
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ExecuteCommandAsync(string command, TimeSpan? timeout = null)
    {
        var waitTimeout = timeout ?? TimeSpan.FromSeconds(5);
        await _lock.WaitAsync();
        try
        {
            if (!IsConnected)
            {
                var connected = await ConnectAsyncInternal();
                if (!connected)
                {
                    return "Error: Unable to connect to kOS Telnet server. Ensure KSP is running and flight scene with kOS is loaded.";
                }
            }

            if (!command.TrimEnd().EndsWith('.'))
            {
                command += ".";
            }

            _logger?.LogDebug("Sending kOS command: {Command}", command);
            await _client!.WriteLineAsync(command);

            // Read output response
            var response = await _client.ReadAsync(waitTimeout);
            return StripAnsi(response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing command over kOS Telnet: {Command}", command);
            return $"Error: {ex.Message}";
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ReadTerminalAsync(TimeSpan? timeout = null)
    {
        await _lock.WaitAsync();
        try
        {
            if (!IsConnected)
            {
                return "Not connected to kOS Telnet.";
            }

            var raw = await _client!.ReadAsync(timeout ?? TimeSpan.FromMilliseconds(500));
            return StripAnsi(raw);
        }
        catch (Exception ex)
        {
            return $"Error reading terminal: {ex.Message}";
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<VesselTelemetry> GetTelemetryAsync(TimeSpan? timeout = null)
    {
        var tel = new VesselTelemetry();
        var cmd = "PRINT \"[TEL]:\" + ROUND(SHIP:ALTITUDE) + \",\" + ROUND(SHIP:APOAPSIS) + \",\" + ROUND(SHIP:PERIAPSIS) + \",\" + ROUND(SHIP:VELOCITY:ORBIT:MAG, 1) + \",\" + ROUND(SHIP:MASS, 2).";
        var output = await ExecuteCommandAsync(cmd, timeout ?? TimeSpan.FromSeconds(3));

        tel.RawOutput = output;
        if (output.Contains("[TEL]:"))
        {
            tel.Connected = true;
            try
            {
                var idx = output.IndexOf("[TEL]:", StringComparison.Ordinal) + 6;
                var endIdx = output.IndexOf('\n', idx);
                var line = endIdx >= 0 ? output.Substring(idx, endIdx - idx).Trim() : output.Substring(idx).Trim();
                var parts = line.Split(',', StringSplitOptions.TrimEntries);

                if (parts.Length >= 5)
                {
                    double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var alt);
                    double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var apo);
                    double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var peri);
                    double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed);
                    double.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mass);

                    tel.Altitude = alt;
                    tel.Apoapsis = apo;
                    tel.Periapsis = peri;
                    tel.OrbitalSpeed = speed;
                    tel.Mass = mass;
                }
            }
            catch
            {
                // Return best effort
            }
        }

        return tel;
    }

    private async Task<bool> ConnectAsyncInternal()
    {
        try
        {
            var stream = new TcpByteStream(_host, _port);
            _client = new Client(stream, CancellationToken.None);
            await Task.Delay(300);
            await _client.ReadAsync(TimeSpan.FromMilliseconds(500));
            return true;
        }
        catch
        {
            _client?.Dispose();
            _client = null;
            return false;
        }
    }

    public static string StripAnsi(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return AnsiRegex.Replace(input, string.Empty);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
        _lock.Dispose();
    }
}
