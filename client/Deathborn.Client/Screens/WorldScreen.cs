using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Audio;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class WorldScreen : IScreen
{
    private readonly ScreenManager _screens;
    private readonly WorldBackgroundRenderer _bg = new();
    private readonly Dictionary<long, PlayerEntity> _players = new();
    private readonly List<InteractableEntity> _interactables = [];
    private readonly List<FireballProjectile> _projectiles = [];
    private readonly Hotbar _hotbar = new();
    private readonly ChatSpotlightOverlay _chat = new();
    private readonly MinimapHud _minimap = new();
    private bool _interactablesSeeded;

    private Vector2 _camera;
    private Vector2 _moveDir;
    private float _inputAccum;
    private string _status = "Connected. WASD to move. Enter to chat. Space or click to attack. [E] to interact.";
    private string _hud = "";
    private string _interactPrompt = "";
    private InteractableEntity? _focused;
    private InteractableEntity? _hovered;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private bool _wasWindowActive = true;

    public WorldScreen(ScreenManager screens)
    {
        _screens = screens;
        _hotbar.SlotActivated += OnHotbarSlot;
        _hotbar.CooldownBlocked += OnHotbarCooldownBlocked;
        _chat.Submitted += OnChatSubmitted;
        _chat.TypingChanged += OnChatTypingChanged;
    }

    public void OnEnter()
    {
        var net = _screens.Net;
        net.Snapshot += OnSnapshot;
        net.Disconnected += OnDisconnected;
        net.ProjectileSpawned += OnProjectileSpawned;
        net.PlayerAction += OnPlayerAction;
        net.ChatMessage += OnChatMessage;
        net.ChatTyping += OnChatTyping;
        net.PlayerHit += OnPlayerHit;

        if (net.LocalCharacterId >= 0 && !_players.ContainsKey(net.LocalCharacterId))
        {
            _players[net.LocalCharacterId] = new PlayerEntity
            {
                Id = net.LocalCharacterId,
                Name = net.SpawnName,
                Position = new Vector2(net.SpawnX, net.SpawnY),
                Target = new Vector2(net.SpawnX, net.SpawnY),
                IsLocal = true,
            };
        }

        SeedStarterTownInteractables();
        SeedHotbar();

        MusicPlayer.PlayPlaylist(DeathbornGame.Instance.Content, GameMusic.Get(GameMusic.StartingArea));
    }

    public void OnExit()
    {
        MusicPlayer.Stop();

        var net = _screens.Net;
        net.Snapshot -= OnSnapshot;
        net.Disconnected -= OnDisconnected;
        net.ProjectileSpawned -= OnProjectileSpawned;
        net.PlayerAction -= OnPlayerAction;
        net.ChatMessage -= OnChatMessage;
        net.ChatTyping -= OnChatTyping;
        net.PlayerHit -= OnPlayerHit;
        _chat.Submitted -= OnChatSubmitted;
        _chat.TypingChanged -= OnChatTypingChanged;
        if (_chat.IsOpen) _chat.Close(submit: false);
        _hotbar.SlotActivated -= OnHotbarSlot;
        _hotbar.CooldownBlocked -= OnHotbarCooldownBlocked;
        _projectiles.Clear();
        _interactables.Clear();
        _interactablesSeeded = false;
    }

    public bool HandleEscape()
    {
        if (_chat.IsOpen)
        {
            _chat.Close(submit: false);
            return true;
        }
        return false;
    }

    public void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        var windowActive = DeathbornGame.Instance.IsActive;

        if (!windowActive)
        {
            _prevKb = kb;
            _prevMouse = mouse;
            _wasWindowActive = false;
        }
        else
        {
            if (!_wasWindowActive)
            {
                // Swallow the click that refocuses the window.
                _prevMouse = mouse;
            }
            _wasWindowActive = true;
        }

        if (windowActive && InputKeys.EnterPressed(kb, _prevKb) && !_chat.IsOpen)
            _chat.Open();

        _chat.Update(gameTime, kb, _prevKb);
        var chatOpen = _chat.IsOpen;

        _moveDir = chatOpen ? Vector2.Zero : ReadMoveDir(kb);
        if (_players.TryGetValue(_screens.Net.LocalCharacterId, out var localPlayer))
            localPlayer.InputDir = _moveDir;

        if (!chatOpen)
        {
            _inputAccum += dt;
            if (_inputAccum >= Config.InputSendInterval || Vector2.DistanceSquared(_moveDir, _lastSentDir) > 0.0001f)
            {
                _inputAccum = 0;
                _lastSentDir = _moveDir;
                _screens.Net.SendInput(_moveDir.X, _moveDir.Y);
            }
        }

        foreach (var p in _players.Values) p.Update(dt);

        if (_players.TryGetValue(_screens.Net.LocalCharacterId, out var localAttacker))
            localAttacker.CheckLocalMeleeHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localAttacker.Id, targetId, damage, ability));

        UpdateProjectiles(dt);
        if (!chatOpen)
            _hotbar.Update(dt, kb, _prevKb);

        if (_players.TryGetValue(_screens.Net.LocalCharacterId, out var local))
            _camera = local.Position;

        if (!chatOpen && windowActive)
        {
            UpdateInteractFocus(mouse.Position);
            UpdateInteractPrompt();

            if (kb.IsKeyDown(Keys.E) && !_prevKb.IsKeyDown(Keys.E))
                TryInteractNearest();

            if (kb.IsKeyDown(Keys.Space) && !_prevKb.IsKeyDown(Keys.Space))
                TryDefaultAttack();

            if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                HandleLeftClick(mouse.Position);
        }

        _hud = $"Pos: ({(int)_camera.X}, {(int)_camera.Y})  Input: ({_moveDir.X:+#0.0;-#0.0;+0.0}, {_moveDir.Y:+#0.0;-#0.0;+0.0})  " +
               $"{(_moveDir.Length() > 0.05f ? "moving" : "idle")}  id={_screens.Net.LocalCharacterId}  players={_players.Count}  " +
               $"ws={(_screens.Net.WsConnected ? "open" : "closed")}";

        _prevKb = kb;
        if (windowActive)
            _prevMouse = mouse;
    }

    private Vector2 _lastSentDir;

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;

        var zoom = GameViewport.WorldZoom;

        sb.Begin();
        _bg.Draw(sb, _camera, ScreenCenter, zoom);

        foreach (var obj in _interactables)
            obj.Draw(sb, font, WorldToScreen(obj.Position), zoom);

        foreach (var proj in _projectiles)
            proj.Draw(sb, WorldToScreen(proj.Position), zoom);

        foreach (var p in _players.Values)
            p.Draw(sb, font, WorldToScreen(p.Position), zoom);

        sb.DrawString(font, _status, new Vector2(12, 12), Color.White);
        sb.DrawString(font, _hud, new Vector2(12, 36), new Color(200, 200, 210));

        if (!string.IsNullOrEmpty(_interactPrompt))
            sb.DrawString(font, _interactPrompt, new Vector2(GameViewport.Width / 2f - 200, GameViewport.Height - 108), new Color(220, 220, 180));

        _hotbar.Draw(sb, font);
        _minimap.Draw(sb, _camera, _screens.Net.LocalCharacterId, _players.Values);
        _chat.Draw(sb, font);
        sb.End();
    }

    private Vector2 ReadMoveDir(KeyboardState kb)
    {
        var dir = Vector2.Zero;
        if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) dir.Y -= 1;
        if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) dir.Y += 1;
        if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dir.X -= 1;
        if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dir.X += 1;
        return dir.LengthSquared() > 1 ? Vector2.Normalize(dir) : dir;
    }

    private Vector2 ScreenCenter => GameViewport.Center;

    private Vector2 WorldToScreen(Vector2 world) =>
        (world - _camera) * GameViewport.WorldZoom + ScreenCenter;

    private Vector2 ScreenToWorld(Point screen) =>
        (new Vector2(screen.X, screen.Y) - ScreenCenter) / GameViewport.WorldZoom + _camera;

    private void UpdateInteractFocus(Point mouseScreen)
    {
        var mouseWorld = ScreenToWorld(mouseScreen);
        var nearest = FindNearestInRange(_camera);
        var underMouse = FindAtPoint(mouseWorld);

        if (_focused != nearest)
        {
            if (_focused != null) _focused.Highlighted = false;
            _focused = nearest;
            if (_focused != null && _focused != _hovered) _focused.Highlighted = true;
        }

        if (_hovered != underMouse)
        {
            if (_hovered != null && _hovered != _focused) _hovered.Highlighted = false;
            _hovered = underMouse;
            if (_hovered != null) _hovered.Highlighted = true;
            else if (_focused != null) _focused.Highlighted = true;
        }
    }

    private void UpdateInteractPrompt()
    {
        if (_focused != null)
            _interactPrompt = $"[E] Interact with {_focused.DisplayName}  (or click)";
        else if (_hovered != null)
        {
            _interactPrompt = _hovered.IsInRange(_camera)
                ? $"Click to interact with {_hovered.DisplayName}"
                : $"Too far - move closer to {_hovered.DisplayName}";
        }
        else _interactPrompt = "";
    }

    private InteractableEntity? FindAtPoint(Vector2 worldPos)
    {
        InteractableEntity? best = null;
        var bestDist = float.MaxValue;
        foreach (var obj in _interactables)
        {
            if (!obj.IsNearPoint(worldPos)) continue;
            var d = Vector2.DistanceSquared(obj.Position, worldPos);
            if (d < bestDist) { bestDist = d; best = obj; }
        }
        return best;
    }

    private InteractableEntity? FindNearestInRange(Vector2 from)
    {
        InteractableEntity? best = null;
        var bestDist = float.MaxValue;
        foreach (var obj in _interactables)
        {
            if (!obj.IsInRange(from)) continue;
            var d = Vector2.DistanceSquared(obj.Position, from);
            if (d < bestDist) { bestDist = d; best = obj; }
        }
        return best;
    }

    private void TryInteractNearest()
    {
        var target = FindNearestInRange(_camera);
        if (target == null) { _status = "Nothing in range to interact with."; return; }
        PerformInteract(target);
    }

    private void HandleLeftClick(Point mouseScreen)
    {
        var world = ScreenToWorld(mouseScreen);
        var target = FindAtPoint(world);
        if (target != null && target.IsInRange(_camera))
            PerformInteract(target);
        else
            TryDefaultAttack();
    }

    private void TryDefaultAttack()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return;
        var dir = PlayerEntity.CardinalFacing(local.FacingDir);
        local.StartAttack(dir);
        _screens.Net.SendPlayerAction(PlayerActions.MeleeAttack, dir.X, dir.Y);
    }

    private bool CastFireball(ProjectileDefinition? definition = null)
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;

        var dir = PlayerEntity.CardinalFacing(local.FacingDir);
        var origin = local.GetProjectileSpawnPoint(dir);
        _projectiles.Add(FireballProjectile.Spawn(origin, dir, local.Id, definition));
        _screens.Net.SendCastFireball(dir.X, dir.Y);
        _status = "You cast Fireball.";
        return true;
    }

    private void OnProjectileSpawned(ProjectileSpawnData data)
    {
        if (data.OwnerId == _screens.Net.LocalCharacterId) return;

        var dir = new Vector2((float)data.DirX, (float)data.DirY);
        var origin = new Vector2((float)data.X, (float)data.Y);
        _projectiles.Add(FireballProjectile.Spawn(origin, dir, data.OwnerId));
    }

    private void OnPlayerAction(PlayerActionData data)
    {
        if (!_players.TryGetValue(data.PlayerId, out var player)) return;

        var isLocal = data.PlayerId == _screens.Net.LocalCharacterId;
        if (isLocal && data.Action == PlayerActions.MeleeAttack && player.IsAttacking)
            return;

        var dir = new Vector2((float)data.DirX, (float)data.DirY);
        if (dir.LengthSquared() < 0.01f)
            dir = player.FacingDir;
        else
            dir = Vector2.Normalize(dir);

        player.PlayAction(data.Action, dir, data.TargetId);
    }

    private void OnChatSubmitted(string text)
    {
        if (_players.TryGetValue(_screens.Net.LocalCharacterId, out var local))
            local.ShowChatMessage(text);
        _screens.Net.SendChatMessage(text);
    }

    private void OnChatTypingChanged(bool typing)
    {
        if (_players.TryGetValue(_screens.Net.LocalCharacterId, out var local))
            local.IsTyping = typing;
        _screens.Net.SendChatTyping(typing);
    }

    private void OnChatMessage(ChatMessageData data)
    {
        if (!_players.TryGetValue(data.PlayerId, out var player)) return;
        player.ShowChatMessage(data.Text);
    }

    private void OnChatTyping(ChatTypingData data)
    {
        if (data.PlayerId == _screens.Net.LocalCharacterId) return;
        if (!_players.TryGetValue(data.PlayerId, out var player)) return;
        player.IsTyping = data.Typing;
    }

    private void UpdateProjectiles(float dt)
    {
        var localId = _screens.Net.LocalCharacterId;
        foreach (var proj in _projectiles)
        {
            var ownerId = proj.OwnerId;
            proj.Update(dt, _players, _interactables, ownerId == localId, (targetId, damage) =>
                ReportAbilityHit(ownerId, targetId, damage, proj.Definition.Id));
        }

        ResolveProjectileClashes();

        _projectiles.RemoveAll(p => !p.Alive);
    }

    private void ReportAbilityHit(long attackerId, long targetId, int damage, string ability)
    {
        if (damage <= 0) return;

        if (_players.TryGetValue(targetId, out var target))
            target.ApplyHit(damage);

        if (attackerId == _screens.Net.LocalCharacterId)
            _screens.Net.SendAbilityHit(targetId, damage, ability);
    }

    private void OnPlayerHit(PlayerHitData data)
    {
        if (!_players.TryGetValue(data.TargetId, out var target)) return;
        target.ApplyHit(data.Damage);
    }

    private void ResolveProjectileClashes()
    {
        for (var i = 0; i < _projectiles.Count; i++)
        {
            var a = _projectiles[i];
            if (!a.CanClash) continue;

            for (var j = i + 1; j < _projectiles.Count; j++)
            {
                var b = _projectiles[j];
                if (!b.CanClash) continue;

                var hit = a.HitRadius + b.HitRadius;
                if (Vector2.DistanceSquared(a.Position, b.Position) > hit * hit) continue;

                a.CancelByClash();
                b.CancelByClash();
            }
        }
    }

    private void PerformInteract(InteractableEntity target)
    {
        _status = target.InteractMessage();
        _screens.Net.SendInteract(target.Id);
    }

    private void OnSnapshot(List<PlayerState> players)
    {
        var seen = new HashSet<long>();
        foreach (var s in players)
        {
            seen.Add(s.Id);
            var pos = new Vector2((float)s.X, (float)s.Y);
            if (!_players.TryGetValue(s.Id, out var p))
            {
                p = PlayerEntity.FromState(s, s.Id == _screens.Net.LocalCharacterId);
                _players[s.Id] = p;
            }
            else p.SetTarget(pos);
        }

        foreach (var id in _players.Keys.Where(id => !seen.Contains(id)).ToList())
            _players.Remove(id);
    }

    private void OnDisconnected() => _status = "Disconnected from server.";

    private void OnHotbarSlot(int index, Dictionary<string, object>? entry)
    {
        var key = Hotbar.KeyLabels[index];
        if (entry == null)
        {
            _status = $"Hotbar slot {key} is empty.";
            return;
        }

        var cooldown = HotbarEntry.GetCooldown(entry);
        var used = false;

        if (entry.GetValueOrDefault(HotbarEntry.IdKey) is string id && id == "fireball")
        {
            var projectileId = entry.GetValueOrDefault("projectileId") as string;
            var def = projectileId != null ? ProjectileDefinitions.Get(projectileId) : ProjectileDefinitions.Fireball;
            used = CastFireball(def);
        }
        else
        {
            _status = $"Used slot {key}: {entry.GetValueOrDefault("name")} ({entry.GetValueOrDefault("kind")})";
            used = true;
        }

        if (used && cooldown > 0f)
            _hotbar.StartCooldown(index, cooldown);
    }

    private void OnHotbarCooldownBlocked(int index, float remaining)
    {
        var entry = _hotbar.Slots[index].Entry;
        var name = entry?.GetValueOrDefault("name") ?? Hotbar.KeyLabels[index];
        _status = $"{name} is on cooldown ({MathF.Ceiling(remaining):0}s).";
    }

    private void SeedHotbar()
    {
        _hotbar.SetSlot(0, new Dictionary<string, object>
        {
            ["id"] = "fireball",
            ["name"] = "Fireball",
            ["kind"] = "spell",
            ["color"] = new Color(1f, 0.45f, 0.12f),
            ["projectileId"] = ProjectileDefinitions.Fireball.Id,
            [HotbarEntry.CooldownKey] = Config.FireballCooldown,
        });
        _hotbar.SetSlot(1, new Dictionary<string, object> { ["id"] = "heal", ["name"] = "Heal", ["kind"] = "spell", ["color"] = new Color(0.35f, 0.75f, 0.45f), [HotbarEntry.CooldownKey] = 0f });
        _hotbar.SetSlot(2, new Dictionary<string, object> { ["id"] = "bandage", ["name"] = "Bandage", ["kind"] = "item", ["color"] = new Color(0.75f, 0.65f, 0.35f), [HotbarEntry.CooldownKey] = 0f });
    }

    private void SeedStarterTownInteractables()
    {
        if (_interactablesSeeded) return;
        _interactablesSeeded = true;

        var origin = new Vector2(Config.StarterTownX, Config.StarterTownY);
        void Add(string id, string name, Vector2 offset, InteractableKind kind, Color tint, float pick = 20f)
        {
            _interactables.Add(new InteractableEntity
            {
                Id = id, DisplayName = name, Position = origin + offset,
                Kind = kind, Tint = tint, PickRadius = pick,
            });
        }

        Add("sign_welcome", "Welcome to Starter Town", new Vector2(0, -85), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        Add("npc_guide", "Guide Aldric", new Vector2(-70, -55), InteractableKind.Npc, new Color(0.72f, 0.58f, 0.42f));
        Add("bank_starter", "Town Bank", new Vector2(70, -55), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        Add("fish_shore_1", "Fishing Spot", new Vector2(-38, -92), InteractableKind.Fishing, new Color(0.55f, 0.72f, 0.85f), 24);
        Add("fish_shore_2", "Fishing Spot", new Vector2(38, -92), InteractableKind.Fishing, new Color(0.5f, 0.68f, 0.82f), 24);
        Add("npc_fisher", "Old Fisher", new Vector2(92, -42), InteractableKind.Npc, new Color(0.65f, 0.7f, 0.75f));
        Add("tree_oak_1", "Oak Tree", new Vector2(92, 0), InteractableKind.Tree, new Color(0.25f, 0.55f, 0.28f));
        Add("tree_oak_2", "Oak Tree", new Vector2(72, 58), InteractableKind.Tree, new Color(0.22f, 0.5f, 0.26f));
        Add("anvil_starter", "Public Anvil", new Vector2(42, 82), InteractableKind.Anvil, new Color(0.38f, 0.4f, 0.44f), 22);
        Add("sign_wilderness", "Wilderness - PvP enabled", new Vector2(88, 72), InteractableKind.Sign, new Color(0.85f, 0.35f, 0.3f), 22);
        Add("chest_starter", "Starter Chest", new Vector2(0, 92), InteractableKind.Chest, new Color(0.62f, 0.42f, 0.22f));
        Add("tree_pine_1", "Pine Tree", new Vector2(-42, 82), InteractableKind.Tree, new Color(0.18f, 0.42f, 0.32f));
        Add("sign_mine", "Mine entrance - danger", new Vector2(-72, 58), InteractableKind.Sign, new Color(0.78f, 0.55f, 0.35f), 22);
        Add("chest_loot_1", "Abandoned Crate", new Vector2(-92, 42), InteractableKind.Chest, new Color(0.48f, 0.32f, 0.2f));
        Add("rock_iron_1", "Iron Rock", new Vector2(-92, 0), InteractableKind.Rock, new Color(0.45f, 0.48f, 0.52f), 22);
        Add("rock_copper_1", "Copper Rock", new Vector2(-72, -28), InteractableKind.Rock, new Color(0.58f, 0.4f, 0.28f), 22);
        Add("npc_hermit", "Hermit", new Vector2(-92, -42), InteractableKind.Npc, new Color(0.55f, 0.45f, 0.38f));
    }
}
