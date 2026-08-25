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

    private readonly HttpClient _http = CreateHttpClient();
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _wsCts;
    private readonly ConcurrentQueue<string> _incoming = new();
    private readonly ConcurrentQueue<Action> _mainThread = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private string _token = "";

    public long LocalCharacterId { get; private set; } = -1;
    public string SpawnName { get; private set; } = "";
    public string SpawnRace { get; private set; } = "human";
    public CharacterAppearanceData SpawnAppearance { get; private set; } = new();
    public float SpawnX { get; private set; }
    public float SpawnY { get; private set; }
    public Dictionary<string, long> SpawnSkills { get; private set; } = new();
    public long SpawnTotalXp { get; private set; }
    public List<InventoryItemState> SpawnInventory { get; private set; } = [];
    public float SpawnHp { get; private set; }
    public float SpawnHpMax { get; private set; }
    public float SpawnStamina { get; private set; } = 100f;
    public float SpawnMana { get; private set; } = 100f;
    public bool WsConnected => _ws?.State == WebSocketState.Open;

    /// <summary>Human-readable reason from the server or the network stack.</summary>
    public string DisconnectMessage { get; private set; } = "";

    public event Action<string>? AuthFailed;
    public event Action? AuthSucceeded;
    public event Action? NeedCharacter;
    public event Action<WelcomeData>? Welcome;
    public event Action<SnapshotData>? Snapshot;
    public event Action<ProjectileSpawnData>? ProjectileSpawned;
    public event Action<SpellEffectSpawnData>? SpellEffectSpawned;
    public event Action<PlayerActionData>? PlayerAction;
    public event Action<ChatMessageData>? ChatMessage;
    public event Action<ChatTypingData>? ChatTyping;
    public event Action<PlayerHitData>? PlayerHit;
    public event Action<PlayerHealData>? PlayerHeal;
    public event Action<PlayerBuffData>? PlayerBuff;
    public event Action<PlayerDeathData>? PlayerDeath;
    public event Action<YouDiedData>? YouDied;
    public event Action<SkillXpGainData>? SkillXpGain;
    public event Action<NpcHitData>? NpcHit;
    public event Action<WorldEventData>? WorldEvent;
    public event Action<BossSpawnData>? BossSpawn;
    public event Action<BossDeathData>? BossDeath;
    public event Action<BossActionData>? BossAction;
    public event Action<HouseBuiltData>? HouseBuilt;
    public event Action<HouseRemovedData>? HouseRemoved;
    public event Action<HouseUpdatedData>? HouseUpdated;
    public event Action<InventoryData>? InventoryUpdated;
    public event Action<FishResultData>? FishResult;
    public event Action<CookResultData>? CookResult;
    public event Action<WorldItemRemovedData>? WorldItemRemoved;
    public event Action<WorldItemDropState>? WorldItemAdded;
    public event Action<string>? ServerError;
    public event Action? Disconnected;
    public event Action<FriendsData>? FriendsUpdated;
    public event Action<PmData>? PrivateMessage;

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add(ProtocolCompat.HttpHeader, ProtocolCompat.Protocol.ToString());
        return http;
    }

    public async Task<ServerVersionInfo?> FetchServerVersionAsync()
    {
        try
        {
            var resp = await _http.GetAsync(Config.HttpBase + "/version");
            if (!resp.IsSuccessStatusCode)
                return null;
            return await resp.Content.ReadFromJsonAsync<ServerVersionInfo>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> EnsureCompatibleAsync()
    {
        var server = await FetchServerVersionAsync();
        if (server == null)
        {
            AuthFailed?.Invoke("Could not reach server version endpoint.");
            return false;
        }

        var (ok, message) = ProtocolCompat.CheckAgainstServer(server);
        if (!ok)
        {
            AuthFailed?.Invoke(message);
            return false;
        }

        return true;
    }

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
            if (!await EnsureCompatibleAsync())
                return;

            var resp = await _http.PostAsJsonAsync(
                Config.HttpBase + path,
                new { email = email.Trim().ToLowerInvariant(), password },
                JsonOpts);

            if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var mismatch = await resp.Content.ReadFromJsonAsync<VersionMismatchResponse>(JsonOpts);
                AuthFailed?.Invoke(mismatch != null
                    ? ProtocolCompat.FormatMismatch(mismatch)
                    : "Incompatible client or server version.");
                return;
            }

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

        if (!await EnsureCompatibleAsync())
            return;

        await DisconnectWorldAsync();
        LocalCharacterId = -1;
        DisconnectMessage = "";

        _ws = new ClientWebSocket();
        _ws.Options.SetRequestHeader(ProtocolCompat.HttpHeader, ProtocolCompat.Protocol.ToString());
        _wsCts = new CancellationTokenSource();
        var url =
            $"{Config.WsBase}?token={Uri.EscapeDataString(_token)}&protocol={ProtocolCompat.Protocol}";
        try
        {
            await _ws.ConnectAsync(new Uri(url), _wsCts.Token);
        }
        catch (Exception ex)
        {
            AuthFailed?.Invoke($"Could not connect to world server: {ex.Message}");
            await DisconnectWorldAsync();
            Disconnected?.Invoke();
            return;
        }

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

    public void ClearWorldSession()
    {
        LocalCharacterId = -1;
        SpawnName = "";
        SpawnRace = "human";
        SpawnAppearance = new CharacterAppearanceData();
        SpawnX = 0;
        SpawnY = 0;
        SpawnSkills = new Dictionary<string, long>();
        SpawnTotalXp = 0;
        SpawnInventory = [];
        SpawnHp = 0;
        SpawnHpMax = 0;
        SpawnStamina = 100f;
        SpawnMana = 100f;
        DisconnectMessage = "";
    }

    public void Poll()
    {
        while (_incoming.TryDequeue(out var raw))
            HandleMessage(raw);

        while (_mainThread.TryDequeue(out var action))
            action();
    }

    public void SendInput(float dirX, float dirY, bool running = false, float? stamina = null, float? mana = null)
    {
        if (LocalCharacterId < 0) return;
        if (stamina.HasValue || mana.HasValue)
            Send("input", new { dirX, dirY, running, stamina, mana });
        else
            Send("input", new { dirX, dirY, running });
    }

    public void CreateCharacter(string name, string race, CharacterAppearanceData appearance)
    {
        Send("create_character", new { name, race, appearance });
    }

    public void SendInteract(string targetId)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(targetId)) return;
        Send("interact", new { targetId });
    }

    public void SendCastFireball(float dirX, float dirY) => SendCastSpell("fireball", dirX, dirY);

    public void SendCastSpell(string spellId, float dirX, float dirY)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(spellId)) return;
        Send("cast_spell", new { spellId, dirX, dirY });
    }

    public void SendPlayerAction(string action, float dirX = 0, float dirY = 0, string? targetId = null)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(action)) return;
        Send("player_action", new { action, dirX, dirY, targetId });
    }

    public void SendChatMessage(string text)
    {
        if (LocalCharacterId < 0) return;
        text = text.Trim();
        if (text.Length == 0) return;
        if (text.Length > Config.ChatMaxLength)
            text = text[..Config.ChatMaxLength];
        Send("chat_message", new { text });
    }

    public void SendChatTyping(bool typing)
    {
        if (LocalCharacterId < 0) return;
        Send("chat_typing", new { typing });
    }

    public void SendAbilityHitNpc(long targetNpcId, int damage, string ability)
    {
        if (targetNpcId >= 0) return;
        Send("ability_hit_npc", new { targetNpcId, damage, ability });
    }

    public void SendAbilityHit(long targetId, int damage, string ability)
    {
        if (LocalCharacterId < 0 || targetId < 0 || string.IsNullOrEmpty(ability)) return;
        Send("ability_hit", new { targetId, damage, ability });
    }

    public void SendAbilityUse(string ability)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(ability)) return;
        Send("ability_use", new { ability });
    }

    public void SendBuildHouse(float? x = null, float? y = null)
    {
        if (LocalCharacterId < 0) return;
        Send("build_house", x.HasValue || y.HasValue ? new { x, y } : new { });
    }

    public void SendDestroyHouse()
    {
        if (LocalCharacterId < 0) return;
        Send("destroy_house", new { });
    }

    public void SendPlaceFurniture(string type, float x, float y)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(type)) return;
        Send("place_furniture", new { type, x, y });
    }

    public void SendFarmAction(string action, int tileX = 0, int tileY = 0, string? itemId = null, int slot = -1, long animalId = 0)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(action)) return;
        Send("farm_action", new { action, tileX, tileY, itemId, slot, animalId });
    }

    public void SendFishAction(string action, int tileX, int tileY)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(action)) return;
        Send("fish_action", new { action, tileX, tileY });
    }

    public void SendCookAction(string itemId, int slot)
    {
        if (LocalCharacterId < 0 || string.IsNullOrEmpty(itemId)) return;
        Send("cook_action", new { itemId, slot });
    }

    public void SendPickupItem(long dropId)
    {
        if (LocalCharacterId < 0 || dropId <= 0) return;
        Send("pickup_item", new { dropId });
    }

    public void SendHouseEnter(long houseId)
    {
        if (LocalCharacterId < 0 || houseId <= 0) return;
        Send("house_enter", new { houseId });
    }

    public void SendHouseExit()
    {
        if (LocalCharacterId < 0) return;
        Send("house_exit", new { });
    }

    public void SendInventoryMove(int fromSlot, int toSlot)
    {
        if (LocalCharacterId < 0) return;
        Send("inventory_move", new { fromSlot, toSlot });
    }

    public void SendDropItem(int slot, float x, float y)
    {
        if (LocalCharacterId < 0 || slot < 0) return;
        Send("drop_item", new { slot, x, y });
    }

    public void SendEquipCosmetic(int slot)
    {
        if (LocalCharacterId < 0 || slot < 0) return;
        Send("equip_cosmetic", new { slot });
    }

    public void SendFriendAdd(long targetCharacterId)
    {
        if (LocalCharacterId < 0 || targetCharacterId <= 0) return;
        Send("friend_add", new { targetCharacterId });
    }

    public void SendFriendRespond(long fromAccountId, bool accept)
    {
        if (LocalCharacterId < 0 || fromAccountId <= 0) return;
        Send("friend_respond", new { fromAccountId, accept });
    }

    public void SendFriendRemove(long friendAccountId)
    {
        if (LocalCharacterId < 0 || friendAccountId <= 0) return;
        Send("friend_remove", new { friendAccountId });
    }

    public void SendPm(long targetCharacterId, string text)
    {
        if (LocalCharacterId < 0 || targetCharacterId <= 0) return;
        text = text.Trim();
        if (text.Length == 0) return;
        Send("pm_send", new { targetCharacterId, text });
    }

    /// <summary>Saves position and vitals on the server, then closes the world connection.</summary>
    public async Task LogoutWorldAsync(float? stamina = null, float? mana = null)
    {
        if (LocalCharacterId >= 0 && WsConnected)
        {
            if (stamina.HasValue || mana.HasValue)
                Send("logout", new { stamina, mana });
            else
                Send("logout", new { });
        }
        await Task.Delay(40);
        await DisconnectWorldAsync();
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

    private void PostDisconnected()
    {
        _mainThread.Enqueue(() =>
        {
            if (string.IsNullOrWhiteSpace(DisconnectMessage))
                DisconnectMessage = "Connection to the server was lost.";
            Disconnected?.Invoke();
        });
    }

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
                SpawnRace = welcome.Race;
                SpawnAppearance = welcome.Appearance ?? new CharacterAppearanceData();
                SpawnSkills = welcome.Skills ?? new Dictionary<string, long>();
                SpawnTotalXp = welcome.TotalXp;
                SpawnInventory = welcome.Inventory ?? [];
                SpawnHp = (float)welcome.Hp;
                SpawnHpMax = (float)welcome.HpMax;
                if (welcome.HpMax > 0)
                {
                    SpawnStamina = (float)welcome.Stamina;
                    SpawnMana = (float)welcome.Mana;
                }
                else
                {
                    SpawnStamina = 100f;
                    SpawnMana = 100f;
                }
                Welcome?.Invoke(welcome);
                break;
            case "need_character":
                NeedCharacter?.Invoke();
                break;
            case "snapshot":
                var snap = env.Data.Deserialize<SnapshotData>(JsonOpts);
                if (snap != null) Snapshot?.Invoke(snap);
                break;
            case "projectile_spawn":
                var spawn = env.Data.Deserialize<ProjectileSpawnData>(JsonOpts);
                if (spawn != null) ProjectileSpawned?.Invoke(spawn);
                break;
            case "spell_effect_spawn":
                var effect = env.Data.Deserialize<SpellEffectSpawnData>(JsonOpts);
                if (effect != null) SpellEffectSpawned?.Invoke(effect);
                break;
            case "player_action":
                var action = env.Data.Deserialize<PlayerActionData>(JsonOpts);
                if (action != null) PlayerAction?.Invoke(action);
                break;
            case "chat_message":
                var chat = env.Data.Deserialize<ChatMessageData>(JsonOpts);
                if (chat != null) ChatMessage?.Invoke(chat);
                break;
            case "chat_typing":
                var typing = env.Data.Deserialize<ChatTypingData>(JsonOpts);
                if (typing != null) ChatTyping?.Invoke(typing);
                break;
            case "player_hit":
                var hit = env.Data.Deserialize<PlayerHitData>(JsonOpts);
                if (hit != null) PlayerHit?.Invoke(hit);
                break;
            case "player_heal":
                var heal = env.Data.Deserialize<PlayerHealData>(JsonOpts);
                if (heal != null) PlayerHeal?.Invoke(heal);
                break;
            case "player_buff":
                var buff = env.Data.Deserialize<PlayerBuffData>(JsonOpts);
                if (buff != null) PlayerBuff?.Invoke(buff);
                break;
            case "player_death":
                var death = env.Data.Deserialize<PlayerDeathData>(JsonOpts);
                if (death != null) PlayerDeath?.Invoke(death);
                break;
            case "you_died":
                var youDied = env.Data.Deserialize<YouDiedData>(JsonOpts);
                if (youDied != null)
                {
                    LocalCharacterId = -1;
                    YouDied?.Invoke(youDied);
                }
                break;
            case "skill_xp_gain":
                var skillGain = env.Data.Deserialize<SkillXpGainData>(JsonOpts);
                if (skillGain != null) SkillXpGain?.Invoke(skillGain);
                break;
            case "npc_hit":
                var npcHit = env.Data.Deserialize<NpcHitData>(JsonOpts);
                if (npcHit != null) NpcHit?.Invoke(npcHit);
                break;
            case "world_event":
                var worldEvent = env.Data.Deserialize<WorldEventData>(JsonOpts);
                if (worldEvent != null) WorldEvent?.Invoke(worldEvent);
                break;
            case "boss_spawn":
                var bossSpawn = env.Data.Deserialize<BossSpawnData>(JsonOpts);
                if (bossSpawn != null) BossSpawn?.Invoke(bossSpawn);
                break;
            case "boss_death":
                var bossDeath = env.Data.Deserialize<BossDeathData>(JsonOpts);
                if (bossDeath != null) BossDeath?.Invoke(bossDeath);
                break;
            case "boss_action":
                var bossAction = env.Data.Deserialize<BossActionData>(JsonOpts);
                if (bossAction != null) BossAction?.Invoke(bossAction);
                break;
            case "house_built":
                var houseBuilt = env.Data.Deserialize<HouseBuiltData>(JsonOpts);
                if (houseBuilt != null) HouseBuilt?.Invoke(houseBuilt);
                break;
            case "house_removed":
                var houseRemoved = env.Data.Deserialize<HouseRemovedData>(JsonOpts);
                if (houseRemoved != null) HouseRemoved?.Invoke(houseRemoved);
                break;
            case "house_updated":
                var houseUpdated = env.Data.Deserialize<HouseUpdatedData>(JsonOpts);
                if (houseUpdated != null) HouseUpdated?.Invoke(houseUpdated);
                break;
            case "inventory":
                var inventory = env.Data.Deserialize<InventoryData>(JsonOpts);
                if (inventory != null) InventoryUpdated?.Invoke(inventory);
                break;
            case "fish_result":
                var fishResult = env.Data.Deserialize<FishResultData>(JsonOpts);
                if (fishResult != null) FishResult?.Invoke(fishResult);
                break;
            case "cook_result":
                var cookResult = env.Data.Deserialize<CookResultData>(JsonOpts);
                if (cookResult != null) CookResult?.Invoke(cookResult);
                break;
            case "world_item_removed":
                var itemRemoved = env.Data.Deserialize<WorldItemRemovedData>(JsonOpts);
                if (itemRemoved != null) WorldItemRemoved?.Invoke(itemRemoved);
                break;
            case "world_item_added":
                var itemAdded = env.Data.Deserialize<WorldItemDropState>(JsonOpts);
                if (itemAdded != null) WorldItemAdded?.Invoke(itemAdded);
                break;
            case "friends":
                var friends = env.Data.Deserialize<FriendsData>(JsonOpts);
                if (friends != null)
                {
                    friends.Friends ??= [];
                    FriendsUpdated?.Invoke(friends);
                }
                break;
            case "pm":
                var pm = env.Data.Deserialize<PmData>(JsonOpts);
                if (pm != null) PrivateMessage?.Invoke(pm);
                break;
            case "error":
                var err = env.Data.Deserialize<MessageData>(JsonOpts);
                ServerError?.Invoke(err?.Message ?? "error");
                break;
            case "server_shutdown":
                var shutdown = env.Data.Deserialize<MessageData>(JsonOpts);
                DisconnectMessage = shutdown?.Message
                    ?? "The realm is closing for maintenance. Please log in again shortly.";
                break;
        }
    }

    public void Dispose()
    {
        LogoutWorldAsync().GetAwaiter().GetResult();
        _sendLock.Dispose();
        _http.Dispose();
    }
}
