using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterPushController : ControllerBase
{
    private readonly WebPushService _pushService;

    public RegisterPushController(WebPushService pushService)
    {
        _pushService = pushService;
    }

    [HttpGet("vapidPublicKey")]
    public IActionResult GetVapidKey() => Ok(new { key = _pushService.PublicKey });

    [HttpPost]
    public IActionResult Register([FromBody] PushSubscription sub)
    {
        _pushService.AddSubscription(sub);
        Console.WriteLine($"[RegisterPush] Added subscription: {sub?.Endpoint}");
        return Ok(new { success = true });
    }
}
