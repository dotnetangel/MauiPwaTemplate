using System.Collections.Concurrent;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Configuration;
using static Fido2NetLib.Objects.COSE;

namespace PwaWeb.Services;

// Read more here: https://github.com/passwordless-lib/fido2-net-lib
public class WebAuthnService
{
    private readonly IFido2 _fido2;
    private readonly ILogger<WebAuthnService> _logger;

    // Static store shared across all instances (production: replace with persistent DB-backed store)
    private static readonly ConcurrentDictionary<string, List<StoredCredential>> _store = new();

    public WebAuthnService(IFido2 fido2, ILogger<WebAuthnService> logger)
    {
        _fido2 = fido2;
        _logger = logger;
    }

    public CredentialCreateOptions GenerateCredentialOptions(string username, string displayName, out byte[] userId)
    {
        // TODO: store test user here.
        userId = System.Text.Encoding.UTF8.GetBytes(username);
        var user = new Fido2User
        {
            DisplayName = displayName,
            Name = username,
            Id = userId
        };

        _store.TryGetValue(username, out var creds);
        var existingKeys = creds?.Select(c => c.Descriptor).ToList() ?? new List<PublicKeyCredentialDescriptor>();        
        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = existingKeys,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true  // Enable credential properties extension
            }
        });
        
        return options;
    }

    public async Task<bool> MakeCredentialAsync(AuthenticatorAttestationRawResponse attestationResponse, CredentialCreateOptions options)
    {
        try
        {
            IsCredentialIdUniqueToUserAsyncDelegate callback = (args, cancellationToken) => Task.FromResult(true);
            
            var makeParams = new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponse,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = callback
            };
            
            var result = await _fido2.MakeNewCredentialAsync(makeParams);
            
            if (result == null)
            {
                _logger.LogWarning("Credential creation returned null result");
                return false;
            }

            var cred = new StoredCredential
            {
                Descriptor = new PublicKeyCredentialDescriptor(result.Id),
                PublicKey = result.PublicKey,
                UserHandle = result.User.Id,
                SignatureCounter = result.SignCount
            };
            
            var userId = System.Text.Encoding.UTF8.GetString(result.User.Id);
            _store.AddOrUpdate(userId, 
                _ => new List<StoredCredential> { cred },
                (_, existing) => { existing.Add(cred); return existing; });
            
            _logger.LogInformation("Successfully created credential for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create credential");
            return false;
        }
    }

    public AssertionOptions GenerateAssertionOptions(string username)
    {
        _store.TryGetValue(username, out var creds);
        var allowed = creds?.Select(c => c.Descriptor).ToList() ?? new List<PublicKeyCredentialDescriptor>();
        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowed,
            UserVerification = UserVerificationRequirement.Discouraged,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true
            }
        });

        return options;
    }

    public async Task<bool> MakeAssertionAsync(AuthenticatorAssertionRawResponse assertionResponse, AssertionOptions options, string username)
    {
        _store.TryGetValue(username, out var creds);
        var stored = creds ?? new List<StoredCredential>();
        
        if (stored.Count == 0)
        {
            _logger.LogWarning("No credentials found for user {Username}", username);
            return false;
        }

        // Find the credential that matches the assertion
        // assertionResponse.Id is byte[], compare directly
        var matchingCred = stored.FirstOrDefault(c => 
            c.Descriptor.Id != null && assertionResponse.Id != null &&
            c.Descriptor.Id.Length == assertionResponse.Id.Length &&
            c.Descriptor.Id.Zip(assertionResponse.Id, (a, b) => a == b).All(x => x));
        
        if (matchingCred == null)
        {
            _logger.LogWarning("No matching credential found for assertion from user {Username}", username);
            // Fallback to first credential for backward compatibility
            matchingCred = stored[0];
        }

        IsUserHandleOwnerOfCredentialIdAsync callback = (args, cancellationToken) => Task.FromResult(true);

        try
        {
            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                OriginalOptions = options,
                StoredPublicKey = matchingCred.PublicKey,
                StoredSignatureCounter = matchingCred.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = callback
            });
            
            if (result != null)
            {
                _logger.LogInformation("Successfully authenticated user {Username}", username);
                return true;
            }
            
            _logger.LogWarning("Authentication failed for user {Username}", username);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}", username);
            return false;
        }
    }
}

public class StoredCredential
{
    public PublicKeyCredentialDescriptor Descriptor { get; set; } = null!;
    public byte[] PublicKey { get; set; } = null!;
    public byte[] UserHandle { get; set; } = null!;
    public uint SignatureCounter { get; set; }
}
