namespace Sip3CX.Abstractions;

public record SipCallResult(ISipCall Call, string RemoteSdp);

public record SipCredentials(
    string Username,
    string Password,
    string Domain,
    int Port = 5060,
    SipTransport Transport = SipTransport.Udp);

public enum SipTransport { Udp, Tcp, Tls, WebSocket, WebSocketSecure }

public enum SipClientState { Unregistered, Registering, Registered, Error }

public enum SipCallState { Idle, Initiating, Ringing, Connected, OnHold, Ended, Failed }

public sealed class CallStateChangedEventArgs(string callId, SipCallState state, string remoteParty) : EventArgs
{
    public string CallId      { get; } = callId;
    public SipCallState State { get; } = state;
    public string RemoteParty { get; } = remoteParty;
}

public sealed class RegistrationStateChangedEventArgs(SipClientState state, string? reason = null) : EventArgs
{
    public SipClientState State { get; } = state;
    public string? Reason       { get; } = reason;
}

public sealed class IncomingCallEventArgs(string callId, string callerUri) : EventArgs
{
    public string CallId    { get; } = callId;
    public string CallerUri { get; } = callerUri;
}
