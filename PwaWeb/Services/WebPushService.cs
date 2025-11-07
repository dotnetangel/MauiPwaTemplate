using Lib.Net.Http.WebPush;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PwaWeb.Services;

public class WebPushService
{
    private readonly PushServiceClient _client;
    private readonly PushSubscriptionVapidDetails _vapidDetails;
    private readonly ConcurrentBag<PushSubscription> _subscriptions = new();

    public WebPushService(string publicKey, string privateKey)
    {
        _client = new PushServiceClient(new HttpClient());
        _vapidDetails = new PushSubscriptionVapidDetails
        {
            Subject = "mailto:admin@example.com",
            PublicKey = publicKey,
            PrivateKey = privateKey
        };
    }

    public string PublicKey => _vapidDetails.PublicKey ?? string.Empty;

    public void AddSubscription(PushSubscription sub)
    {
        if (sub == null || string.IsNullOrEmpty(sub.Endpoint)) return;
        if (!_subscriptions.Any(s => s.Endpoint == sub.Endpoint))
            _subscriptions.Add(sub);
    }

    public async Task SendNotificationAsync(string title, string body)
    {
        var payload = JsonSerializer.Serialize(new { title, body });
        foreach (var sub in _subscriptions)
        {
            try
            {
                await _client.RequestPushMessageDeliveryAsync(sub, payload, _vapidDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Push] Failed to send to {sub.Endpoint}: {ex.Message}");
            }
        }
    }
}
