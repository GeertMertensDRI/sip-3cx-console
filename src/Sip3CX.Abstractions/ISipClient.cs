namespace Sip3CX.Abstractions;

public interface ISipClient : IAsyncDisposable
{
    SipClientState State { get; }

    event EventHandler<RegistrationStateChangedEventArgs>? RegistrationStateChanged;
    event EventHandler<CallStateChangedEventArgs>?         CallStateChanged;
    event EventHandler<IncomingCallEventArgs>?             IncomingCall;

    Task RegisterAsync(SipCredentials credentials, CancellationToken ct = default);
    Task UnregisterAsync(CancellationToken ct = default);

    Task<SipCallResult> PlaceCallAsync(string target, string browserSdpOffer, CancellationToken ct = default);
    Task AcceptCallAsync(string callId, CancellationToken ct = default);
    Task HangUpAsync(string callId, CancellationToken ct = default);
}
