using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;
using System.ComponentModel.DataAnnotations;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterPushController : ControllerBase
{
    private readonly WebPushService _pushService;
    private readonly ILogger<RegisterPushController> _logger;

    public RegisterPushController(WebPushService pushService, ILogger<RegisterPushController> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    [HttpGet("vapidPublicKey")]
    public IActionResult GetVapidKey()
    {
        try
        {
            var key = _pushService.PublicKey;
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("VAPID public key is not configured");
                return StatusCode(500, new { error = "VAPID keys not configured" });
            }
            
            return Ok(new { key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VAPID public key");
            return StatusCode(500, new { error = "Failed to retrieve VAPID key" });
        }
    }

    [HttpPost]
    public IActionResult Register([FromBody] PushSubscription? sub)
    {
        try
        {
            if (sub == null)
            {
                _logger.LogWarning("Received null push subscription");
                return BadRequest(new { error = "Invalid subscription data" });
            }

            _pushService.AddSubscription(sub);
            _logger.LogInformation("Successfully registered push subscription");
            return Ok(new { success = true, message = "Subscription registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering push subscription");
            return StatusCode(500, new { error = "Failed to register subscription" });
        }
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            var count = _pushService.GetSubscriptionCount();
            return Ok(new { subscriptionCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription stats");
            return StatusCode(500, new { error = "Failed to retrieve stats" });
        }
    }
}
