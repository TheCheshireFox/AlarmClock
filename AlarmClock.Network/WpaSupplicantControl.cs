using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace AlarmClock.Network;

file sealed class LocalSocket(string path) : IDisposable
{
    public string Path { get; } = path;

    public static LocalSocket Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"alarmclock-wpa-{Guid.NewGuid():N}");
        TryDelete(path);
        return new LocalSocket(path);
    }
        
    public void Dispose() => TryDelete(Path);
        
    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // NOP
        }
    }
}

public enum ScanStatus
{
    Unknown,
    Ok,
    FailBusy
}

public class WpaSupplicantControl(string controlSocketDir = "/run/wpa_supplicant", TimeSpan? commandTimeout = null)
{
    private readonly TimeSpan _commandTimeout = commandTimeout ?? TimeSpan.MaxValue;
    private readonly string _controlSocket = GetControlSocketPath(controlSocketDir);

    private static string GetControlSocketPath(string controlSocketDir)
    {
        if (!Directory.Exists(controlSocketDir))
            throw new DirectoryNotFoundException($"wpa_supplicant control directory was not found: {controlSocketDir}");

        var controlSocket = Directory
            .EnumerateFiles(controlSocketDir)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return !string.IsNullOrWhiteSpace(name) &&
                       !name.StartsWith(".", StringComparison.Ordinal) &&
                       !string.Equals(name, "global", StringComparison.OrdinalIgnoreCase);
            })
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

        if (controlSocket is null)
            throw new FileNotFoundException($"No wpa_supplicant interface control sockets were found in {controlSocketDir}.");
        
        return controlSocket;
    }

    public async Task<Dictionary<string, string>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync("STATUS", cancellationToken);
        
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('='))
            .Where(x => x.Length == 2)
            .ToDictionary(x => x[0].Trim(), x => x[1].Trim());
    }

    public async Task<ScanStatus> ScanAsync(CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync("SCAN", cancellationToken);
        return response switch
        {
            "OK" => ScanStatus.Ok,
            "FAIL-BUSY" => ScanStatus.FailBusy,
            _ => ScanStatus.Unknown
        };   
    }

    public async Task<IReadOnlyCollection<string>> GetScanResultsAsync(CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync("SCAN_RESULTS", cancellationToken);
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(x => x.Split('\t', 5))
            .Where(x => x.Length == 5)
            .Select(x => Regex.Unescape(x[4]))
            .ToList();
    }

    public async Task<string> AddNetworkAsync(CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync("ADD_NETWORK", cancellationToken);
        return int.TryParse(response, out _)
            ? response
            : throw new Exception($"Failed to add network: {response}");
    }

    public async Task SetNetworkAsync(string networkId, string ssid, string password, CancellationToken cancellationToken)
    {
        var ssidHex = Convert.ToHexString(Encoding.UTF8.GetBytes(ssid)).ToLowerInvariant();
        
        await SendOkCommandAsync($"SET_NETWORK {networkId} ssid {ssidHex}", cancellationToken);
        
        if (string.IsNullOrWhiteSpace(password))
        {
            await SendOkCommandAsync($"SET_NETWORK {networkId} key_mgmt NONE", cancellationToken);
        }
        else
        {
            password = password
                .Replace("\\", @"\\")
                .Replace("\"", "\\\"");
            
            await SendOkCommandAsync($"SET_NETWORK {networkId} psk \"{password}\"", cancellationToken);
        }
    }
    
    public async Task EnableNetworkAsync(string networkId, CancellationToken cancellationToken)
    {
        await SendOkCommandAsync($"ENABLE_NETWORK {networkId}", cancellationToken);
    }
    
    public async Task SelectNetworkAsync(string networkId, CancellationToken cancellationToken)
    {
        await SendOkCommandAsync($"SELECT_NETWORK {networkId}", cancellationToken);
    }

    public async Task RemoveNetworkAsync(string networkId, CancellationToken cancellationToken)
    {
        await SendOkCommandAsync($"REMOVE_NETWORK {networkId}", cancellationToken);   
    }
    
    public async Task SaveConfigAsync(CancellationToken cancellationToken)
    {
        await SendOkCommandAsync("SAVE_CONFIG", cancellationToken);
    }
    
    private async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        using var localSocket = LocalSocket.Create();
        using var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_commandTimeout);
        
        socket.Bind(new UnixDomainSocketEndPoint(localSocket.Path));
        
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(_controlSocket), cts.Token);
        await socket.SendAsync(Encoding.UTF8.GetBytes(command), SocketFlags.None, cts.Token);
        return await ReceiveAsync(socket, cts.Token);
    }
    
    private async Task SendOkCommandAsync(string command, CancellationToken cancellationToken)
    {
        var response = await SendCommandAsync(command, cancellationToken);
        if (response is not "OK")
            throw new Exception($"'{command}' failed with response: {response}");
    }

    private static async Task<string> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
    {
        const int bufferSize = 128 * 1024;
        
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var received = await socket.ReceiveAsync(buffer, cancellationToken);
            return Encoding.UTF8.GetString(buffer[..received]).Trim();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}