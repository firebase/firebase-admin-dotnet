using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Class representing options for the AppCheck.VerifyToken method.
/// </summary>
public class VerifyAppCheckTokenOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use the replay protection feature, set this to true. The AppCheck.VerifyToken
    /// method will mark the token as consumed after verifying it.
    ///
    /// Tokens that are found to be already consumed will be marked as such in the response.
    ///
    /// Tokens are only considered to be consumed if it is sent to App Check backend by calling the
    /// AppCheck.VerifyToken method with this field set to true; other uses of the token
    /// do not consume it.
    ///
    /// This replay protection feature requires an additional network call to the App Check backend
    /// and forces your clients to obtain a fresh attestation from your chosen attestation providers.
    /// This can therefore negatively impact performance and can potentially deplete your attestation
    /// providers' quotas faster. We recommend that you use this feature only for protecting
    /// low volume, security critical, or expensive operations.
    /// </summary>
    public bool Consume { get; set; } = false;
}
