using Microsoft.AspNetCore.Mvc;
using PwaWeb.Services;
using System.Security.Cryptography;

namespace PwaWeb.Controllers;

[ApiController]
public class WellKnownController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly RsaKeyService _rsaKeyService;

    public WellKnownController(IConfiguration configuration, RsaKeyService rsaKeyService)
    {
        _configuration = configuration;
        _rsaKeyService = rsaKeyService;
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
        // Get the RSA public key parameters
        var publicKey = _rsaKeyService.GetPublicKey();
        
        // Convert RSA parameters to Base64Url encoded strings for JWK format
        var exponent = Base64UrlEncode(publicKey.Exponent!);
        var modulus = Base64UrlEncode(publicKey.Modulus!);
        
        // Create a JWK (JSON Web Key) with the public key
        var jwk = new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            n = modulus,
            e = exponent
        };

        var jwks = new
        {
            keys = new[] { jwk }
        };

        return Ok(jwks);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input);
        output = output.Split('=')[0]; // Remove padding
        output = output.Replace('+', '-'); // Replace + with -
        output = output.Replace('/', '_'); // Replace / with _
        return output;
    }
}
