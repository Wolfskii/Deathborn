using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Audio;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Screens;

public sealed class WorldScreen : IScreen, IDebugInfoScreen
{
    private readonly ScreenManager _screens;
    private readonly WorldBackgroundRenderer _bg = new();
    private readonly Dictionary<long, PlayerEntity> _players = new();
    private readonly List<InteractableEntity> _interactables = [];
    private readonly List<IWorldEffect> _effects = [];
    private readonly Hotbar _hotbar = new();
    private readonly ChatSpotlightOverlay _chat = new();
    private readonly MinimapHud _minimap = new();
    private readonly GameWindowManager _windows = new();
    private readonly WorldMapOverlay _worldMap = new();
    private readonly DeathGhostOverlay _deathOverlay = new();
    private readonly BuffTracker _buffTracker = new();
    private readonly BuffBarOverlay _buffBar = new();
    private readonly Dictionary<long, float> _hunterMarks = new();
    private readonly List<PlayerCorpse> _corpses = [];
    private readonly Dictionary<long, PlayerEntity> _deathWatch = new();
    private GhostEntity? _ghost;
    private Vector2 _corpsePosition;
    private bool _ghostMode;
    private bool _ghostModePending;
    private bool _interactablesSeeded;

    private Vector2 _camera;
    private Vector2 _moveDir;
    private float _inputAccum;
    private string _status = "Connected. WASD to move. Enter to chat. Space or click to attack. [E] to interact.";
    private string _interactPrompt = "";
    private InteractableEntity? _focused;
    private InteractableEntity? _hovered;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private bool _wasWindowActive = true;
    private bool _debugHudVisible;

    private string[] _debugLines = [];

    public WorldScreen(ScreenManager screens)
    {
        _screens = screens;
        _hotbar.SlotActivated += OnHotbarSlot;
        _hotbar.CooldownBlocked += OnHotbarCooldownBlocked;
        _chat.Submitted += OnChatSubmitted;
        _chat.TypingChanged += OnChatTypingChanged;
        _deathOverlay.NewLifeRequested += OnNewLifeRequested;
    }

    public void OnEnter()
    {
        var net = _screens.Net;
        net.Snapshot += OnSnapshot;
        net.Disconnected += OnDisconnected;
        net.ProjectileSpawned += OnProjectileSpawned;
        net.SpellEffectSpawned += OnSpellEffectSpawned;
        net.PlayerAction += OnPlayerAction;
        net.ChatMessage += OnChatMessage;
        net.ChatTyping += OnChatTyping;
        net.PlayerHit += OnPlayerHit;
        net.PlayerHeal += OnPlayerHeal;
        net.PlayerBuff += OnPlayerBuff;
        net.PlayerDeath += OnPlayerDeath;
        net.YouDied += OnYouDied;

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
        TextField.ReleaseFocus();

        _windows.Character.Bind(
            () => _players.TryGetValue(net.LocalCharacterId, out var p) ? p.Stats : null,
            () => _players.TryGetValue(net.LocalCharacterId, out var p) ? p.Name : net.SpawnName);
        _screens.SetOpenCharacterHandler(() => _windows.OpenCharacter());

        MusicPlayer.PlayPlaylist(DeathbornGame.Instance.Content, GameMusic.Get(GameMusic.StartingArea));
    }

