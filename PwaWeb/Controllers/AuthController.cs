using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PwaWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private static RSA? _rsa;
    private static RsaSecurityKey? _signingKey;

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Initialize RSA key for signing (reuse across requests)
        if (_rsa == null)
        {
            _rsa = RSA.Create(2048);
            _signingKey = new RsaSecurityKey(_rsa);
        }
    }

    [HttpPost("token")]
    public IActionResult CreateToken([FromForm] string? grant_type, [FromForm] string? client_id)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrEmpty(grant_type))
            {
                return BadRequest(new 
                { 
                    error = "invalid_request", 
                    error_description = "The grant_type parameter is missing." 
                });
            }

            // Support client_credentials grant type for a slim implementation
            if (grant_type != "client_credentials")
            {
                return BadRequest(new 
                { 
                    error = "unsupported_grant_type", 
                    error_description = "Only client_credentials grant type is supported." 
                });
            }

            var issuer = _configuration.GetValue<string>("OpenIddict:Issuer") ?? "http://localhost:5000";
            var tokenLifetime = _configuration.GetValue<int>("OpenIddict:TokenLifetime", 3600);
            
            // Create claims for the token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, client_id ?? "anonymous_client"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                // Add custom claims as requested: "said" and "subscriptionId"
                new Claim("said", Guid.NewGuid().ToString()),
                new Claim("subscriptionId", Guid.NewGuid().ToString())
            };

            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(tokenLifetime),
                Issuer = issuer,
                Audience = issuer,
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
            };

            // Generate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            _logger.LogInformation("JWT token created for client: {ClientId}", client_id ?? "anonymous_client");

            // Return token response in OAuth 2.0 format
            return Ok(new 
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = tokenLifetime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating JWT token");
            return StatusCode(500, new 
            { 
                error = "server_error", 
                error_description = "An error occurred while processing the token request." 
            });
        }
    }
}
