using System.Collections.Concurrent;
using System.Text.Json;
using Lib.Net.Http.WebPush;

namespace PwaWeb.Services;

public class WebPushService
{
    private readonly PushServiceClient _client;
    private readonly string _publicKey;
    private readonly string _privateKey;
    private readonly string _subject;
    private readonly ConcurrentBag<PushSubscription> _subscriptions = new();
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IHttpClientFactory httpClientFactory, string publicKey, string privateKey, string subject, ILogger<WebPushService> logger)
    {
        _publicKey = publicKey;
        _privateKey = privateKey;
        _subject = subject;
        _logger = logger;
        _client = new PushServiceClient(httpClientFactory.CreateClient("WebPush"));
    }

    public string PublicKey => _publicKey;

    public void AddSubscription(PushSubscription sub)
    {
        if (sub == null || string.IsNullOrEmpty(sub.Endpoint))
        {
            _logger.LogWarning("Attempted to add null or invalid subscription");
            return;
        }
        
        if (!_subscriptions.Any(s => s.Endpoint == sub.Endpoint))
        {
            _subscriptions.Add(sub);
            _logger.LogInformation("Added push subscription: {Endpoint}", sub.Endpoint);
        }
        else
        {
            _logger.LogDebug("Subscription already exists: {Endpoint}", sub.Endpoint);
        }
    }

    public async Task SendNotificationAsync(string title, string body)
    {
        var payload = JsonSerializer.Serialize(new { title, body });
        var successCount = 0;
        var failCount = 0;

        foreach (var sub in _subscriptions)
        {
            try
            {
                var message = new PushMessage(payload)
                {
                    Topic = "notifications",
                    Urgency = PushMessageUrgency.Normal
                };

                using var vapidAuth = new Lib.Net.Http.WebPush.Authentication.VapidAuthentication(_publicKey, _privateKey)
                {
                    Subject = _subject
                };

                await _client.RequestPushMessageDeliveryAsync(sub, message, vapidAuth);
                successCount++;
                _logger.LogDebug("Successfully sent notification to {Endpoint}", sub.Endpoint);
            }
            catch (PushServiceClientException ex)
            {
                failCount++;
                _logger.LogError(ex, "Push service error sending to {Endpoint}: {StatusCode}", 
                    sub.Endpoint, ex.StatusCode);
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogError(ex, "Failed to send notification to {Endpoint}", sub.Endpoint);
            }
        }

        _logger.LogInformation("Sent notifications: {Success} succeeded, {Failed} failed", 
            successCount, failCount);
    }

    public int GetSubscriptionCount() => _subscriptions.Count;
}
