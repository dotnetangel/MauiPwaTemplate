using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;
using System.ComponentModel.DataAnnotations;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebPushController : ControllerBase
{
    private readonly WebPushService _pushService;
    private readonly ILogger<WebPushController> _logger;

    public WebPushController(WebPushService pushService, ILogger<WebPushController> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] PushMessage? msg)
    {
        try
        {
            if (msg == null || string.IsNullOrWhiteSpace(msg.Title))
            {
                _logger.LogWarning("Received invalid push message");
                return BadRequest(new { error = "Invalid message data. Title is required." });
            }

            await _pushService.SendNotificationAsync(msg.Title, msg.Body ?? string.Empty);
            _logger.LogInformation("Push notification sent: {Title}", msg.Title);
            return Ok(new { sent = true, message = "Notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification");
            return StatusCode(500, new { error = "Failed to send notification" });
        }
    }
}

public record PushMessage(
    [Required] string Title, 
    string? Body = null);
