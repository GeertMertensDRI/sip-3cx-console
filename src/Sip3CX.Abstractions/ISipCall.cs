namespace Sip3CX.Abstractions;

public interface ISipCall : IAsyncDisposable
{
    string      CallId      { get; }
    SipCallState State      { get; }
    string      RemoteParty { get; }
    DateTimeOffset StartedAt { get; }

    Task HoldAsync(CancellationToken ct = default);
    Task ResumeAsync(CancellationToken ct = default);
    Task HangUpAsync(CancellationToken ct = default);
    Task SendDtmfAsync(char digit, CancellationToken ct = default);
    Task TransferAsync(string target, CancellationToken ct = default);
}
