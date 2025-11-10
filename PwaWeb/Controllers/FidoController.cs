using Fido2NetLib;
using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;
using System.ComponentModel.DataAnnotations;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FidoController : ControllerBase
{
    private readonly WebAuthnService _service;
    private readonly ILogger<FidoController> _logger;

    public FidoController(WebAuthnService service, ILogger<FidoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("register/options")]
    public IActionResult RegisterOptions([FromBody] RegisterRequest? req)
    {
        try
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username))
            {
                _logger.LogWarning("Invalid register options request - missing username");
                return BadRequest(new { error = "Username is required" });
            }

            // Sanitize username for logging to prevent log injection
            var sanitizedUsername = System.Text.RegularExpressions.Regex.Replace(req.Username, @"[\r\n]", "");
            
            var options = _service.GenerateCredentialOptions(req.Username, req.DisplayName ?? req.Username, out var _);
            _logger.LogInformation("Generated credential options for user: {Username}", sanitizedUsername);
            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating credential options");
            return StatusCode(500, new { error = "Failed to generate credential options" });
        }
    }

    [HttpPost("register/complete")]
    public async Task<IActionResult> RegisterComplete([FromBody] AttestationResponseWrapper? wrapper)
    {
        try
        {
            if (wrapper == null || wrapper.Attestation == null || wrapper.Options == null)
            {
                _logger.LogWarning("Invalid attestation response");
                return BadRequest(new { error = "Invalid attestation data" });
            }

            var result = await _service.MakeCredentialAsync(wrapper.Attestation, wrapper.Options);
            
            if (result)
            {
                _logger.LogInformation("Successfully registered passkey");
                return Ok(new { success = true, message = "Passkey registered successfully" });
            }
            else
            {
                _logger.LogWarning("Failed to register passkey");
                return BadRequest(new { success = false, error = "Failed to register passkey" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey registration");
            return StatusCode(500, new { error = "Failed to complete registration" });
        }
    }

    [HttpPost("login/options")]
    public IActionResult LoginOptions([FromBody] LoginRequest? req)
    {
        try
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username))
            {
                _logger.LogWarning("Invalid login options request - missing username");
                return BadRequest(new { error = "Username is required" });
            }

            // Sanitize username for logging to prevent log injection
            var sanitizedUsername = System.Text.RegularExpressions.Regex.Replace(req.Username, @"[\r\n]", "");
            
            var options = _service.GenerateAssertionOptions(req.Username);
            _logger.LogInformation("Generated assertion options for user: {Username}", sanitizedUsername);
            return Ok(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating assertion options");
            return StatusCode(500, new { error = "Failed to generate login options" });
        }
    }

    [HttpPost("login/complete")]
    public async Task<IActionResult> LoginComplete([FromBody] AssertionResponseWrapper? wrapper)
    {
        try
        {
            // Validate all required fields are present and non-empty
            if (wrapper == null)
            {
                _logger.LogWarning("Invalid assertion response - wrapper is null");
                return BadRequest(new { error = "Invalid login data" });
            }
            
            if (wrapper.Assertion == null)
            {
                _logger.LogWarning("Invalid assertion response - assertion is null");
                return BadRequest(new { error = "Invalid login data" });
            }
            
            if (wrapper.Options == null)
            {
                _logger.LogWarning("Invalid assertion response - options is null");
                return BadRequest(new { error = "Invalid login data" });
            }
            
            if (string.IsNullOrWhiteSpace(wrapper.Username))
            {
                _logger.LogWarning("Invalid assertion response - username is missing");
                return BadRequest(new { error = "Invalid login data" });
            }

            // Sanitize username for logging to prevent log injection
            var sanitizedUsername = System.Text.RegularExpressions.Regex.Replace(wrapper.Username, @"[\r\n]", "");
            
            var ok = await _service.MakeAssertionAsync(wrapper.Assertion, wrapper.Options, wrapper.Username);
            
            if (ok)
            {
                _logger.LogInformation("Successful passkey login for user: {Username}", sanitizedUsername);
                return Ok(new { success = true, message = "Login successful" });
            }
            else
            {
                _logger.LogWarning("Failed passkey login for user: {Username}", sanitizedUsername);
                return Unauthorized(new { success = false, error = "Authentication failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing passkey login");
            return StatusCode(500, new { error = "Failed to complete login" });
        }
    }
}

public record RegisterRequest(
    [Required] string Username, 
    string? DisplayName = null);

public record LoginRequest(
    [Required] string Username);

public record AttestationResponseWrapper(
    [Required] AuthenticatorAttestationRawResponse Attestation, 
    [Required] CredentialCreateOptions Options);

public record AssertionResponseWrapper(
    [Required] AuthenticatorAssertionRawResponse Assertion, 
    [Required] AssertionOptions Options, 
    [Required] string Username);
