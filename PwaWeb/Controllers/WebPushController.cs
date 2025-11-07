using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebPushController : ControllerBase
{
    private readonly WebPushService _pushService;

    public WebPushController(WebPushService pushService)
    {
        _pushService = pushService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] PushMessage msg)
    {
        await _pushService.SendNotificationAsync(msg.Title, msg.Body);
        return Ok(new { sent = true });
    }
}

public record PushMessage(string Title, string Body);