    public void OnExit()
    {
        MusicPlayer.Stop();

        var net = _screens.Net;
        net.Snapshot -= OnSnapshot;
        net.Disconnected -= OnDisconnected;
        net.ProjectileSpawned -= OnProjectileSpawned;
        net.SpellEffectSpawned -= OnSpellEffectSpawned;
        net.PlayerAction -= OnPlayerAction;
        net.ChatMessage -= OnChatMessage;
        net.ChatTyping -= OnChatTyping;
        net.PlayerHit -= OnPlayerHit;
        net.PlayerHeal -= OnPlayerHeal;
        net.PlayerBuff -= OnPlayerBuff;
        net.PlayerDeath -= OnPlayerDeath;
        net.YouDied -= OnYouDied;
        _deathOverlay.NewLifeRequested -= OnNewLifeRequested;
        _chat.Submitted -= OnChatSubmitted;
        _chat.TypingChanged -= OnChatTypingChanged;
        if (_chat.IsOpen) _chat.Close(submit: false);
        _hotbar.SlotActivated -= OnHotbarSlot;
        _hotbar.CooldownBlocked -= OnHotbarCooldownBlocked;
        _screens.SetOpenCharacterHandler(null);
        _windows.Character.Close();
        _worldMap.Close();
        _effects.Clear();
        _corpses.Clear();
        _deathWatch.Clear();
        _ghost = null;
        _ghostMode = false;
        _ghostModePending = false;
        _interactables.Clear();
        _interactablesSeeded = false;
    }

    public IReadOnlyList<string> DebugInfoLines => _debugLines;

