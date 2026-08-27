# SIP 3CX Console — .NET 10

A small **.NET 10** console application that connects to a **3CX** PBX via SIP,
using [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) under the hood.

## Solution structure

```
sip-3cx-console/
├── src/
│   ├── Sip3CX.Abstractions/   ← Interfaces & models (no external deps)
│   ├── Sip3CX/                ← SIPSorcery implementation + DI extensions
│   └── Sip3CX.Console/        ← Interactive console entry-point
└── sip-3cx-console.sln
```

## Configuration

Edit `src/Sip3CX.Console/appsettings.json`:

```json
{
  "Sip": {
    "Username":  "100",
    "Password":  "your-extension-password",
    "Domain":    "your-3cx-server.com",
    "Port":      5060,
    "Transport": "Udp"
  }
}
```

| Field       | Description                                       |
|-------------|---------------------------------------------------|
| `Username`  | 3CX extension number (e.g. `100`)                |
| `Password`  | Extension SIP password (set in 3CX Management Console) |
| `Domain`    | Your 3CX hostname or IP                          |
| `Port`      | SIP port — default `5060` (UDP/TCP) or `5061` (TLS) |
| `Transport` | `Udp` \| `Tcp` \| `Tls` \| `WebSocket` \| `WebSocketSecure` |

## Run

```bash
cd src/Sip3CX.Console
dotnet run
```

## Available commands

| Command                    | Description                                |
|----------------------------|--------------------------------------------|
| `call sip:101@pbx.host`    | Place a call to extension 101              |
| `hangup <callId>`          | Hang up a specific call                    |
| `hangall`                  | Hang up all active calls                   |
| `hold <callId>`            | Put a call on hold                         |
| `resume <callId>`          | Resume a held call                         |
| `dtmf <digit>`             | Send DTMF on the first active call         |
| `transfer sip:102@pbx`     | Blind-transfer the first active call       |
| `list`                     | List all active calls with state & times   |
| `status`                   | Show SIP registration state                |
| `quit` / `exit`            | Unregister and exit                        |

## Design patterns used

- **Factory pattern** — `ISipClientFactory` / `SipClientFactory` creates the right
  `ISipClient` implementation based on the configured transport.
- **Dependency injection** — `ServiceCollectionExtensions.AddSip3CxServices()` wires
  everything up; swap implementations without touching the console app.
- **Interface segregation** — `ISipClient`, `ISipCall`, `ISipCallManager` are
  independent; mock any layer for unit testing.
- **Disposable resources** — both `ISipClient` and `ISipCallManager` implement
  `IAsyncDisposable` and clean up on exit.

## 3CX-specific notes

- 3CX uses standard SIP/UDP on port **5060** and SIP/TLS on **5061**.
- The extension password is found in *3CX Management Console → Users → SIP Credentials*.
- For remote/cloud 3CX, use **TLS** or **WebSocketSecure** transport and ensure
  the firewall allows the SIP port.
- Audio uses `RtpAVSession` with `AudioSourcesEnum.Microphone` — the machine's
  default microphone and speaker are used automatically by SIPSorcery.
