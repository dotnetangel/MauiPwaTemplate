using Microsoft.AspNetCore.Mvc;

namespace PwaWeb.Controllers;

[ApiController]
public class WellKnownController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WellKnownController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet(".well-known/openid-configuration")]
    public IActionResult GetConfiguration()
    {
        var issuer = _configuration.GetValue<string>("OpenIddict:Issuer") ?? "http://localhost:5000";
        
        // Return minimal OpenID Connect configuration
        var config = new
        {
            issuer = issuer,
            token_endpoint = $"{issuer}/api/auth/token",
            jwks_uri = $"{issuer}/.well-known/jwks",
            grant_types_supported = new[] { "client_credentials" },
            response_types_supported = new[] { "token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            token_endpoint_auth_methods_supported = new[] { "none", "client_secret_post", "client_secret_basic" },
            claims_supported = new[] { "sub", "iss", "aud", "exp", "iat", "said", "subscriptionId" }
        };

        return Ok(config);
    }

    [HttpGet(".well-known/jwks")]
    public IActionResult GetJwks()
    {
        // Return empty JWKS for now - clients will validate tokens using the public key from the token itself
        // In a production scenario, you'd expose the public key here
        var jwks = new
        {
            keys = new object[] { }
        };

        return Ok(jwks);
    }
}
