using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Configuration;

namespace PwaWeb.Services;

public class WebAuthnService
{
    private readonly Fido2 _lib;
    private readonly string _origin;
    private readonly string _serverDomain;
    private readonly string _serverName;

    // production: replace in-memory with persistent DB-backed store
    private readonly Dictionary<string, List<StoredCredential>> _store = new();

    public WebAuthnService(IConfigurationSection config)
    {
        _serverDomain = config.GetValue<string>("ServerDomain") ?? "example.com";
        _serverName = config.GetValue<string>("ServerName") ?? "MauiPwaSample";
        _origin = config.GetValue<string>("Origin") ?? "https://example.com";

        var fidoCfg = new Fido2Configuration
        {
            ServerDomain = _serverDomain,
            ServerName = _serverName,
            Origin = _origin
        };
        _lib = new Fido2(fidoCfg);
    }

    public CredentialCreateOptions GenerateCredentialOptions(string username, string displayName, out byte[] userId)
    {
        userId = System.Text.Encoding.UTF8.GetBytes(username);
        var user = new Fido2User
        {
            DisplayName = displayName,
            Name = username,
            Id = userId
        };
        var pubKeyCredParams = new[] { new PubKeyCredParam(Algorithm.ES256) };
        var options = _lib.RequestNewCredential(user, pubKeyCredParams, authenticatorSelection: null, attestation: AttestationConveyancePreference.None);
        return options;
    }

    public async Task<Fido2NetLib.AttestationVerificationSuccess> MakeCredentialAsync(AuthenticatorAttestationRawResponse attestationResponse, CredentialCreateOptions options)
    {
        var result = await _lib.MakeNewCredentialAsync(attestationResponse, options, async (args) => true);
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
        var options = _lib.GetAssertionOptions(allowed, UserVerificationRequirement.Discouraged);
        return options;
    }

    public async Task<bool> MakeAssertionAsync(AuthenticatorAssertionRawResponse assertionResponse, AssertionOptions options, string username)
    {
        _store.TryGetValue(username, out var creds);
        var stored = creds ?? new List<StoredCredential>();
        var storedPubKeys = stored.Select(c => new StoredPublicKeyCredential
        {
            Descriptor = c.Descriptor,
            PublicKey = c.PublicKey,
            UserHandle = c.UserHandle,
            SignatureCounter = c.SignatureCounter
        }).ToList();

        var res = await _lib.MakeAssertionAsync(assertionResponse, options, storedPubKeys, async (args) => true);
        return res != null;
    }
}

public class StoredCredential
{
    public PublicKeyCredentialDescriptor Descriptor { get; set; } = null!;
    public byte[] PublicKey { get; set; } = null!;
    public byte[] UserHandle { get; set; } = null!;
    public uint SignatureCounter { get; set; }
}
