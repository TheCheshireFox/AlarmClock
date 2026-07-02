namespace AlarmClock.Network;

public record ConnectionStatus(string Ssid, bool Connected);

public interface IWiFiManager
{
    Task<ConnectionStatus> GetConnectionStatusAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ListAccessPointsAsync(CancellationToken cancellationToken);
    
    // Every start of the app, the WiFiManager will try to connect to the last known WiFi network
    Task<bool> ConnectAsync(string ssid, string password, CancellationToken cancellationToken);
}