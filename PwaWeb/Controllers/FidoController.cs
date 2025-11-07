using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FidoController : ControllerBase
{
    private readonly WebAuthnService _service;

    public FidoController(WebAuthnService service)
    {
        _service = service;
    }

    [HttpPost("register/options")]
    public IActionResult RegisterOptions([FromBody] RegisterRequest req)
    {
        var options = _service.GenerateCredentialOptions(req.Username, req.DisplayName, out var _);
        return Ok(options);
    }

    [HttpPost("register/complete")]
    public async Task<IActionResult> RegisterComplete([FromBody] AttestationResponseWrapper wrapper)
    {
        var result = await _service.MakeCredentialAsync(wrapper.Attestation, wrapper.Options);
        return Ok(new { success = true });
    }

    [HttpPost("login/options")]
    public IActionResult LoginOptions([FromBody] LoginRequest req)
    {
        var options = _service.GenerateAssertionOptions(req.Username);
        return Ok(options);
    }

    [HttpPost("login/complete")]
    public async Task<IActionResult> LoginComplete([FromBody] AssertionResponseWrapper wrapper)
    {
        var ok = await _service.MakeAssertionAsync(wrapper.Assertion, wrapper.Options, wrapper.Username);
        return Ok(new { success = ok });
    }
}

public record RegisterRequest(string Username, string DisplayName);
public record LoginRequest(string Username);
public record AttestationResponseWrapper(AuthenticatorAttestationRawResponse Attestation, CredentialCreateOptions Options);
public record AssertionResponseWrapper(AuthenticatorAssertionRawResponse Assertion, AssertionOptions Options, string Username);
