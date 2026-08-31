namespace Sip3CX;

/// <summary>
/// Obsolete — <see cref="ISipClientFactory"/> and this implementation are no longer needed.
/// <see cref="SipSorceryClient"/> is registered directly as a singleton via AddSip3CxServices().
/// </summary>
[Obsolete("SipClientFactory is no longer needed. ISipClient is registered directly as a singleton.", error: false)]
public sealed class SipClientFactory { }
