using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace PwaWeb.Services;

/// <summary>
/// Service for managing RSA keys used for JWT signing.
/// Maintains a single RSA instance for the application lifetime.
/// </summary>
public class RsaKeyService : IDisposable
{
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _signingKey;
    private bool _disposed;

    public RsaKeyService()
    {
        _rsa = RSA.Create(2048);
        _signingKey = new RsaSecurityKey(_rsa);
    }

    public RsaSecurityKey SigningKey => _signingKey;

    public RSA Rsa => _rsa;

    /// <summary>
    /// Gets the RSA parameters for the public key.
    /// </summary>
    public RSAParameters GetPublicKey()
    {
        return _rsa.ExportParameters(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _rsa?.Dispose();
            }
            _disposed = true;
        }
    }
}
