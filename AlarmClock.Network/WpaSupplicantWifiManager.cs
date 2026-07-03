using Microsoft.Extensions.Logging;

namespace AlarmClock.Network;

public class WpaSupplicantWifiManager(ILogger<WpaSupplicantWifiManager> logger) : IWiFiManager
{
    private const string WpaSupplicantPath = "/var/run/wpa_supplicant";
    private static readonly TimeSpan _commandTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan _scanDelay = TimeSpan.FromSeconds(3);

    private readonly WpaSupplicantControl _control = new(WpaSupplicantPath, _commandTimeout, logger);

    public async Task<ConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken)
    {
        var status = await _control.GetStatusAsync(cancellationToken);
        var connected = status.TryGetValue("wpa_state", out var state) && state is "COMPLETED";

        return new ConnectionStatus(status.GetValueOrDefault("ssid", string.Empty), connected);
    }

    public async Task<IReadOnlyCollection<string>> ListAccessPointsAsync(CancellationToken cancellationToken)
    {
        var scanStatus = await _control.ScanAsync(cancellationToken);
        if (scanStatus != ScanStatus.Ok && scanStatus != ScanStatus.FailBusy)
            throw new Exception("Failed to scan access points");

        await Task.Delay(_scanDelay, cancellationToken);

        return (await _control.GetScanResultsAsync(cancellationToken))
            .Distinct()
            .Order()
            .ToList();
    }

    public async Task<bool> ConnectAsync(string ssid, string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ssid);

        var networkId = await _control.AddNetworkAsync(cancellationToken);

        try
        {
            await _control.SetNetworkAsync(networkId, ssid, password, cancellationToken);
            await _control.EnableNetworkAsync(networkId, cancellationToken);
            await _control.SelectNetworkAsync(networkId, cancellationToken);

            var connected = await WaitForConnectionAsync(ssid, cancellationToken);
            if (!connected)
            {
                logger.LogWarning("Failed to connect to {Ssid}", ssid);

                await _control.RemoveNetworkAsync(networkId, CancellationToken.None);
                return false;
            }

            await _control.SaveConfigAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to {Ssid}", ssid);

            await _control.RemoveNetworkAsync(networkId, CancellationToken.None);
            throw;
        }
    }

    private async Task<bool> WaitForConnectionAsync(string ssid, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_commandTimeout);

        try
        {
            while (!cts.IsCancellationRequested)
            {
                var status = await _control.GetStatusAsync(cts.Token);
                var connected = status.TryGetValue("wpa_state", out var state) && state is "COMPLETED";
                var connectedSsid = status.GetValueOrDefault("ssid", string.Empty);

                if (connected && string.Equals(connectedSsid, ssid))
                    return true;

                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return false;
    }
}
