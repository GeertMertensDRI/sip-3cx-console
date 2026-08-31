namespace Sip3CX.Abstractions;

public interface ISipCallManager : IAsyncDisposable
{
    IReadOnlyDictionary<string, ISipCall> ActiveCalls { get; }
    event EventHandler? CallsChanged;

    Task<SipCallResult> DialAsync(string target, string browserSdpOffer, CancellationToken ct = default);
    Task HangUpAsync(string callId, CancellationToken ct = default);
    Task HangUpAllAsync(CancellationToken ct = default);
}
