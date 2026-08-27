namespace Sip3CX.Abstractions;

public interface ISipCallManager : IAsyncDisposable
{
    IReadOnlyDictionary<string, ISipCall> ActiveCalls { get; }
    event EventHandler? CallsChanged;

    Task<ISipCall> DialAsync(string target, CancellationToken ct = default);
    Task HangUpAsync(string callId, CancellationToken ct = default);
    Task HangUpAllAsync(CancellationToken ct = default);
}
