using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Deathborn.Client.Net;

/// <summary>
/// HTTP auth + authoritative WebSocket world connection. The client only sends
/// input and interaction requests; the server decides outcomes.
/// </summary>
public sealed class GameClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _http = new();
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _wsCts;
    private readonly ConcurrentQueue<string> _incoming = new();
    private readonly ConcurrentQueue<Action> _mainThread = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private string _token = "";

    public long LocalCharacterId { get; private set; } = -1;
    public string SpawnName { get; private set; } = "";
    public float SpawnX { get; private set; }
    public float SpawnY { get; private set; }
    public bool WsConnected => _ws?.State == WebSocketState.Open;

    public event Action<string>? AuthFailed;
    public event Action? AuthSucceeded;
    public event Action? NeedCharacter;
    public event Action<WelcomeData>? Welcome;
    public event Action<List<PlayerState>>? Snapshot;
    public event Action<string>? ServerError;
    public event Action? Disconnected;

    public async Task RegisterAsync(string email, string password)
    {
        await AuthRequestAsync("/register", email, password);
    }

    public async Task LoginAsync(string email, string password)
    {
        await AuthRequestAsync("/login", email, password);
    }

    private async Task AuthRequestAsync(string path, string email, string password)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync(
                Config.HttpBase + path,
                new { email = email.Trim().ToLowerInvariant(), password },
                JsonOpts);

            var body = await resp.Content.ReadFromJsonAsync<TokenResponse>(JsonOpts);
            if (resp.IsSuccessStatusCode && body?.Token is { Length: > 0 } token)
            {
                _token = token;
                AuthSucceeded?.Invoke();
            }
            else
            {
                AuthFailed?.Invoke(body?.Error ?? $"Request failed ({(int)resp.StatusCode})");
            }
        }
        catch (Exception ex)
        {
            AuthFailed?.Invoke(ex.Message);
        }
    }

    public async Task ConnectWorldAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            AuthFailed?.Invoke("Not authenticated");
            return;
        }

        await DisconnectWorldAsync();
        LocalCharacterId = -1;

        _ws = new ClientWebSocket();
        _wsCts = new CancellationTokenSource();
        var url = $"{Config.WsBase}?token={Uri.EscapeDataString(_token)}";
        await _ws.ConnectAsync(new Uri(url), _wsCts.Token);
        _ = Task.Run(() => ReceiveLoopAsync(_ws, _wsCts.Token));
    }

    public async Task DisconnectWorldAsync()
    {
        if (_wsCts != null)
        {
            _wsCts.Cancel();
            _wsCts.Dispose();
            _wsCts = null;
        }
        if (_ws != null)
        {
            if (_ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch
                {
                    // ignore close errors
                }
            }
            _ws.Dispose();
            _ws = null;
        }
        while (_incoming.TryDequeue(out _)) { }
    }

    public void Poll()
    {
        while (_incoming.TryDequeue(out var raw))
            HandleMessage(raw);

        while (_mainThread.TryDequeue(out var action))
            action();
    }

    public void SendInput(float dirX, float dirY)
    {
        if (LocalCharacterId < 0) return;
        Send("input", new { dirX, dirY });
    }

    public void CreateCharacter(string name)
    {
        Send("create_character", new { name });
    }

    public void SendInteract(string targetId)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(targetId)) return;
        Send("interact", new { targetId });
    }

    private void Send(string type, object data)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var env = JsonSerializer.Serialize(new { type, data }, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(env);
        _ = SendRawAsync(bytes);
    }

    private async Task SendRawAsync(byte[] bytes)
    {
        var ws = _ws;
        if (ws?.State != WebSocketState.Open) return;
        await _sendLock.WaitAsync();
        try
        {
            if (ws.State == WebSocketState.Open)
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch
        {
            // receive loop reports disconnect
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();
        try
        {
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                sb.Clear();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        PostDisconnected();
                        return;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                _incoming.Enqueue(sb.ToString());
            }
        }
        catch (OperationCanceledException)
        {
            // expected on disconnect
        }
        catch
        {
            PostDisconnected();
        }
    }

    private void PostDisconnected() => _mainThread.Enqueue(() => Disconnected?.Invoke());

    private void HandleMessage(string raw)
    {
        Envelope? env;
        try
        {
            env = JsonSerializer.Deserialize<Envelope>(raw, JsonOpts);
        }
        catch
        {
            return;
        }
        if (env == null || string.IsNullOrEmpty(env.Type)) return;

        switch (env.Type)
        {
            case "welcome":
                var welcome = env.Data.Deserialize<WelcomeData>(JsonOpts);
                if (welcome == null) return;
                LocalCharacterId = welcome.CharacterId;
                SpawnX = (float)welcome.X;
                SpawnY = (float)welcome.Y;
                SpawnName = welcome.Name;
                Welcome?.Invoke(welcome);
                break;
            case "need_character":
                NeedCharacter?.Invoke();
                break;
            case "snapshot":
                var snap = env.Data.Deserialize<SnapshotData>(JsonOpts);
                Snapshot?.Invoke(snap?.Players ?? []);
                break;
            case "error":
                var err = env.Data.Deserialize<MessageData>(JsonOpts);
                ServerError?.Invoke(err?.Message ?? "error");
                break;
        }
    }

    public void Dispose()
    {
        DisconnectWorldAsync().GetAwaiter().GetResult();
        _sendLock.Dispose();
        _http.Dispose();
    }
}