    public bool HandleEscape()
    {
        if (_worldMap.IsOpen)
        {
            _worldMap.Close();
            return true;
        }

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
                // Swallow input that refocuses the window.
                _prevMouse = mouse;
                _prevKb = kb;
            }
            _wasWindowActive = true;
        }

        if (windowActive && kb.IsKeyDown(Keys.F12) && !_prevKb.IsKeyDown(Keys.F12))
            _debugHudVisible = !_debugHudVisible;

        if (windowActive && InputKeys.EnterPressed(kb, _prevKb) && !_chat.IsOpen)
            _chat.Open();

        _chat.Update(gameTime, kb, _prevKb);

        if (_ghostMode)
        {
            UpdateGhostMode(dt, kb, mouse, windowActive);
            _prevKb = kb;
            if (windowActive)
                _prevMouse = mouse;
            return;
        }

        var chatOpen = _chat.IsOpen;
        var menuOpen = _screens.EscMenuOpen;

        if (windowActive && !chatOpen && kb.IsKeyDown(Keys.M) && !_prevKb.IsKeyDown(Keys.M))
            _worldMap.Toggle();

        if (menuOpen && _worldMap.IsOpen)
            _worldMap.Close();

        var localEntity = FindLocalPlayer();
        var abilityBusy = localEntity is { IsBusy: true };
        var inputBlocked = chatOpen || menuOpen || abilityBusy || IsLocalDyingOrDead();

        _buffTracker.Update(dt);
        UpdateHunterMarks(dt);

        var allowWindowShortcuts = windowActive && !chatOpen;
        var buffCapturesMouse = _buffBar.Update(mouse, _prevMouse, _buffTracker);
        var uiCapturesMouse = _windows.Update(mouse, _prevMouse, kb, _prevKb, allowWindowShortcuts) || buffCapturesMouse;

        _moveDir = inputBlocked ? Vector2.Zero : ReadMoveDir(kb);
        if (localEntity is { IsDead: false })
        {
            localEntity.InputDir = _moveDir;
            if (_moveDir.LengthSquared() > 0.0001f)
                localEntity.AimDir = PlayerEntity.CardinalFacing(_moveDir);
            else if (windowActive && !inputBlocked)
                UpdateLocalAimFacing(localEntity, mouse.Position, _prevMouse.Position);
        }

        if (inputBlocked || IsLocalDyingOrDead())
        {
            if (_lastSentDir.LengthSquared() > 0.0001f)
            {
                _lastSentDir = Vector2.Zero;
                _screens.Net.SendInput(0, 0);
            }
        }
        else
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
        ProcessDeathWatch();

        if (localEntity is { IsDead: false })
        {
            localEntity.CheckLocalMeleeHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localEntity.Id, targetId, damage, ability));
            localEntity.CheckWhirlwindHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localEntity.Id, targetId, damage, ability));
            localEntity.CheckDashHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localEntity.Id, targetId, damage, ability));
        }

        UpdateProjectiles(dt);
        _hotbar.Update(dt, kb, _prevKb, acceptInput: !inputBlocked);

        if (_ghostMode && _ghost != null)
            _camera = _ghost.Position;
        else if (localEntity != null)
            _camera = localEntity.Position;

        if (!inputBlocked && windowActive && !uiCapturesMouse)
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

        _debugLines =
        [
            _status,
            $"Pos: ({(int)_camera.X}, {(int)_camera.Y})  Input: ({_moveDir.X:+#0.0;-#0.0;+0.0}, {_moveDir.Y:+#0.0;-#0.0;+0.0})  {(_moveDir.Length() > 0.05f ? "moving" : "idle")}",
            $"id={_screens.Net.LocalCharacterId}  players={_players.Count}  ws={(_screens.Net.WsConnected ? "open" : "closed")}",
        ];

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

        sb.Begin(samplerState: SamplerState.PointClamp);
        _bg.Draw(sb, _camera, ScreenCenter, zoom);

        foreach (var obj in _interactables)
            obj.Draw(sb, font, WorldToScreen(obj.Position), zoom);

        foreach (var effect in _effects.Where(e => e.DrawUnderEntities))
            effect.Draw(sb, WorldToScreen(effect.Position), zoom);

        foreach (var corpse in _corpses)
            corpse.Draw(sb, WorldToScreen(corpse.Position), zoom);

        foreach (var p in _players.Values.OrderBy(p => p.Position.Y))
        {
            p.Draw(sb, font, WorldToScreen(p.Position), zoom);
            if (_hunterMarks.ContainsKey(p.Id))
                PlayerEntity.DrawHunterMark(sb, WorldToScreen(p.Position), zoom);
        }

        if (_ghostMode && _ghost != null)
            _ghost.Draw(sb, WorldToScreen(_ghost.Position), zoom);

        foreach (var effect in _effects.Where(e => !e.DrawUnderEntities))
            effect.Draw(sb, WorldToScreen(effect.Position), zoom);

        if (_debugHudVisible)
        {
            var y = 12f;
            foreach (var line in _debugLines)
            {
                sb.DrawString(font, line, new Vector2(12, y), y == 12 ? Color.White : new Color(200, 200, 210));
                y += font.LineSpacing;
            }
        }

        if (!string.IsNullOrEmpty(_interactPrompt))
            sb.DrawString(font, _interactPrompt, new Vector2(GameViewport.Width / 2f - 200, GameViewport.Height - 108), new Color(220, 220, 180));

        if (!_ghostMode)
        {
            _hotbar.Draw(sb, font);
            if (!IsLocalDyingOrDead())
            {
                _buffBar.Draw(sb, font, _buffTracker);
                _minimap.Draw(sb, _camera, _screens.Net.LocalCharacterId, _players.Values);
            }
        }
        _windows.Draw(sb, font);
        sb.End();

        DrawChatOverlay(sb, font, zoom);
        DrawWorldMapOverlay(sb, font);

        if (_ghostMode)
        {
            sb.Begin();
            _deathOverlay.Draw(sb, font);
            sb.End();
        }
    }

    private void UpdateGhostMode(float dt, KeyboardState kb, MouseState mouse, bool windowActive)
    {
        if (_ghost == null) return;

        _ghost.MoveDir = windowActive ? ReadMoveDir(kb) : Vector2.Zero;
        _ghost.Update(dt);
        _camera = _ghost.Position;

        if (windowActive)
        {
            var clicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            _deathOverlay.Update(mouse.Position, clicked);
        }
    }

    private PlayerEntity? FindLocalPlayer() =>
        _players.Values.FirstOrDefault(p => p.IsLocal);

    private bool IsLocalDyingOrDead()
    {
        var local = FindLocalPlayer();
        return local is { IsDead: true };
    }

    private void ProcessDeathWatch()
    {
        foreach (var id in _deathWatch.Keys.ToList())
        {
            if (!_deathWatch.TryGetValue(id, out var player) || !player.IsCorpse)
                continue;

            var corpsePos = player.IsLocal ? _corpsePosition : player.Position;
            _corpses.Add(new PlayerCorpse(
                CharacterSprites.CreateCorpseDeathAnim(),
                corpsePos,
                player.MoveDir));

            var wasLocal = player.IsLocal;
            _players.Remove(id);
            _deathWatch.Remove(id);

            if (wasLocal && _ghostModePending)
                ActivateGhostMode();
        }
    }

    private void ActivateGhostMode()
    {
        _ghostMode = true;
        _ghostModePending = false;
        _ghost = new GhostEntity
        {
            Position = _corpsePosition + new Vector2(0, -GhostEntity.FloatHeight),
        };
        _effects.Clear();
        _status = "Your spirit is free. Fly with WASD. Create a new character to return.";
        _chat.Close(submit: false);
        _worldMap.Close();
        _windows.Character.Close();
    }

    private void BeginPlayerDeath(long playerId, Vector2 deathPos, Vector2 facing)
    {
        if (facing.LengthSquared() < 0.01f)
            facing = new Vector2(0, 1);

        if (_players.TryGetValue(playerId, out var player))
        {
            if (_deathWatch.ContainsKey(playerId))
            {
                if (player.IsLocal)
                    _corpsePosition = deathPos;
                return;
            }

            player.Position = deathPos;
            player.Target = deathPos;
            player.InputDir = Vector2.Zero;
            player.BeginDeath(facing);
            _deathWatch[playerId] = player;
            if (player.IsLocal)
                _corpsePosition = deathPos;
        }
        else
        {
            _corpses.Add(new PlayerCorpse(
                CharacterSprites.CreateCorpseDeathAnim(),
                deathPos,
                facing));
        }
    }

    private void OnPlayerDeath(PlayerDeathData data)
    {
        var deathPos = new Vector2((float)data.X, (float)data.Y);
        var facing = new Vector2((float)data.DirX, (float)data.DirY);
        BeginPlayerDeath(data.PlayerId, deathPos, facing);
    }

    private void OnYouDied(YouDiedData data)
    {
        _corpsePosition = new Vector2((float)data.X, (float)data.Y);
        _ghostModePending = true;

        var facing = new Vector2((float)data.DirX, (float)data.DirY);
        var local = FindLocalPlayer();
        if (local != null)
        {
            if (!local.IsDead)
                BeginPlayerDeath(local.Id, _corpsePosition, facing);
            else if (!_deathWatch.ContainsKey(local.Id))
                _deathWatch[local.Id] = local;

            if (local.IsCorpse)
                ActivateGhostMode();
        }
    }

    private void OnNewLifeRequested() => _screens.Change(new CharacterCreateScreen(_screens));

    private void DrawWorldMapOverlay(SpriteBatch sb, SpriteFont font)
    {
        if (!_worldMap.IsOpen) return;
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return;

        sb.Begin(samplerState: SamplerState.PointClamp);
        _worldMap.Draw(sb, font, local.Position);
        sb.End();
    }

    private void DrawChatOverlay(SpriteBatch sb, SpriteFont font, float zoom)
    {
        if (!_chat.IsOpen) return;
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return;

        var screenPos = WorldToScreen(local.Position);
        sb.Begin();
        _chat.Draw(sb, font, screenPos, zoom);
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
            TryMeleeAttack(GetAimDirection());
    }

    private void TryDefaultAttack() => TryMeleeAttack(GetAimDirection());

    private void TryMeleeAttack(Vector2 aimDir)
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return;
        if (!local.StartAttack(aimDir)) return;
        _screens.Net.SendPlayerAction(PlayerActions.MeleeAttack, aimDir.X, aimDir.Y);
    }

    private Vector2 GetAimDirection()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local))
            return new Vector2(0, 1);

        if (_moveDir.LengthSquared() > 0.0001f)
            return PlayerEntity.CardinalFacing(_moveDir);

        if (local.AimDir.LengthSquared() > 0.01f)
            return local.AimDir;

        return new Vector2(0, 1);
    }

    private void UpdateLocalAimFacing(PlayerEntity local, Point mouseScreen, Point prevMouseScreen)
    {
        if (!IsMouseInViewport(mouseScreen) || local.IsMoving || local.IsBusy)
            return;

        if (mouseScreen == prevMouseScreen)
            return;

        var toMouse = ScreenToWorld(mouseScreen) - local.Position;
        if (toMouse.LengthSquared() <= 4f)
            return;

        local.AimDir = PlayerEntity.CardinalFacing(toMouse);
    }

    private static bool IsMouseInViewport(Point p) =>
        p.X >= 0 && p.Y >= 0 && p.X < GameViewport.Width && p.Y < GameViewport.Height;

    private bool CastProjectileSpell(ProjectileDefinition def, ProjectileStyle style, float castLock, string status)
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (local.IsBusy) return false;

        var dir = GetAimDirection();
        local.StartAbilityLock(castLock);
        local.MoveDir = dir;

        var origin = local.GetProjectileSpawnPoint(dir);
        _effects.Add(SpellProjectile.Spawn(origin, dir, local.Id, def, style));
        _screens.Net.SendCastSpell(def.Id, dir.X, dir.Y);
        _status = status;
        return true;
    }

    private bool CastFireball(ProjectileDefinition? definition = null) =>
        CastProjectileSpell(
            definition ?? ProjectileDefinitions.Fireball,
            ProjectileStyle.Fire,
            Config.FireballCastLockDuration,
            "You cast Fireball.");

    private bool CastIceShard() =>
        CastProjectileSpell(
            ProjectileDefinitions.IceShard,
            ProjectileStyle.Ice,
            Config.IceShardCastLockDuration,
            "You cast Ice Shard.");

    private bool CastArcBolt()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (local.IsBusy) return false;

        var dir = GetAimDirection();
        local.StartAbilityLock(Config.ArcBoltCastLockDuration);
        local.MoveDir = dir;

        var target = FindArcBoltTarget(local.Position, dir, local.Id);
        if (target != null)
        {
            _effects.Add(new ArcBoltEffect(local.Position, target.Position, local.Id));
            ReportAbilityHit(local.Id, target.Id, Config.ArcBoltDamage, "arc_bolt");
        }
        else
        {
            var end = local.Position + dir * Config.ArcBoltRange;
            _effects.Add(new ArcBoltEffect(local.Position, end, local.Id));
        }

        _screens.Net.SendCastSpell("arc_bolt", dir.X, dir.Y);
        _status = target != null ? "Arc Bolt strikes!" : "Arc Bolt fizzles.";
        return true;
    }

    private bool CastPoisonCloud()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (local.IsBusy) return false;

        var dir = GetAimDirection();
        local.StartAbilityLock(Config.PoisonCloudCastLockDuration);
        local.MoveDir = dir;

        _effects.Add(new PoisonCloudEffect(local.Position, local.Id));
        _screens.Net.SendCastSpell("poison_cloud", dir.X, dir.Y);
        _status = "You release a Poison Cloud.";
        return true;
    }

    private bool UseBandage()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (local.IsBandaging) return false;

        _screens.Net.SendAbilityUse("bandage");
        local.StartBandageHoT();
        _status = "Bandage applied — healing over time.";
        return true;
    }

    private bool CastShieldBash()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        var dir = GetAimDirection();
        if (!local.StartMeleeAbility(MeleeAbilityDefinitions.ShieldBash, dir)) return false;
        _screens.Net.SendCastSpell("shield_bash", dir.X, dir.Y);
        _status = "Shield Bash!";
        return true;
    }

    private bool CastWhirlwind()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        var dir = GetAimDirection();
        if (!local.StartWhirlwind()) return false;
        _effects.Add(new WhirlwindEffect(local.Position, local.Id, Config.WhirlwindDuration));
        _screens.Net.SendCastSpell("whirlwind", dir.X, dir.Y);
        _status = "Whirlwind!";
        return true;
    }

    private bool CastWarriorDash()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        var dir = GetAimDirection();
        if (!local.StartDash(dir, Config.WarriorDashDistance, Config.WarriorDashDuration)) return false;
        _effects.Add(new DashTrailEffect(local.Position, dir, local.Id, Config.WarriorDashDuration));
        _screens.Net.SendCastSpell("warrior_dash", dir.X, dir.Y);
        _status = "Charge!";
        return true;
    }

    private bool UseBattleShout()
    {
        _screens.Net.SendAbilityUse("battle_shout");
        _status = "Battle Shout — damage increased!";
        return true;
    }

    private bool UseIronSkin()
    {
        _screens.Net.SendAbilityUse("iron_skin");
        _status = "Iron Skin — damage reduced!";
        return true;
    }

    private bool CastHunterMark()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        var dir = GetAimDirection();
        local.StartAbilityLock(0.3f);
        local.MoveDir = dir;
        _screens.Net.SendCastSpell("hunter_mark", dir.X, dir.Y);
        _status = "Hunter's Mark cast.";
        return true;
    }

    private bool UseSecondWind()
    {
        _screens.Net.SendAbilityUse("second_wind");
        _status = "Second Wind — recovering health.";
        return true;
    }

    private void UpdateHunterMarks(float dt)
    {
        foreach (var id in _hunterMarks.Keys.ToList())
        {
            _hunterMarks[id] -= dt;
            if (_hunterMarks[id] <= 0f)
                _hunterMarks.Remove(id);
        }
    }

    private void OnPlayerBuff(PlayerBuffData data)
    {
        if (data.Duration <= 0)
        {
            if (data.BuffId == "hunter_mark" && data.MarkTargetId > 0)
                _hunterMarks.Remove(data.MarkTargetId);
            if (data.PlayerId == _screens.Net.LocalCharacterId)
                _buffTracker.Remove(data.BuffId);
            return;
        }

        if (data.BuffId == "hunter_mark" && data.MarkTargetId > 0)
            _hunterMarks[data.MarkTargetId] = (float)data.Duration;

        if (data.PlayerId != _screens.Net.LocalCharacterId) return;

        _buffTracker.Apply(data.BuffId, (float)data.Duration, data.MarkTargetId);
        var (name, desc, _) = BuffCatalog.Describe(data.BuffId);
        _status = $"{name}: {desc}";
    }

    private PlayerEntity? FindArcBoltTarget(Vector2 origin, Vector2 facing, long selfId)
    {
        PlayerEntity? best = null;
        var bestDist = Config.ArcBoltRange * Config.ArcBoltRange;
        foreach (var (id, player) in _players)
        {
            if (id == selfId || player.IsDead) continue;
            var to = player.Position - origin;
            var distSq = to.LengthSquared();
            if (distSq > bestDist || distSq < 1f) continue;
            if (Vector2.Dot(Vector2.Normalize(to), facing) < 0.35f) continue;
            bestDist = distSq;
            best = player;
        }
        return best;
    }

    private void OnProjectileSpawned(ProjectileSpawnData data)
    {
        if (data.OwnerId == _screens.Net.LocalCharacterId) return;

        var dir = new Vector2((float)data.DirX, (float)data.DirY);
        var origin = new Vector2((float)data.X, (float)data.Y);
        var spellId = data.SpellId ?? "fireball";
        if (spellId == "ice_shard")
            _effects.Add(SpellProjectile.Spawn(origin, dir, data.OwnerId, ProjectileDefinitions.IceShard, ProjectileStyle.Ice));
        else
            _effects.Add(SpellProjectile.Spawn(origin, dir, data.OwnerId, ProjectileDefinitions.Fireball, ProjectileStyle.Fire));
    }

    private void OnSpellEffectSpawned(SpellEffectSpawnData data)
    {
        if (data.SpellId != "poison_cloud") return;
        if (data.OwnerId == _screens.Net.LocalCharacterId) return;
        _effects.Add(new PoisonCloudEffect(new Vector2((float)data.X, (float)data.Y), data.OwnerId));
    }

    private void OnPlayerAction(PlayerActionData data)
    {
        if (!_players.TryGetValue(data.PlayerId, out var player)) return;

        var isLocal = data.PlayerId == _screens.Net.LocalCharacterId;
        if (isLocal && data.Action == PlayerActions.MeleeAttack && player.IsBusy)
            return;

        var dir = new Vector2((float)data.DirX, (float)data.DirY);
        if (dir.LengthSquared() < 0.01f)
            dir = player.FacingDir;
        else
            dir = Vector2.Normalize(dir);

        player.PlayAction(data.Action, dir, data.TargetId);

        if (data.PlayerId == _screens.Net.LocalCharacterId) return;

        switch (data.Action)
        {
            case PlayerActions.CastArcBolt:
                if (_players.TryGetValue(data.PlayerId, out var caster))
                {
                    var target = FindArcBoltTarget(caster.Position, dir, caster.Id);
                    var end = target?.Position ?? caster.Position + dir * Config.ArcBoltRange;
                    _effects.Add(new ArcBoltEffect(caster.Position, end, data.PlayerId));
                }
                break;
            case PlayerActions.UseBandage:
                player.StartBandageHoT();
                break;
            case PlayerActions.Whirlwind:
                _effects.Add(new WhirlwindEffect(player.Position, player.Id, Config.WhirlwindDuration));
                break;
            case PlayerActions.WarriorDash:
                _effects.Add(new DashTrailEffect(player.Position, dir, player.Id, Config.WarriorDashDuration));
                break;
        }
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
        foreach (var effect in _effects)
        {
            var ownerId = effect.OwnerId;
            effect.Update(dt, _players, _interactables, ownerId == localId, (targetId, damage) =>
                ReportAbilityHit(ownerId, targetId, damage, effect.AbilityId));
        }

        ResolveProjectileClashes();
        _effects.RemoveAll(e => !e.Alive);
    }

    private void ReportAbilityHit(long attackerId, long targetId, int damage, string ability)
    {
        if (damage <= 0) return;

        if (attackerId == _screens.Net.LocalCharacterId)
            _screens.Net.SendAbilityHit(targetId, damage, ability);
    }

    private void OnPlayerHit(PlayerHitData data)
    {
        if (!_players.TryGetValue(data.TargetId, out var target)) return;
        if (data.HpMax > 0)
            target.SyncStats((float)data.Hp, (float)data.HpMax);
        target.ApplyHit(data.Damage);
    }

    private void OnPlayerHeal(PlayerHealData data)
    {
        if (!_players.TryGetValue(data.PlayerId, out var player)) return;
        if (data.HpMax > 0)
            player.SyncStats((float)data.Hp, (float)data.HpMax);

        if (data.PlayerId == _screens.Net.LocalCharacterId)
            _status = data.Ability switch
            {
                "bandage" => $"+{data.Amount} HP from bandage ({(int)data.Hp}/{(int)data.HpMax}).",
                "second_wind" => $"+{data.Amount} HP from Second Wind ({(int)data.Hp}/{(int)data.HpMax}).",
                _ => $"+{data.Amount} HP ({(int)data.Hp}/{(int)data.HpMax}).",
            };
    }

    private void ResolveProjectileClashes()
    {
        for (var i = 0; i < _effects.Count; i++)
        {
            var a = _effects[i];
            if (!a.CanClash) continue;

            for (var j = i + 1; j < _effects.Count; j++)
            {
                var b = _effects[j];
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
            else if (!_deathWatch.ContainsKey(s.Id))
            {
                p.SetTarget(pos);
            }

            if (s.HpMax > 0 && !_deathWatch.ContainsKey(s.Id))
                p.SyncStats((float)s.Hp, (float)s.HpMax);
        }

        foreach (var id in _players.Keys.Where(id => !seen.Contains(id)).ToList())
        {
            if (_deathWatch.ContainsKey(id))
                continue;
            _players.Remove(id);
        }
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
        var id = entry.GetValueOrDefault(HotbarEntry.IdKey) as string;

        if (id == "fireball")
        {
            var projectileId = entry.GetValueOrDefault("projectileId") as string;
            var def = projectileId != null ? ProjectileDefinitions.Get(projectileId) : ProjectileDefinitions.Fireball;
            used = CastFireball(def);
        }
        else if (id == "ice_shard")
            used = CastIceShard();
        else if (id == "arc_bolt")
            used = CastArcBolt();
        else if (id == "poison_cloud")
            used = CastPoisonCloud();
        else if (id == "bandage")
            used = UseBandage();
        else if (id == "shield_bash")
            used = CastShieldBash();
        else if (id == "whirlwind")
            used = CastWhirlwind();
        else if (id == "warrior_dash")
            used = CastWarriorDash();
        else if (id == "battle_shout")
            used = UseBattleShout();
        else if (id == "iron_skin")
            used = UseIronSkin();
        else if (id == "hunter_mark")
            used = CastHunterMark();
        else if (id == "second_wind")
            used = UseSecondWind();
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
            ["id"] = "shield_bash",
            ["name"] = "Shield Bash",
            ["kind"] = "melee",
            [HotbarEntry.CooldownKey] = Config.ShieldBashCooldown,
        });
        _hotbar.SetSlot(1, new Dictionary<string, object>
        {
            ["id"] = "whirlwind",
            ["name"] = "Whirlwind",
            ["kind"] = "melee",
            [HotbarEntry.CooldownKey] = Config.WhirlwindCooldown,
        });
        _hotbar.SetSlot(2, new Dictionary<string, object>
        {
            ["id"] = "warrior_dash",
            ["name"] = "Charge",
            ["kind"] = "melee",
            [HotbarEntry.CooldownKey] = Config.WarriorDashCooldown,
        });
        _hotbar.SetSlot(3, new Dictionary<string, object>
        {
            ["id"] = "fireball",
            ["name"] = "Fireball",
            ["kind"] = "spell",
            ["projectileId"] = ProjectileDefinitions.Fireball.Id,
            [HotbarEntry.CooldownKey] = Config.FireballCooldown,
        });
        _hotbar.SetSlot(4, new Dictionary<string, object>
        {
            ["id"] = "ice_shard",
            ["name"] = "Ice Shard",
            ["kind"] = "spell",
            [HotbarEntry.CooldownKey] = Config.IceShardCooldown,
        });
        _hotbar.SetSlot(5, new Dictionary<string, object>
        {
            ["id"] = "battle_shout",
            ["name"] = "Battle Shout",
            ["kind"] = "buff",
            [HotbarEntry.CooldownKey] = Config.BattleShoutCooldown,
        });
        _hotbar.SetSlot(6, new Dictionary<string, object>
        {
            ["id"] = "iron_skin",
            ["name"] = "Iron Skin",
            ["kind"] = "buff",
            [HotbarEntry.CooldownKey] = Config.IronSkinCooldown,
        });
        _hotbar.SetSlot(7, new Dictionary<string, object>
        {
            ["id"] = "hunter_mark",
            ["name"] = "Hunter's Mark",
            ["kind"] = "utility",
            [HotbarEntry.CooldownKey] = Config.HunterMarkCooldown,
        });
        _hotbar.SetSlot(8, new Dictionary<string, object>
        {
            ["id"] = "bandage",
            ["name"] = "Bandage",
            ["kind"] = "item",
            [HotbarEntry.CooldownKey] = Config.BandageCooldown,
        });
        _hotbar.SetSlot(9, new Dictionary<string, object>
        {
            ["id"] = "second_wind",
            ["name"] = "Second Wind",
            ["kind"] = "heal",
            [HotbarEntry.CooldownKey] = Config.SecondWindCooldown,
        });
    }

    private void SeedStarterTownInteractables()
    {
        if (_interactablesSeeded) return;
        _interactablesSeeded = true;

        var origin = WorldMap.Realik.DefaultSpawn;
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
