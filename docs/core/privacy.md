[Home](../../README.md) · [Documentation](../README.md) · [Core](./README.md)

# Privacy of logs and traces

Personal data must not sit readable in logs and traces, yet stay readable to the people allowed to see it. With privacy on, selected field values are **encrypted on their way out of the process**, and only the holder of the key can read them back.

## Turning it on

```json
"RequestPipeline": {
  "Privacy": {
    "Enabled": true,
    "ProtectedFields": [ "customer.email" ],
    "Keys": { "Current": "", "Previous": [] }
  }
}
```

| Key | Default | Meaning |
|---|---|---|
| `Enabled` | `false` | When true, protected fields are encrypted before they reach logs and trace tags. The request context itself always holds the real values. |
| `ProtectedFields` | `[]` | Extra field names to protect, added to the built-in ones. Matching ignores case. |
| `Keys:Current` | empty | Encrypts every new value. **32 random bytes, base64 encoded.** Set it through an environment variable, never commit it. Required when `Enabled` is true. |
| `Keys:Previous` | `[]` | Old keys, kept only to read tokens written before a rotation. Each must also be 32 base64 bytes. |

The settings validator rejects privacy enabled without a valid current key, and a previous key that is not 32 base64 bytes, naming the key (`Privacy:Keys:Current`).

## What is protected

By default: `client.address`, `network.peer.address`, `user_agent.original` and `enduser.id`, plus everything in `ProtectedFields`. They are protected in two places:

- **The log scope** that the request middleware opens, so every log line of the request carries the encrypted values.
- **The span tags**, rewritten when the span ends, before any exporter sees it (the ASP.NET Core instrumentation adds those tags with real values on its own, so the [telemetry package](../telemetry/README.md) re-protects them in a trace processor).

`RequestContext` keeps the real values for your code. Code that adds its own log fields or tags calls the protector itself:

```csharp
logger.LogInformation("Invoice sent to {Email}", privacy.Protect("customer.email", email));
```

## `IPrivacyProtector`

| Member | Does |
|---|---|
| `IsProtected(field)` | True when privacy is on and the field is protected. |
| `Protect(field, value)` | Returns the value encrypted when its field is protected; otherwise unchanged (null and empty stay as they are). |
| `Unprotect(token)` | Decrypts a token with the current key, then each previous key. Returns `null` for a malformed, tampered or unknown-key token. |

The implementation, `PrivacyProtector`, is registered as a singleton by the request pipeline.

## The token

A value becomes `base64url(nonce + ciphertext + tag)`: **AES-256-GCM with a random 12-byte nonce**. Consequences:

- The same value gives a **different token every time**, so tokens cannot be matched against each other (you cannot count requests per client from the tokens alone).
- A token says nothing about which key made it; decryption tries the current key and then each previous one, which is safe because a wrong key fails the authentication tag.
- A tampered token is rejected.

## Keys and rotation

Generate a key with `openssl rand -base64 32`, or with the script below. Keys come from the environment:

```
RequestPipeline__Privacy__Keys__Current=<key>
RequestPipeline__Privacy__Keys__Previous__0=<an old key>
```

To rotate: make the new key `Current` and move the old one to `Previous` for as long as you need to read its tokens. **Removing a key makes every token it made unreadable.**

## Reading and generating by hand

[`docs/scripts/privacy.cs`](../scripts/privacy.cs) needs only the .NET SDK. `PRIVACY_KEYS` is the current key and every previous one, each tried in turn:

```
dotnet run docs/scripts/privacy.cs -- generate-key
PRIVACY_KEYS=<current>,<previous> dotnet run docs/scripts/privacy.cs -- unprotect <token>
```
