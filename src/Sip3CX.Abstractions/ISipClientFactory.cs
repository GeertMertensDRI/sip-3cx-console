namespace Sip3CX.Abstractions;

/// <summary>
/// Obsolete — no longer needed. ISipClient is registered directly as a singleton.
/// Kept as an empty placeholder to avoid breaking any external references during transition.
/// </summary>
[Obsolete("ISipClientFactory is no longer needed. Register ISipClient directly as a singleton.", error: false)]
public interface ISipClientFactory { }

