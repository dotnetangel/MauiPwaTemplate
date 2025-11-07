using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Configuration;
using static Fido2NetLib.Objects.COSE;

namespace PwaWeb.Services;

// Read more here: https://github.com/passwordless-lib/fido2-net-lib
public class WebAuthnService
{
    private readonly IFido2 _fido2;
    private readonly string _origin;
    private readonly string _serverDomain;
    private readonly string _serverName;

    // production: replace in-memory with persistent DB-backed store
    private readonly Dictionary<string, List<StoredCredential>> _store = new();

    public WebAuthnService(IFido2 fido2)
    {
        _fido2 = fido2;
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

    public async Task<AttestationVerificationSuccess> MakeCredentialAsync(AuthenticatorAttestationRawResponse attestationResponse, CredentialCreateOptions options)
    {
        var result = await _fido2.MakeNewCredentialAsync(attestationResponse, options, async (args) => true);
        var cred = new StoredCredential
        {
            Descriptor = result.Result.CredentialDescriptor,
            PublicKey = result.Result.AttestationResult.CredentialPublicKey,
            UserHandle = result.Result.User.Id,
            SignatureCounter = result.Result.AttestationResult.Counter
        };
        var userId = System.Text.Encoding.UTF8.GetString(result.Result.User.Id);
        lock (_store)
        {
            if (!_store.ContainsKey(userId)) _store[userId] = new List<StoredCredential>();
            _store[userId].Add(cred);
        }
        return result;
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
        var storedPubKeys = stored.Select(c => new StoredCredential
        {
            Descriptor = c.Descriptor,
            PublicKey = c.PublicKey,
            UserHandle = c.UserHandle,
            SignatureCounter = c.SignatureCounter
        }).ToList();

        IsUserHandleOwnerOfCredentialIdAsync callback = async (args) =>
        {
            return true;
        };

        var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            OriginalOptions = options,
            StoredPublicKey = stored[0].PublicKey,
            StoredSignatureCounter = stored[0].SignatureCounter,
            IsUserHandleOwnerOfCredentialIdCallback = callback
        });
        
        return result != null;
    }
}

public class StoredCredential
{
    public PublicKeyCredentialDescriptor Descriptor { get; set; } = null!;
    public byte[] PublicKey { get; set; } = null!;
    public byte[] UserHandle { get; set; } = null!;
    public uint SignatureCounter { get; set; }
}
