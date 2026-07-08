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
    private readonly Dictionary<long, BossEntity> _bosses = new();
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
    private readonly DragDropManager _dragDrop = new();
    private readonly PlayerInventory _inventory = new();
    private readonly PlayerSkills _skills = new();
    private readonly PlayerFriends _friends = new();
    private readonly PlayerContextMenuOverlay _playerContextMenu = new();
    private readonly List<PlayerCorpse> _corpses = [];
    private readonly Dictionary<long, PlayerEntity> _deathWatch = new();
    private GhostEntity? _ghost;
    private Vector2 _corpsePosition;
    private bool _ghostMode;
    private bool _ghostModePending;
    private bool _interactablesSeeded;
    private readonly ZoneBannerOverlay _zoneBanner = new();
    private readonly WorldFeedbackOverlay _feedback = new();
    private readonly BossTrackerOverlay _bossTracker = new();
    private readonly BossHealthBarOverlay _bossHealthBar = new();
    private readonly HousingDecorateOverlay _housingDecorate = new();
    private readonly FishingMinigameOverlay _fishing = new();
    private readonly List<HouseState> _houses = [];
    private HousePlotZone? _hoveredDoorHouse;
    private Vector2? _lastInteractWorldPos;
    private string? _currentZoneId;
    private bool _zonePresenceInitialized;
    private WorldEventState? _lastWorldEvent;

    private Vector2 _camera;
    private Vector2 _moveDir;
    private float _inputAccum;
    private string _status = "Connected. WASD to move. [F] friends. Right-click players. Enter to chat. Space or click to attack. [E] to interact.";
    private string _interactPrompt = "";
    private InteractableEntity? _focused;
    private InteractableEntity? _hovered;
    private PlayerEntity? _hoveredPlayer;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private bool _wasWindowActive = true;
    private bool _debugHudVisible;
    private int? _pendingHotbarDragIndex;
    private Point _hotbarDragStartMouse;
    private float _interiorFade;
    private long _interiorHouseId;
    private HousePlotZone? _cachedInteriorHouse;

    private string[] _debugLines = [];

    public WorldScreen(ScreenManager screens)
    {
        _screens = screens;
        _hotbar.SlotActivated += OnHotbarSlot;
        _hotbar.CooldownBlocked += OnHotbarCooldownBlocked;
        _chat.Submitted += OnChatSubmitted;
        _chat.TypingChanged += OnChatTypingChanged;
        _deathOverlay.NewLifeRequested += OnNewLifeRequested;
        _playerContextMenu.ItemChosen += OnPlayerContextMenu;
    }

    public void OnEnter()
    {
        var net = _screens.Net;
        net.Snapshot += OnWorldSnapshot;
        net.NpcHit += OnNpcHit;
        net.WorldEvent += OnWorldEvent;
        net.BossSpawn += OnBossSpawn;
        net.BossDeath += OnBossDeath;
        net.BossAction += OnBossAction;
        net.HouseBuilt += OnHouseBuilt;
        net.HouseRemoved += OnHouseRemoved;
        net.HouseUpdated += OnHouseUpdated;
        net.InventoryUpdated += OnInventoryUpdated;
        net.WorldItemRemoved += OnWorldItemRemoved;
        net.WorldItemAdded += OnWorldItemAdded;
        net.ServerError += OnServerError;
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
        net.SkillXpGain += OnSkillXpGain;
        net.FriendsUpdated += OnFriendsUpdated;
        net.PrivateMessage += OnPrivateMessage;

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

        _skills.ApplySnapshot(net.SpawnSkills, net.SpawnTotalXp);
        _inventory.ApplyFromServer(net.SpawnInventory);
        SyncLocalStatsFromSkills();
        DeathbornGame.Instance.IsMouseVisible = false;

        WorldZones.Initialize(WorldMap.Realik);
        WorldFoliage.Initialize(WorldMap.Realik);
        SeedWorldTownInteractables();
        SeedHotbar();
        TextField.ReleaseFocus();

        _windows.Character.Bind(
            () => _players.TryGetValue(net.LocalCharacterId, out var p) ? p.Stats : null,
            () => _players.TryGetValue(net.LocalCharacterId, out var p) ? p.Name : net.SpawnName,
            () => _skills);
        _windows.SpellBook.Bind(_dragDrop);
        _windows.SpellBook.AbilityClicked += OnSpellBookAbilityClicked;
        _windows.Inventory.Bind(_dragDrop, _inventory);
        _windows.Inventory.SlotClicked += OnInventorySlotClicked;
        _windows.Skills.Bind(() => _skills);
        _windows.Friends.Bind(
            _friends,
            () => _screens.Net.LocalCharacterId,
            (targetId, text) => _screens.Net.SendPm(targetId, text),
            (fromAccountId, accept) => _screens.Net.SendFriendRespond(fromAccountId, accept),
            friendAccountId => _screens.Net.SendFriendRemove(friendAccountId));
        _screens.SetOpenCharacterHandler(() => _windows.OpenCharacter());
        _screens.SetOpenSpellBookHandler(() => _windows.OpenSpellBook());
        _screens.SetOpenInventoryHandler(() => _windows.OpenInventory());
        _screens.SetOpenSkillsHandler(() => _windows.OpenSkills());
        _screens.SetBuildHouseHandler(TryBuildHouse);
        UpdateBuildHouseEnabled();
        _fishing.Caught += OnFishCaught;
        _fishing.Cancelled += () => _status = "Fishing cancelled.";

        MusicPlayer.PlayPlaylist(DeathbornGame.Instance.Content, GameMusic.Get(GameMusic.StartingArea));
    }

    public void OnExit()
    {
        MusicPlayer.Stop();

        var net = _screens.Net;
        net.Snapshot -= OnWorldSnapshot;
        net.NpcHit -= OnNpcHit;
        net.WorldEvent -= OnWorldEvent;
        net.BossSpawn -= OnBossSpawn;
        net.BossDeath -= OnBossDeath;
        net.BossAction -= OnBossAction;
        net.HouseBuilt -= OnHouseBuilt;
        net.HouseRemoved -= OnHouseRemoved;
        net.HouseUpdated -= OnHouseUpdated;
        net.InventoryUpdated -= OnInventoryUpdated;
        net.WorldItemRemoved -= OnWorldItemRemoved;
        net.WorldItemAdded -= OnWorldItemAdded;
        net.ServerError -= OnServerError;
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
        net.SkillXpGain -= OnSkillXpGain;
        net.FriendsUpdated -= OnFriendsUpdated;
        net.PrivateMessage -= OnPrivateMessage;
        _deathOverlay.NewLifeRequested -= OnNewLifeRequested;
        _playerContextMenu.ItemChosen -= OnPlayerContextMenu;
        _fishing.Caught -= OnFishCaught;
        _chat.Submitted -= OnChatSubmitted;
        _chat.TypingChanged -= OnChatTypingChanged;
        if (_chat.IsOpen) _chat.Close(submit: false);
        _hotbar.SlotActivated -= OnHotbarSlot;
        _hotbar.CooldownBlocked -= OnHotbarCooldownBlocked;
        _windows.SpellBook.AbilityClicked -= OnSpellBookAbilityClicked;
        _windows.Inventory.SlotClicked -= OnInventorySlotClicked;
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
        _bosses.Clear();
        _feedback.Clear();
        _lastInteractWorldPos = null;
        _lastWorldEvent = null;
        DeathbornGame.Instance.IsMouseVisible = true;
    }

    public IReadOnlyList<string> DebugInfoLines => _debugLines;

    public bool HandleEscape()
    {
        if (_playerContextMenu.IsOpen)
        {
            _playerContextMenu.Close();
            return true;
        }

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

        if (windowActive && InputKeys.EnterPressed(kb, _prevKb) && !_chat.IsOpen && !_windows.Friends.IsPmFocused)
            _chat.Open();

        _chat.Update(gameTime, kb, _prevKb);
        _windows.Friends.TickInput(gameTime, kb, _prevKb);

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

        if (windowActive && !chatOpen && kb.IsKeyDown(Keys.M) && !_prevKb.IsKeyDown(Keys.M) && InteriorHouse() == null)
            _worldMap.Toggle();

        if (menuOpen && _worldMap.IsOpen)
            _worldMap.Close();

        var localEntity = FindLocalPlayer();
        var abilityBusy = localEntity is { IsBusy: true };
        var inputBlocked = chatOpen || menuOpen || abilityBusy || IsLocalDyingOrDead();

        _buffTracker.Update(dt);
        _inventory.Update(dt);
        UpdateHunterMarks(dt);
        _zoneBanner.Update(dt);
        WaterTiles.Update(dt);
        WorldFoliage.Update(dt);
        UpdateZonePresence(localEntity);

        var decorateActive = _housingDecorate.IsActive;
        var fishingActive = _fishing.IsActive;
        var blockGameplay = inputBlocked || decorateActive || fishingActive;
        var allowWindowShortcuts = windowActive && !chatOpen && !decorateActive && !fishingActive;
        var buffCapturesMouse = _buffBar.Update(mouse, _prevMouse, _buffTracker);
        var uiCapturesMouse = _windows.Update(mouse, _prevMouse, kb, _prevKb, allowWindowShortcuts) || buffCapturesMouse;
        var contextCapturesMouse = _playerContextMenu.Update(mouse, _prevMouse, blockGameplay || uiCapturesMouse);
        uiCapturesMouse |= contextCapturesMouse;

        UpdateHousing(localEntity, kb, _prevKb, mouse, blockGameplay, uiCapturesMouse);
        if (fishingActive)
            _fishing.Update(dt, kb, _prevKb, mouse, _prevMouse);

        _moveDir = blockGameplay ? Vector2.Zero : ReadMoveDir(kb);
        var wantsRun = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
        if (localEntity is { IsDead: false })
        {
            var canRun = localEntity.Stats.Stamina >= Config.MinStaminaToRun;
            localEntity.IsRunning = wantsRun && canRun && _moveDir.LengthSquared() > 0.0001f;
            localEntity.InputDir = _moveDir;

            localEntity.Stats.TickRegen(dt, localEntity.IsRunning);
            if (localEntity.IsRunning)
            {
                localEntity.Stats.Stamina = MathF.Max(0f,
                    localEntity.Stats.Stamina - Config.RunStaminaDrainPerSecond * dt);
            }

            if (_moveDir.LengthSquared() > 0.0001f)
                localEntity.AimDir = PlayerEntity.CardinalFacing(_moveDir);
            else if (windowActive && !inputBlocked)
                UpdateLocalAimFacing(localEntity, mouse.Position, _prevMouse.Position);
        }

        if (blockGameplay || IsLocalDyingOrDead())
        {
            if (_lastSentDir.LengthSquared() > 0.0001f || _lastSentRunning)
            {
                _lastSentDir = Vector2.Zero;
                _lastSentRunning = false;
                _screens.Net.SendInput(0, 0, false);
            }
        }
        else
        {
            var running = localEntity?.IsRunning == true;
            _inputAccum += dt;
            if (_inputAccum >= Config.InputSendInterval
                || Vector2.DistanceSquared(_moveDir, _lastSentDir) > 0.0001f
                || running != _lastSentRunning)
            {
                _inputAccum = 0;
                _lastSentDir = _moveDir;
                _lastSentRunning = running;
                _screens.Net.SendInput(_moveDir.X, _moveDir.Y, running);
            }
        }

        foreach (var p in _players.Values) p.Update(dt);
        foreach (var b in _bosses.Values) b.Update(dt);
        ProcessDeathWatch();

        if (localEntity is { IsDead: false })
        {
            localEntity.CheckLocalMeleeHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localEntity.Id, targetId, damage, ability));
            localEntity.CheckLocalBossMeleeHits(_bosses, (targetId, damage, ability) =>
                ReportAbilityHitNpc(localEntity.Id, targetId, damage, ability));
            localEntity.CheckWhirlwindHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localEntity.Id, targetId, damage, ability));
            localEntity.CheckWhirlwindBossHits(_bosses, (targetId, damage, ability) =>
                ReportAbilityHitNpc(localEntity.Id, targetId, damage, ability));
            localEntity.CheckDashHits(_players, (targetId, damage, ability) =>
                ReportAbilityHit(localEntity.Id, targetId, damage, ability));
            localEntity.CheckDashBossHits(_bosses, (targetId, damage, ability) =>
                ReportAbilityHitNpc(localEntity.Id, targetId, damage, ability));
        }

        UpdateProjectiles(dt);
        _feedback.Update(dt, _players, _bosses);
        _hotbar.Update(dt, kb, _prevKb, acceptInput: !blockGameplay && !_dragDrop.IsDragging);

        if (_ghostMode && _ghost != null)
            _camera = _ghost.Position;
        else if (localEntity != null)
            _camera = localEntity.Position;

        UpdateInteriorFade(dt, localEntity);

        if (!blockGameplay && windowActive && !uiCapturesMouse && !_dragDrop.IsDragging)
        {
            UpdateInteractFocus(mouse.Position);
            UpdatePlayerHover(mouse.Position);
            UpdateInteractPrompt();

            if (!_housingDecorate.IsActive)
            {
                if (kb.IsKeyDown(Keys.E) && !_prevKb.IsKeyDown(Keys.E))
                    TryInteractNearest();

                if (kb.IsKeyDown(Keys.Space) && !_prevKb.IsKeyDown(Keys.Space))
                    TryDefaultAttack();

                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released
                    && !IsOverHotbar(mouse.Position))
                    HandleLeftClick(mouse.Position);

                if (mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released
                    && !IsOverHotbar(mouse.Position) && !_playerContextMenu.IsOpen)
                    HandleRightClick(mouse.Position);
            }
        }
        else if (!blockGameplay && windowActive)
        {
            UpdatePlayerHover(mouse.Position);
        }
        else
        {
            _hoveredPlayer = null;
        }

        UpdateDragDrop(mouse, _prevMouse, uiCapturesMouse);

        _debugLines =
        [
            _status,
            $"Pos: ({(int)_camera.X}, {(int)_camera.Y})  Input: ({_moveDir.X:+#0.0;-#0.0;+0.0}, {_moveDir.Y:+#0.0;-#0.0;+0.0})  {MovementLabel(localEntity)}",
            $"id={_screens.Net.LocalCharacterId}  players={_players.Count}  ws={(_screens.Net.WsConnected ? "open" : "closed")}",
        ];

        _prevKb = kb;
        if (windowActive)
            _prevMouse = mouse;
    }

    private Vector2 _lastSentDir;
    private bool _lastSentRunning;

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;

        var zoom = GameViewport.WorldZoom;
        var interiorHouse = InteriorHouse();

        sb.Begin(samplerState: SamplerState.PointClamp);

        if (interiorHouse != null)
            DrawInteriorWorld(sb, font, interiorHouse, zoom);
        else
            DrawExteriorWorld(sb, font, zoom);

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
        {
            var hotbarTop = GameViewport.Height - Hotbar.SlotHeight - 28;
            var promptY = hotbarTop - 14 - font.LineSpacing;
            SpriteFontSafe.DrawString(sb, font, _interactPrompt,
                new Vector2(GameViewport.Width / 2f - 200, promptY), new Color(220, 220, 180));
        }

        if (!_ghostMode)
        {
            _hotbar.Draw(sb, font, _inventory);
            if (!IsLocalDyingOrDead() && !_dragDrop.IsDragging)
                DrawHotbarTooltip(sb, font);
            if (!IsLocalDyingOrDead() && interiorHouse == null)
            {
                _buffBar.Draw(sb, font, _buffTracker);
                _minimap.Draw(sb, font, _camera, _screens.Net.LocalCharacterId, _players.Values, _bosses.Values,
                    LocalHomestead());
            }
            else if (!IsLocalDyingOrDead())
                _buffBar.Draw(sb, font, _buffTracker);
        }
        _windows.Draw(sb, font);
        _playerContextMenu.Draw(sb, font);

        if (!_ghostMode)
        {
            if (interiorHouse == null)
                _zoneBanner.Draw(sb, font);
            _housingDecorate.Draw(sb, font);
            _fishing.Draw(sb, font);
        }

        if (_dragDrop.IsDragging)
            _dragDrop.DrawGhost(sb, font, Mouse.GetState().Position);

        _feedback.DrawScreen(sb, font);

        if (interiorHouse != null && _interiorFade < 1f)
        {
            var alpha = (int)(255 * (1f - _interiorFade));
            DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), new Color(0, 0, 0, alpha));
        }

        sb.End();

        DrawChatOverlay(sb, font, zoom);
        if (interiorHouse == null)
            DrawWorldMapOverlay(sb, font);

        if (_ghostMode)
        {
            sb.Begin();
            _deathOverlay.Draw(sb, font);
            sb.End();
        }

        DrawCustomCursor(sb);
    }

    private void DrawExteriorWorld(SpriteBatch sb, SpriteFont font, float zoom)
    {
        _bg.Draw(sb, _camera, ScreenCenter, zoom);
        WorldFoliage.Draw(sb, WorldMap.Realik, _camera, ScreenCenter, zoom);
        TownRenderer.Draw(sb, _camera, ScreenCenter, zoom);
        HouseRenderer.Draw(sb, _camera, ScreenCenter, zoom, WorldZones.Houses);
        HouseRenderer.DrawDoorHighlights(sb, _camera, ScreenCenter, zoom, WorldZones.Houses, _hoveredDoorHouse);

        foreach (var obj in _interactables)
            obj.Draw(sb, font, WorldToScreen(obj.Position), zoom);

        foreach (var effect in _effects.Where(e => e.DrawUnderEntities))
            effect.Draw(sb, WorldToScreen(effect.Position), zoom);

        foreach (var corpse in _corpses)
            corpse.Draw(sb, WorldToScreen(corpse.Position), zoom);

        DrawPlayers(sb, font, zoom, houseId: null);

        foreach (var b in _bosses.Values.OrderBy(b => b.Position.Y))
            b.Draw(sb, font, WorldToScreen(b.Position), zoom);

        if (_ghostMode && _ghost != null)
            _ghost.Draw(sb, WorldToScreen(_ghost.Position), zoom);

        foreach (var effect in _effects.Where(e => !e.DrawUnderEntities))
            effect.Draw(sb, WorldToScreen(effect.Position), zoom);

        _feedback.DrawWorld(sb, font, WorldToScreen, zoom, _players, _bosses);

        if (!_ghostMode && !IsLocalDyingOrDead())
        {
            _bossTracker.Draw(sb, font, _camera, _bosses.Values);
            _bossHealthBar.Draw(sb, font, _camera, _bosses.Values);
        }
    }

    private void DrawInteriorWorld(SpriteBatch sb, SpriteFont font, HousePlotZone house, float zoom)
    {
        var local = FindLocalPlayer();
        var exitHighlight = local != null && HousingConstants.IsNearInteriorExit(local.Position, house.Center);
        HouseInteriorRenderer.Draw(sb, font, house, ScreenCenter, zoom, exitHighlight,
            house.DisplayName(_screens.Net.LocalCharacterId));

        DrawPlayers(sb, font, zoom, house.Id);

        foreach (var effect in _effects.Where(e => e.DrawUnderEntities))
            effect.Draw(sb, WorldToScreen(effect.Position), zoom);

        foreach (var effect in _effects.Where(e => !e.DrawUnderEntities))
            effect.Draw(sb, WorldToScreen(effect.Position), zoom);

        _feedback.DrawWorld(sb, font, WorldToScreen, zoom, _players, _bosses);
    }

    private void DrawPlayers(SpriteBatch sb, SpriteFont font, float drawZoom, long? houseId)
    {
        IEnumerable<PlayerEntity> players = _players.Values;
        if (houseId is long hid)
            players = players.Where(p => p.InsideHouseId == hid);
        else
            players = players.Where(p => p.InsideHouseId <= 0);

        foreach (var p in players.OrderBy(p => p.Position.Y))
        {
            var screenPos = WorldToScreen(p.Position);
            if (p == _hoveredPlayer)
                p.DrawHoverHighlight(sb, screenPos, drawZoom);
            p.Draw(sb, font, screenPos, drawZoom);
            if (_hunterMarks.ContainsKey(p.Id))
                PlayerEntity.DrawHunterMark(sb, screenPos, drawZoom);
        }
    }

    private void UpdateInteriorFade(float dt, PlayerEntity? local)
    {
        var insideId = local?.InsideHouseId ?? 0;
        if (insideId != _interiorHouseId)
        {
            _interiorHouseId = insideId;
            _interiorFade = 0f;
        }

        if (insideId > 0)
            _interiorFade = MathF.Min(1f, _interiorFade + dt * 2.8f);
    }

    private HousePlotZone? InteriorHouse()
    {
        var local = FindLocalPlayer();
        if (local is not { InsideHouseId: > 0 })
        {
            _cachedInteriorHouse = null;
            return null;
        }

        var id = local.InsideHouseId;
        var house = WorldZones.Houses.FirstOrDefault(h => h.Id == id);
        if (house != null)
            _cachedInteriorHouse = house;
        return house ?? _cachedInteriorHouse;
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
            _feedback.SpawnDeath(playerId, player.Name, player.IsLocal);
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
        _inventory.ApplyFromServer([]);
        UpdateBuildHouseEnabled();
        _housingDecorate.Deactivate();

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

    private void UpdateDragDrop(MouseState mouse, MouseState prevMouse, bool uiCapturesMouse)
    {
        if (_ghostMode) return;

        if (!_dragDrop.IsDragging && !uiCapturesMouse)
        {
            if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
            {
                if (Hotbar.TryGetSlotIndexAt(mouse.Position, out var idx)
                    && _hotbar.Slots[idx].Entry != null)
                {
                    _pendingHotbarDragIndex = idx;
                    _hotbarDragStartMouse = mouse.Position;
                }
            }

            if (_pendingHotbarDragIndex is int pending && mouse.LeftButton == ButtonState.Pressed)
            {
                var dx = mouse.X - _hotbarDragStartMouse.X;
                var dy = mouse.Y - _hotbarDragStartMouse.Y;
                if (dx * dx + dy * dy > 36)
                {
                    var entry = _hotbar.Slots[pending].Entry!;
                    _dragDrop.BeginHotbar(entry, pending);
                    _pendingHotbarDragIndex = null;
                }
            }
        }

        if (mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed)
        {
            if (!_dragDrop.IsDragging && !IsLocalDyingOrDead() && _pendingHotbarDragIndex is int pending)
            {
                var dx = mouse.X - _hotbarDragStartMouse.X;
                var dy = mouse.Y - _hotbarDragStartMouse.Y;
                if (dx * dx + dy * dy <= 36
                    && Hotbar.TryGetSlotIndexAt(mouse.Position, out var idx) && idx == pending)
                    _hotbar.TryActivate(pending);
            }
            _pendingHotbarDragIndex = null;
        }

        if (!_dragDrop.IsDragging) return;

        if (mouse.LeftButton != ButtonState.Released || prevMouse.LeftButton != ButtonState.Pressed)
            return;

        var payload = _dragDrop.Active!;
        if (_windows.Inventory.IsOpen && _windows.Inventory.TryGetSlotAt(mouse.Position, out var inventoryIdx))
        {
            if (payload.Kind == DragPayloadKind.Item && payload.SourceInventoryIndex >= 0)
            {
                if (payload.SourceInventoryIndex != inventoryIdx)
                {
                    _inventory.MoveSlot(payload.SourceInventoryIndex, inventoryIdx);
                    _screens.Net.SendInventoryMove(payload.SourceInventoryIndex, inventoryIdx);
                }
            }
        }
        else if (Hotbar.TryGetSlotIndexAt(mouse.Position, out var hotbarIdx))
        {
            var replaced = _hotbar.Slots[hotbarIdx].Entry;
            var newEntry = payload.Kind switch
            {
                DragPayloadKind.Ability => AbilityCatalog.ToHotbarEntry(payload.AbilityId!),
                DragPayloadKind.Item => ItemCatalog.ToHotbarEntry(payload.ItemId!, payload.SourceInventoryIndex),
                DragPayloadKind.Hotbar => payload.HotbarEntry,
                _ => null,
            };
            _hotbar.AssignSlot(hotbarIdx, newEntry);

            if (payload.Kind == DragPayloadKind.Hotbar && payload.SourceHotbarIndex >= 0)
            {
                if (payload.SourceHotbarIndex == hotbarIdx)
                    _hotbar.AssignSlot(hotbarIdx, newEntry);
                else
                    _hotbar.AssignSlot(payload.SourceHotbarIndex, replaced);
            }

            _status = newEntry?.GetValueOrDefault("name") is string n
                ? $"Assigned {n} to hotbar slot {Hotbar.KeyLabels[hotbarIdx]}."
                : $"Hotbar slot {Hotbar.KeyLabels[hotbarIdx]} updated.";
        }
        else if (TryDropPayloadInWorld(payload, mouse.Position))
        {
            // dropped on ground
        }
        else if (payload.Kind == DragPayloadKind.Hotbar && payload.SourceHotbarIndex >= 0
                 && !Hotbar.TryGetSlotIndexAt(mouse.Position, out _)
                 && !_windows.IsPointOverOpenWindow(mouse.Position)
                 && !IsOverHotbar(mouse.Position))
        {
            _hotbar.AssignSlot(payload.SourceHotbarIndex, null);
            _status = "Removed ability from hotbar.";
        }

        _dragDrop.End();
    }

    private void DrawHotbarTooltip(SpriteBatch sb, SpriteFont font)
    {
        if (!Hotbar.TryGetSlotIndexAt(Mouse.GetState().Position, out var idx)) return;
        var entry = _hotbar.Slots[idx].Entry;
        if (entry == null) return;

        AbilityTooltipDraw.DrawHotbarEntry(sb, font, entry, Hotbar.GetSlotBounds(idx),
            new Point(GameViewport.Width, GameViewport.Height));
    }

    private void DrawCustomCursor(SpriteBatch sb)
    {
        var mouse = Mouse.GetState().Position;
        var cursorKind = UiCursorKind.Normal;
        Rectangle? hoverSlotRect = null;

        if (Hotbar.TryGetSlotIndexAt(mouse, out var hotbarIdx))
        {
            hoverSlotRect = Hotbar.GetSlotBounds(hotbarIdx);
            var entry = _hotbar.Slots[hotbarIdx].Entry;
            if (entry != null)
                cursorKind = IsEntryBlocked(entry, hotbarIdx, null) ? UiCursorKind.Blocked : UiCursorKind.Hover;
        }
        else if (_windows.Inventory.IsOpen && _windows.Inventory.TryGetSlotAt(mouse, out var invIdx, out var invRect))
        {
            hoverSlotRect = invRect;
            var slot = _inventory.Slots[invIdx];
            if (!slot.IsEmpty && slot.ItemId != null)
            {
                var entry = ItemCatalog.ToHotbarEntry(slot.ItemId, invIdx);
                cursorKind = IsEntryBlocked(entry, null, invIdx) ? UiCursorKind.Blocked : UiCursorKind.Hover;
            }
        }
        else if (_hovered != null || _hoveredDoorHouse != null)
        {
            cursorKind = UiCursorKind.Hover;
        }

        sb.Begin(samplerState: SamplerState.PointClamp);
        if (hoverSlotRect is { } rect)
            UiCursorTheme.DrawSlotOverlay(sb, rect);
        UiCursorTheme.DrawCursor(sb, mouse, cursorKind);
        sb.End();
    }

    private bool IsEntryBlocked(Dictionary<string, object> entry, int? hotbarIndex, int? inventoryIndex)
    {
        if (hotbarIndex is int hi && _hotbar.Slots[hi].IsOnCooldown)
            return true;
        if (inventoryIndex is int ii && _inventory.Slots[ii].IsOnCooldown)
            return true;

        if (entry.GetValueOrDefault("fromInventory") is true && !HasLinkedInventoryItem(entry))
            return true;

        var id = entry.GetValueOrDefault(HotbarEntry.IdKey) as string;
        var info = id != null ? AbilityCatalog.Get(id) : null;
        var local = FindLocalPlayer();
        if (info != null && local != null && !AbilityResourceCosts.CanAfford(local.Stats, info))
            return true;

        return false;
    }

    private bool HasLinkedInventoryItem(Dictionary<string, object> entry)
    {
        if (entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
        {
            var slot = slotObj switch
            {
                int i => i,
                long l => (int)l,
                _ => -1,
            };
            if (slot >= 0)
                return _inventory.CountAt(slot) > 0;
        }

        var itemId = entry.GetValueOrDefault("itemId") as string
            ?? entry.GetValueOrDefault(HotbarEntry.IdKey) as string;
        return itemId != null && _inventory.CountOf(itemId) > 0;
    }

    private static bool IsOverHotbar(Point p)
    {
        Hotbar.GetBarLayout(out var x0, out var y);
        var totalW = 10 * Hotbar.SlotWidth + 9 * Hotbar.SlotGap;
        var bar = new Rectangle(x0 - Hotbar.BarPadding, y - Hotbar.BarPadding,
            totalW + Hotbar.BarPadding * 2, Hotbar.SlotHeight + Hotbar.BarPadding * 2);
        return bar.Contains(p);
    }

    private void OnNewLifeRequested() => _screens.Change(new CharacterCreateScreen(_screens));

    private void DrawWorldMapOverlay(SpriteBatch sb, SpriteFont font)
    {
        if (!_worldMap.IsOpen) return;
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return;

        sb.Begin(samplerState: SamplerState.PointClamp);
        _worldMap.Draw(sb, font, local.Position, _bosses.Values, LocalHomestead());
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

    private static string MovementLabel(PlayerEntity? local)
    {
        if (local == null || !local.IsMoving) return "idle";
        return local.IsRunning ? "running" : "walking";
    }

    private bool TryPayAbilityCost(string abilityId)
    {
        var local = FindLocalPlayer();
        if (local == null) return false;
        var info = AbilityCatalog.Get(abilityId);
        if (info == null) return true;
        if (AbilityResourceCosts.TrySpend(local.Stats, info, out var message))
            return true;
        _status = message;
        return false;
    }

    private Vector2 ScreenCenter => GameViewport.Center;

    private Vector2 WorldToScreen(Vector2 world)
    {
        if (FindLocalPlayer() is { InsideHouseId: > 0 })
            return ScreenCenter + (world - _camera) * GameViewport.WorldZoom;
        return (world - _camera) * GameViewport.WorldZoom + ScreenCenter;
    }

    private Vector2 ScreenToWorld(Point screen)
    {
        if (FindLocalPlayer() is { InsideHouseId: > 0 })
            return _camera + (new Vector2(screen.X, screen.Y) - ScreenCenter) / GameViewport.WorldZoom;
        return (new Vector2(screen.X, screen.Y) - ScreenCenter) / GameViewport.WorldZoom + _camera;
    }

    private void UpdateInteractFocus(Point mouseScreen)
    {
        var mouseWorld = ScreenToWorld(mouseScreen);
        var nearest = FindNearestInRange(_camera);
        var underMouse = FindAtPoint(mouseWorld);
        _hoveredDoorHouse = InteriorHouse() == null
            ? HousingConstants.FindDoorAt(WorldZones.Houses, mouseWorld)
            : null;

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
        var local = FindLocalPlayer();
        if (local is { InsideHouseId: > 0 })
        {
            var house = InteriorHouse();
            if (house != null && HousingConstants.IsNearInteriorExit(local.Position, house.Center))
                _interactPrompt = "Walk into the door to exit  (or [E])";
            else
                _interactPrompt = "Walk to the bottom door to leave";
            return;
        }

        if (_hoveredDoorHouse != null && Vector2.Distance(_camera, HousingConstants.DoorWorldPosition(_hoveredDoorHouse.Center)) <= Config.InteractRange)
        {
            _interactPrompt = $"Walk into the door to enter {_hoveredDoorHouse.DisplayName(_screens.Net.LocalCharacterId)}";
            return;
        }

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

    private PlayerEntity? FindPlayerAtPoint(Vector2 worldPos)
    {
        var pickRadius = PlayerEntity.Radius * 1.5f;
        PlayerEntity? best = null;
        var bestDist = float.MaxValue;
        var interiorId = InteriorHouse()?.Id;
        foreach (var p in _players.Values)
        {
            if (p.IsLocal || p.IsDead) continue;
            if (interiorId is long hid)
            {
                if (p.InsideHouseId != hid) continue;
            }
            else if (p.InsideHouseId > 0)
            {
                continue;
            }
            var d = Vector2.DistanceSquared(p.Position, worldPos);
            if (d > pickRadius * pickRadius) continue;
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    private void UpdatePlayerHover(Point mouseScreen)
    {
        _hoveredPlayer = FindPlayerAtPoint(ScreenToWorld(mouseScreen));
    }

    private void HandleRightClick(Point mouseScreen)
    {
        var player = FindPlayerAtPoint(ScreenToWorld(mouseScreen));
        if (player == null) return;

        var canAdd = !_friends.IsFriendCharacter(player.Id) && !_friends.HasPendingOutCharacter(player.Id);
        _playerContextMenu.Open(mouseScreen, player.Id, player.Name, canAdd, canWhisper: true);
    }

    private void OnPlayerContextMenu(PlayerContextAction action, long characterId, string name)
    {
        switch (action)
        {
            case PlayerContextAction.AddFriend:
                _screens.Net.SendFriendAdd(characterId);
                _status = $"Friend request sent to {name}.";
                break;
            case PlayerContextAction.Whisper:
                _windows.Friends.OpenWhisper(characterId, name);
                break;
        }
    }

    private void OnFriendsUpdated(FriendsData data) => _friends.Apply(data);

    private void OnPrivateMessage(PmData data)
    {
        _friends.AddMessage(data, _screens.Net.LocalCharacterId);
        if (!data.Outgoing)
        {
            _status = $"Whisper from {data.FromName}: {data.Text}";
            if (!_windows.Friends.IsOpen)
                _windows.Friends.OpenWhisper(data.FromCharacterId, data.FromName);
        }
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
        if (TryHouseDoorInteract()) return;
        var target = FindNearestInRange(_camera);
        if (target == null) { _status = "Nothing in range to interact with."; return; }
        PerformInteract(target);
    }

    private void HandleLeftClick(Point mouseScreen)
    {
        var world = ScreenToWorld(mouseScreen);
        if (TryHouseDoorInteract(world)) return;
        var target = FindAtPoint(world);
        if (target != null && target.IsInRange(_camera))
            PerformInteract(target);
        else
            TryMeleeAttack(GetAimDirection());
    }

    private bool TryHouseDoorInteract(Vector2? worldPos = null)
    {
        var local = FindLocalPlayer();
        if (local == null) return false;
        var world = worldPos ?? _camera;

        if (local.InsideHouseId > 0)
        {
            var house = InteriorHouse();
            if (house == null) return false;
            if (!HousingConstants.IsNearInteriorExit(local.Position, house.Center)) return false;
            _screens.Net.SendHouseExit();
            _status = "Leaving homestead...";
            return true;
        }

        var doorHouse = HousingConstants.FindDoorAt(WorldZones.Houses, world);
        if (doorHouse == null) return false;
        if (Vector2.Distance(_camera, HousingConstants.DoorWorldPosition(doorHouse.Center)) > Config.InteractRange)
            return false;

        _screens.Net.SendHouseEnter(doorHouse.Id);
        _status = $"Entering {doorHouse.DisplayName(_screens.Net.LocalCharacterId)}...";
        return true;
    }

    private void TryDefaultAttack() => TryMeleeAttack(GetAimDirection());

    private void TryMeleeAttack(Vector2 aimDir)
    {
        if (!TryPayAbilityCost("slash")) return;
        TryMeleeAttackInternal(aimDir);
    }

    private bool TryMeleeAttackInternal(Vector2 aimDir)
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (!local.StartAttack(aimDir)) return false;
        _screens.Net.SendPlayerAction(PlayerActions.MeleeAttack, aimDir.X, aimDir.Y);
        return true;
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

    private bool CastProjectileSpell(string abilityId, ProjectileDefinition def, ProjectileStyle style, float castLock, string status)
    {
        if (!TryPayAbilityCost(abilityId)) return false;
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
            "fireball",
            definition ?? ProjectileDefinitions.Fireball,
            ProjectileStyle.Fire,
            Config.FireballCastLockDuration,
            "You cast Fireball.");

    private bool CastIceShard() =>
        CastProjectileSpell(
            "ice_shard",
            ProjectileDefinitions.IceShard,
            ProjectileStyle.Ice,
            Config.IceShardCastLockDuration,
            "You cast Ice Shard.");

    private bool CastArcBolt()
    {
        if (!TryPayAbilityCost("arc_bolt")) return false;
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

    private bool CastBloodBolt()
    {
        if (!TryPayAbilityCost("blood_bolt")) return false;
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (local.IsBusy) return false;

        var dir = GetAimDirection();
        local.StartAbilityLock(Config.BloodBoltCastLockDuration);
        local.MoveDir = dir;

        var target = FindArcBoltTarget(local.Position, dir, local.Id);
        if (target != null)
        {
            _effects.Add(new ArcBoltEffect(local.Position, target.Position, local.Id, "blood_bolt"));
            ReportAbilityHit(local.Id, target.Id, Config.BloodBoltDamage, "blood_bolt");
        }
        else
        {
            var end = local.Position + dir * Config.ArcBoltRange;
            _effects.Add(new ArcBoltEffect(local.Position, end, local.Id, "blood_bolt"));
        }

        _screens.Net.SendCastSpell("blood_bolt", dir.X, dir.Y);
        _status = target != null ? "Blood Bolt tears through your foe!" : "Blood Bolt fizzles.";
        return true;
    }

    private bool CastPoisonCloud()
    {
        if (!TryPayAbilityCost("poison_cloud")) return false;
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
        if (!TryPayAbilityCost("bandage")) return false;
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        if (local.IsBandaging) return false;

        _screens.Net.SendAbilityUse("bandage");
        local.StartBandageHoT();
        _status = "Bandage applied — healing over time.";
        return true;
    }

    private bool CastShieldBash()
    {
        if (!TryPayAbilityCost("shield_bash")) return false;
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return false;
        var dir = GetAimDirection();
        if (!local.StartMeleeAbility(MeleeAbilityDefinitions.ShieldBash, dir)) return false;
        _screens.Net.SendCastSpell("shield_bash", dir.X, dir.Y);
        _status = "Shield Bash!";
        return true;
    }

    private bool CastWhirlwind()
    {
        if (!TryPayAbilityCost("whirlwind")) return false;
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
        if (!TryPayAbilityCost("warrior_dash")) return false;
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
        if (!TryPayAbilityCost("battle_shout")) return false;
        _screens.Net.SendAbilityUse("battle_shout");
        _status = "Battle Shout — damage increased!";
        return true;
    }

    private bool UseIronSkin()
    {
        if (!TryPayAbilityCost("iron_skin")) return false;
        _screens.Net.SendAbilityUse("iron_skin");
        _status = "Iron Skin — damage reduced!";
        return true;
    }

    private bool CastHunterMark()
    {
        if (!TryPayAbilityCost("hunter_mark")) return false;
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
        if (!TryPayAbilityCost("second_wind")) return false;
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
            case PlayerActions.CastBloodBolt:
                if (_players.TryGetValue(data.PlayerId, out var caster))
                {
                    var abilityId = data.Action == PlayerActions.CastBloodBolt ? "blood_bolt" : "arc_bolt";
                    var target = FindArcBoltTarget(caster.Position, dir, caster.Id);
                    var end = target?.Position ?? caster.Position + dir * Config.ArcBoltRange;
                    _effects.Add(new ArcBoltEffect(caster.Position, end, data.PlayerId, abilityId));
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
            effect.Update(dt, _players, _bosses, _interactables, ownerId == localId,
                (targetId, damage) => ReportAbilityHit(ownerId, targetId, damage, effect.AbilityId),
                (npcId, damage) => ReportAbilityHitNpc(ownerId, npcId, damage, effect.AbilityId));
        }

        ResolveProjectileClashes();
        _effects.RemoveAll(e => !e.Alive);
    }

    private void ReportAbilityHitNpc(long attackerId, long targetNpcId, int damage, string ability)
    {
        if (damage <= 0 || targetNpcId >= 0) return;
        if (!_bosses.ContainsKey(targetNpcId)) return;
        if (attackerId == _screens.Net.LocalCharacterId)
            _screens.Net.SendAbilityHitNpc(targetNpcId, damage, ability);
    }

    private void ReportAbilityHit(long attackerId, long targetId, int damage, string ability)
    {
        if (damage <= 0) return;
        if (targetId < 0)
        {
            ReportAbilityHitNpc(attackerId, targetId, damage, ability);
            return;
        }
        if (!_players.TryGetValue(attackerId, out var attacker)) return;
        if (!_players.TryGetValue(targetId, out var target)) return;
        if (!WorldZones.AllowPvP(attacker.Position, target.Position))
        {
            if (attackerId == _screens.Net.LocalCharacterId)
                _status = WorldZones.WorldBossEventActive
                    ? "PvP disabled during world boss event."
                    : "Cannot attack here — safe zone.";
            return;
        }

        if (attackerId == _screens.Net.LocalCharacterId)
            _screens.Net.SendAbilityHit(targetId, damage, ability);
    }

    private void UpdateZonePresence(PlayerEntity? local)
    {
        if (local == null || local.IsDead || _ghostMode) return;

        var localId = _screens.Net.LocalCharacterId;
        var town = WorldZones.ZoneAt(local.Position);
        var house = town == null ? WorldZones.HouseAt(local.Position) : null;

        string id;
        string name;
        bool safe;

        if (town != null)
        {
            id = town.Id;
            name = town.Name;
            safe = town.Safe;
        }
        else if (house != null)
        {
            id = house.ZoneId;
            name = house.DisplayName(localId);
            safe = true;
        }
        else
        {
            id = WorldZones.WildernessId;
            name = "The Wilderness";
            safe = false;
        }

        _zoneBanner.SetPersistentZone(safe ? name : null, safe);

        if (!_zonePresenceInitialized)
        {
            _zonePresenceInitialized = true;
            _currentZoneId = id;
            if (safe)
                _zoneBanner.ShowEnter(name, "PvP disabled - safe haven");
            return;
        }

        if (id == _currentZoneId) return;

        var wasSafe = _currentZoneId != WorldZones.WildernessId;
        _currentZoneId = id;

        if (safe)
        {
            _zoneBanner.ShowEnter(name, "PvP disabled - safe haven");
            _status = $"Entered {name}. PvP is off.";
        }
        else if (wasSafe)
        {
            _zoneBanner.ShowEnter("The Wilderness", "PvP enabled - watch your back");
            _status = "Left safe zone. PvP is enabled.";
        }
    }

    private void OnSkillXpGain(SkillXpGainData data)
    {
        var name = SkillDefinitions.DisplayName(data.SkillId);
        var isLocal = data.PlayerId == _screens.Net.LocalCharacterId;
        var isCombat = data.SkillId is "attack" or "strength" or "defense" or "hitpoints";
        Vector2? xpPos = isLocal && !isCombat ? _lastInteractWorldPos : null;

        if (isLocal)
        {
            _skills.ApplyGain(data.SkillId, data.Xp, data.TotalXp);
            if (data.HpMax > 0 && _players.TryGetValue(data.PlayerId, out var local))
                local.SyncStats((float)data.Hp, (float)data.HpMax);
            SyncLocalStatsFromSkills();
        }

        _feedback.SpawnSkillXp(data.PlayerId, name, data.Amount, data.LeveledUp, data.Level, xpPos);
        if (data.LeveledUp && isLocal)
            _feedback.SpawnLocalLevelUpBanner(name, data.Level);

        if (isLocal)
            _status = data.LeveledUp
                ? $"{name} level up! Now level {data.Level}."
                : $"+{data.Amount} {name} XP.";
    }

    private void SyncLocalStatsFromSkills()
    {
        if (!_players.TryGetValue(_screens.Net.LocalCharacterId, out var local)) return;
        local.Stats.Level = _skills.TotalLevel;
        var hpLevel = _skills.Level("hitpoints");
        var expectedMax = SkillDefinitions.HitpointsMax(hpLevel);
        if (local.Stats.HpMax < expectedMax)
        {
            local.Stats.HpMax = expectedMax;
            local.Stats.Hp = MathF.Min(local.Stats.Hp, local.Stats.HpMax);
        }
        local.Stats.ExpPercent = _skills.TotalXp > 0
            ? MathF.Min(100f, _skills.TotalXp / 1000f)
            : 0f;
    }

    private void OnPlayerHit(PlayerHitData data)
    {
        if (!_players.TryGetValue(data.TargetId, out var target)) return;
        if (data.HpMax > 0)
            target.SyncStats((float)data.Hp, (float)data.HpMax);
        target.ApplyHit(data.Damage);

        if (data.Damage > 0)
            _feedback.SpawnDamage(data.TargetId, data.Damage);
        else
            _feedback.SpawnMiss(data.TargetId);
    }

    private void OnPlayerHeal(PlayerHealData data)
    {
        if (!_players.TryGetValue(data.PlayerId, out var player)) return;
        if (data.HpMax > 0)
            player.SyncStats((float)data.Hp, (float)data.HpMax);

        _feedback.SpawnHeal(data.PlayerId, data.Amount);

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
        _lastInteractWorldPos = target.Position;
        if (target.Kind == InteractableKind.GroundItem && target.DropId > 0)
        {
            _screens.Net.SendPickupItem(target.DropId);
            _status = target.InteractMessage();
            return;
        }

        if (target.Kind == InteractableKind.Fishing)
        {
            _fishing.Start(target.Id, target.DisplayName, _skills.Level("fishing"));
            _status = $"Casting at {target.DisplayName}...";
            return;
        }

        _status = target.InteractMessage();
        _screens.Net.SendInteract(target.Id);
    }

    private void OnFishCaught(string spotId)
    {
        _lastInteractWorldPos = null;
        foreach (var spot in _interactables)
        {
            if (spot.Id == spotId)
            {
                _lastInteractWorldPos = spot.Position;
                break;
            }
        }
        _screens.Net.SendInteract(spotId);
        _status = "You caught a fish! +Fishing XP.";
    }

    private void OnWorldSnapshot(SnapshotData snap)
    {
        OnSnapshotPlayers(snap.Players);

        if (snap.WorldEvent != null)
            ApplyWorldEvent(snap.WorldEvent);

        _houses.Clear();
        if (snap.Houses != null)
        {
            _houses.AddRange(snap.Houses);
            ApplyHouseList();
        }

        SyncWorldItemInteractables(snap.WorldItems);

        var seen = new HashSet<long>();
        foreach (var s in snap.Npcs ?? [])
        {
            seen.Add(s.Id);
            var pos = new Vector2((float)s.X, (float)s.Y);
            if (!_bosses.TryGetValue(s.Id, out var b))
            {
                b = new BossEntity { Id = s.Id, Position = pos };
                _bosses[s.Id] = b;
            }
            b.Sync(s);
            b.SetTarget(pos);
        }

        foreach (var id in _bosses.Keys.Where(id => !seen.Contains(id)).ToList())
            _bosses.Remove(id);
    }

    private void OnNpcHit(NpcHitData data)
    {
        if (!_bosses.TryGetValue(data.TargetNpcId, out var boss)) return;
        boss.Hp = (float)data.Hp;
        boss.HpMax = (float)data.HpMax;
        _feedback.SpawnBossDamage(data.TargetNpcId, data.Damage);
    }

    private void OnWorldEvent(WorldEventData data)
    {
        WorldZones.WorldBossEventActive = data.Active && data.PvPOff;
        ApplyWorldEvent(new WorldEventState
        {
            Active = data.Active,
            Name = data.Name,
            PvPOff = data.PvPOff,
            BossCount = data.BossCount,
        });
    }

    private void ApplyWorldEvent(WorldEventState data)
    {
        WorldZones.WorldBossEventActive = data.Active && data.PvPOff;
        var prev = _lastWorldEvent;
        _lastWorldEvent = new WorldEventState
        {
            Active = data.Active,
            PvPOff = data.PvPOff,
            Name = data.Name,
            BossCount = data.BossCount,
        };

        if (prev != null && prev.Active == data.Active && prev.PvPOff == data.PvPOff)
            return;

        if (data.Active)
        {
            _zoneBanner.ShowEnter(data.Name ?? "World Boss Event", "PvP disabled - unite to defeat the threat!");
            _status = "World boss event! PvP is off. Find the boss on your map.";
        }
        else if (prev is { Active: true, PvPOff: true })
        {
            _zoneBanner.ShowEnter("Threat Subsided", "PvP rules return to normal");
        }
    }

    private void OnBossSpawn(BossSpawnData data)
    {
        var pos = new Vector2((float)data.X, (float)data.Y);
        if (!_bosses.TryGetValue(data.NpcId, out var boss))
        {
            boss = new BossEntity { Id = data.NpcId, Position = pos };
            _bosses[data.NpcId] = boss;
        }
        boss.DefId = data.DefId;
        boss.Name = data.Name;
        boss.SetTarget(pos);
        _zoneBanner.ShowEnter(data.Name, "World boss spawned - check your map!");
        _status = $"{data.Name} has spawned in the wilderness!";
    }

    private void OnBossDeath(BossDeathData data)
    {
        _bosses.Remove(data.NpcId);
        _zoneBanner.ShowEnter($"{data.Name} defeated!", "The world boss event may end soon.");
        _status = $"{data.Name} has been defeated!";
    }

    private void OnBossAction(BossActionData data)
    {
        if (!_bosses.TryGetValue(data.NpcId, out var boss)) return;
        boss.Action = data.Action;
        boss.AbilityFlash = 0.5f;
    }

    private void OnHouseBuilt(HouseBuiltData data)
    {
        UpsertHouse(data.House);
        _zoneBanner.ShowEnter(data.House.OwnerId == _screens.Net.LocalCharacterId
            ? "Your Homestead"
            : $"{data.House.OwnerName}'s Homestead",
            "Safe haven established — PvP off, garden ready.");
        if (data.House.OwnerId == _screens.Net.LocalCharacterId)
            _status = "Homestead built! You received a Homestead Key. Tend your garden and press H inside to decorate.";
    }

    private void OnHouseRemoved(HouseRemovedData data)
    {
        _houses.RemoveAll(h => h.Id == data.HouseId);
        ApplyHouseList();
        if (data.OwnerId == _screens.Net.LocalCharacterId)
        {
            _housingDecorate.Deactivate();
            _status = "The homestead was abandoned and removed.";
        }
    }

    private void OnHouseUpdated(HouseUpdatedData data) => UpsertHouse(data.House);

    private void OnInventoryUpdated(InventoryData data)
    {
        _inventory.ApplyFromServer(data.Items);
        RefreshHotbarInventoryLinks();
        UpdateBuildHouseEnabled();
    }

    private void RefreshHotbarInventoryLinks()
    {
        for (var i = 0; i < _hotbar.Slots.Length; i++)
        {
            var entry = _hotbar.Slots[i].Entry;
            if (entry?.GetValueOrDefault("fromInventory") is not true) continue;

            if (entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
            {
                var slot = slotObj switch
                {
                    int idx => idx,
                    long l => (int)l,
                    _ => -1,
                };
                if (slot < 0 || slot >= PlayerInventory.SlotCount) continue;
                var invSlot = _inventory.Slots[slot];
                var itemId = entry.GetValueOrDefault("itemId") as string
                    ?? entry.GetValueOrDefault(HotbarEntry.IdKey) as string;
                if (invSlot.IsEmpty || invSlot.ItemId != itemId)
                    _hotbar.AssignSlot(i, null);
            }
        }
    }

    private void OnWorldItemRemoved(WorldItemRemovedData data)
    {
        _interactables.RemoveAll(i => i.DropId == data.DropId);
    }

    private void OnWorldItemAdded(WorldItemDropState drop) => AddWorldItemInteractable(drop);

    private void AddWorldItemInteractable(WorldItemDropState drop)
    {
        _interactables.RemoveAll(i => i.DropId == drop.Id);
        var info = ItemCatalog.Get(drop.ItemId);
        var name = drop.ItemId == "house_key" ? "Homestead Key" : info?.Name ?? drop.ItemId;
        _interactables.Add(new InteractableEntity
        {
            Id = $"world_item_{drop.Id}",
            DropId = drop.Id,
            ItemId = drop.ItemId,
            DisplayName = drop.Count > 1 ? $"{name} x{drop.Count}" : name,
            Position = new Vector2((float)drop.X, (float)drop.Y),
            Kind = InteractableKind.GroundItem,
            Tint = drop.ItemId == "house_key"
                ? new Color(0.92f, 0.78f, 0.28f)
                : new Color(0.72f, 0.58f, 0.42f),
            PickRadius = 18f,
        });
    }

    private bool TryDropPayloadInWorld(DragPayload payload, Point mouseScreen)
    {
        if (IsLocalDyingOrDead()) return false;
        if (_windows.IsPointOverOpenWindow(mouseScreen) || IsOverHotbar(mouseScreen)) return false;

        var slot = ResolveDropInventorySlot(payload);
        if (slot < 0) return false;

        var local = FindLocalPlayer();
        if (local == null) return false;

        var world = ScreenToWorld(mouseScreen);
        world = ClampDropPosition(local.Position, world);

        _screens.Net.SendDropItem(slot, world.X, world.Y);

        if (payload.Kind == DragPayloadKind.Hotbar && payload.SourceHotbarIndex >= 0)
            _hotbar.AssignSlot(payload.SourceHotbarIndex, null);

        var name = payload.DisplayName ?? "Item";
        _status = $"Dropped {name}.";
        return true;
    }

    private static int ResolveDropInventorySlot(DragPayload payload) => payload.Kind switch
    {
        DragPayloadKind.Item when payload.SourceInventoryIndex >= 0 => payload.SourceInventoryIndex,
        DragPayloadKind.Hotbar => InventorySlotFromHotbarEntry(payload.HotbarEntry),
        _ => -1,
    };

    private static int InventorySlotFromHotbarEntry(Dictionary<string, object>? entry)
    {
        if (entry?.GetValueOrDefault("fromInventory") is not true) return -1;
        if (!entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj)) return -1;
        return slotObj switch
        {
            int idx => idx,
            long l => (int)l,
            _ => -1,
        };
    }

    private static Vector2 ClampDropPosition(Vector2 playerPos, Vector2 target)
    {
        const float dropRange = 96f;
        var offset = target - playerPos;
        if (offset.LengthSquared() <= dropRange * dropRange) return target;
        return playerPos + new Vector2(8f, 10f);
    }

    private void OnServerError(string message) => _status = message;

    private HousePlotZone? LocalHomestead() =>
        WorldZones.HomesteadFor(_screens.Net.LocalCharacterId, _inventory.HouseKeyId());

    private bool LocalHasHomestead() => LocalHomestead() != null;

    private void UpdateBuildHouseEnabled() =>
        _screens.SetBuildHouseEnabled(!LocalHasHomestead());

    private void UpsertHouse(HouseState house)
    {
        _houses.RemoveAll(h => h.Id == house.Id);
        _houses.Add(house);
        ApplyHouseList();
    }

    private void ApplyHouseList()
    {
        WorldZones.SyncHouses(_houses);
        SyncHouseInteractables();
        UpdateBuildHouseEnabled();
    }

    private void SyncWorldItemInteractables(List<WorldItemDropState>? drops)
    {
        _interactables.RemoveAll(i => i.Kind == InteractableKind.GroundItem);
        if (drops == null) return;

        foreach (var drop in drops)
            AddWorldItemInteractable(drop);
    }

    private void SyncHouseInteractables()
    {
        _interactables.RemoveAll(i => i.Id.StartsWith("house_", StringComparison.Ordinal) && i.Id.Contains("_crop_", StringComparison.Ordinal));

        var localId = _screens.Net.LocalCharacterId;
        foreach (var house in WorldZones.Houses)
        {
            for (var i = 0; i < HousingConstants.GardenCropOffsets.Length; i++)
            {
                var isOwn = house.OwnerId == localId;
                _interactables.Add(new InteractableEntity
                {
                    Id = HousingConstants.CropTargetId(house.Id, i),
                    DisplayName = isOwn ? "Garden Plot" : $"{house.OwnerName}'s Garden",
                    Position = house.Center + HousingConstants.GardenCropOffsets[i],
                    Kind = InteractableKind.FarmPlot,
                    Tint = isOwn ? new Color(0.3f, 0.55f, 0.26f) : new Color(0.26f, 0.48f, 0.22f),
                    PickRadius = 22f,
                });
            }
        }
    }

    private void TryBuildHouse()
    {
        if (LocalHasHomestead())
        {
            _status = "You already have a homestead or carry its key.";
            return;
        }

        var local = FindLocalPlayer();
        if (local == null) return;

        if (WorldZones.ZoneAt(local.Position) != null)
        {
            _status = "Leave town first — build in the wilderness.";
            return;
        }

        _screens.Net.SendBuildHouse();
        _status = "Building homestead at your location...";
    }

    private void UpdateHousing(
        PlayerEntity? local,
        KeyboardState kb,
        KeyboardState prevKb,
        MouseState mouse,
        bool inputBlocked,
        bool uiCapturesMouse)
    {
        if (local is { IsDead: false })
        {
            var ownHouse = LocalHomestead();
            if (ownHouse != null
                && HousingConstants.InHouseInterior(local.Position, ownHouse.Center)
                && kb.IsKeyDown(Keys.H) && !prevKb.IsKeyDown(Keys.H))
            {
                _housingDecorate.Toggle();
                _status = _housingDecorate.IsActive
                    ? "Decorate mode — Tab to cycle, click to place, H/Esc to exit."
                    : "Decorate mode off.";
            }
        }

        if (!_housingDecorate.IsActive) return;

        _housingDecorate.Update(kb, prevKb, mouse, inputBlocked);

        if (_housingDecorate.TryConsumePlaceClick(mouse, _prevMouse, inputBlocked || uiCapturesMouse))
        {
            var world = ScreenToWorld(mouse.Position);
            _screens.Net.SendPlaceFurniture(_housingDecorate.SelectedType, world.X, world.Y);
            _status = $"Placing {_housingDecorate.SelectedType}...";
        }
    }

    private void OnSnapshotPlayers(List<PlayerState> players)
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
                var houseChanged = p.InsideHouseId != s.InsideHouseId;
                if (houseChanged)
                {
                    p.InsideHouseId = s.InsideHouseId;
                    p.Position = pos;
                    p.Target = pos;
                    if (p.InputDir.LengthSquared() > 0.0001f)
                        p.MoveDir = PlayerEntity.CardinalFacing(p.InputDir);
                }
                else
                {
                    p.SetTarget(pos);
                    p.InsideHouseId = s.InsideHouseId;
                }
            }

            p.HeadCosmetic = string.IsNullOrEmpty(s.HeadCosmetic) ? null : s.HeadCosmetic;

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

    private bool EquipCosmeticFromEntry(Dictionary<string, object> entry)
    {
        if (entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
        {
            var slot = slotObj switch
            {
                int idx => idx,
                long l => (int)l,
                _ => -1,
            };
            if (slot >= 0)
            {
                EquipCosmeticFromSlot(slot);
                return true;
            }
        }
        _status = "Cannot equip cosmetic.";
        return false;
    }

    private void EquipCosmeticFromSlot(int slotIndex)
    {
        if (_ghostMode || IsLocalDyingOrDead()) return;
        if (slotIndex < 0 || slotIndex >= PlayerInventory.SlotCount) return;
        var slot = _inventory.Slots[slotIndex];
        if (slot.IsEmpty || slot.ItemId == null || !ItemCatalog.IsCosmetic(slot.ItemId))
        {
            _status = "That item is not wearable.";
            return;
        }

        var local = FindLocalPlayer();
        var wasEquipped = local?.HeadCosmetic == slot.ItemId;
        _screens.Net.SendEquipCosmetic(slotIndex);
        if (local != null)
            local.HeadCosmetic = wasEquipped ? null : slot.ItemId;
        _status = wasEquipped
            ? $"Removed {ItemCatalog.Get(slot.ItemId)?.Name ?? slot.ItemId}."
            : $"Wearing {ItemCatalog.Get(slot.ItemId)?.Name ?? slot.ItemId}.";
    }

    private void OnInventorySlotClicked(int slotIndex)
    {
        if (_ghostMode || IsLocalDyingOrDead()) return;
        var slot = _inventory.Slots[slotIndex];
        if (slot.IsEmpty || slot.ItemId == null) return;
        if (slot.IsOnCooldown)
        {
            var name = ItemCatalog.Get(slot.ItemId)?.Name ?? slot.ItemId;
            _status = $"{name} is on cooldown ({MathF.Ceiling(slot.CooldownRemaining):0}s).";
            return;
        }
        if (slot.ItemId == "house_key")
        {
            _status = "The homestead key is not a consumable.";
            return;
        }
        if (ItemCatalog.IsCosmetic(slot.ItemId))
        {
            EquipCosmeticFromSlot(slotIndex);
            return;
        }
        TryUseEntry(null, ItemCatalog.ToHotbarEntry(slot.ItemId, slotIndex));
    }

    private void OnSpellBookAbilityClicked(string abilityId)
    {
        if (_ghostMode || IsLocalDyingOrDead()) return;
        TryUseEntry(null, AbilityCatalog.ToHotbarEntry(abilityId));
    }

    private void OnHotbarSlot(int index, Dictionary<string, object>? entry) =>
        TryUseEntry(index, entry);

    private void TryUseEntry(int? hotbarIndex, Dictionary<string, object>? entry)
    {
        var key = hotbarIndex is int hi ? Hotbar.KeyLabels[hi] : null;
        if (entry == null)
        {
            if (key != null)
                _status = $"Hotbar slot {key} is empty.";
            return;
        }

        var id = entry.GetValueOrDefault(HotbarEntry.IdKey) as string;
        if (hotbarIndex == null && id != null)
        {
            var cdSlot = FindHotbarSlotForAbility(id, entry);
            if (cdSlot >= 0 && _hotbar.Slots[cdSlot].IsOnCooldown)
            {
                OnHotbarCooldownBlocked(cdSlot, _hotbar.Slots[cdSlot].CooldownRemaining);
                return;
            }
        }

        var cooldown = HotbarEntry.GetCooldown(entry);
        var used = false;

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
        else if (id == "blood_bolt")
            used = CastBloodBolt();
        else if (id == "poison_cloud")
            used = CastPoisonCloud();
        else if (id == "bandage")
            used = entry.GetValueOrDefault("fromInventory") is true
                ? UseConsumableFromEntry(entry, "bandage")
                : UseBandage();
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
        else         if (id == "second_wind")
            used = UseSecondWind();
        else if (id == "slash")
        {
            used = TryPayAbilityCost("slash") && TryMeleeAttackInternal(GetAimDirection());
        }
        else if (id == "health_potion")
            used = UseHealthPotion(entry);
        else if (id == "mana_potion")
            used = UseManaPotion(entry);
        else if (id == "stamina_potion")
            used = UseStaminaPotion(entry);
        else if (id == "antidote")
            used = UseAntidote(entry);
        else if (entry.GetValueOrDefault("kind") as string == "cosmetic")
            used = EquipCosmeticFromEntry(entry);
        else
        {
            _status = key != null
                ? $"Used slot {key}: {entry.GetValueOrDefault("name")} ({entry.GetValueOrDefault("kind")})"
                : $"Used {entry.GetValueOrDefault("name")}.";
            used = true;
        }

        if (used && cooldown > 0f)
        {
            var cdSlot = hotbarIndex ?? FindHotbarSlotForAbility(id!, entry);
            if (cdSlot >= 0)
                _hotbar.StartCooldown(cdSlot, cooldown);

            if (entry.GetValueOrDefault("fromInventory") is true
                && entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
            {
                var slotIndex = slotObj switch
                {
                    int i => i,
                    long l => (int)l,
                    _ => -1,
                };
                if (slotIndex >= 0)
                    _inventory.StartCooldownAt(slotIndex, cooldown);
            }
        }
    }

    private int FindHotbarSlotForAbility(string abilityId, Dictionary<string, object> entry)
    {
        if (entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
        {
            var invSlot = slotObj switch
            {
                int i => i,
                long l => (int)l,
                _ => -1,
            };
            if (invSlot >= 0)
            {
                for (var i = 0; i < _hotbar.Slots.Length; i++)
                {
                    var e = _hotbar.Slots[i].Entry;
                    if (e == null) continue;
                    if (e.TryGetValue(HotbarEntry.InventorySlotKey, out var s) && s.Equals(slotObj))
                        return i;
                }
            }
        }

        for (var i = 0; i < _hotbar.Slots.Length; i++)
        {
            if (_hotbar.Slots[i].Entry?.GetValueOrDefault(HotbarEntry.IdKey) as string == abilityId)
                return i;
        }
        return -1;
    }

    private void OnHotbarCooldownBlocked(int index, float remaining)
    {
        var entry = _hotbar.Slots[index].Entry;
        var name = entry?.GetValueOrDefault("name") ?? Hotbar.KeyLabels[index];
        _status = $"{name} is on cooldown ({MathF.Ceiling(remaining):0}s).";
    }

    private bool UseConsumableFromEntry(Dictionary<string, object> entry, string itemId)
    {
        if (entry.GetValueOrDefault("fromInventory") is true && !_inventory.HasItem(itemId))
        {
            _status = "You don't have that item in your inventory.";
            return false;
        }

        var used = itemId switch
        {
            "bandage" => UseBandage(),
            "health_potion" => UseHealthPotionLocal(),
            "mana_potion" => UseManaPotionLocal(),
            "stamina_potion" => UseStaminaPotionLocal(),
            "antidote" => UseAntidoteLocal(),
            _ => false,
        };

        if (used && entry.GetValueOrDefault("fromInventory") is true)
        {
            if (entry.TryGetValue(HotbarEntry.InventorySlotKey, out var slotObj))
            {
                var slotIdx = slotObj switch
                {
                    int i => i,
                    long l => (int)l,
                    _ => -1,
                };
                if (slotIdx >= 0)
                    _inventory.ConsumeAt(slotIdx);
                else
                    _inventory.Consume(itemId);
            }
            else
                _inventory.Consume(itemId);
        }
        return used;
    }

    private bool UseHealthPotion(Dictionary<string, object> entry) =>
        entry.GetValueOrDefault("fromInventory") is true
            ? UseConsumableFromEntry(entry, "health_potion")
            : UseHealthPotionLocal();

    private bool UseManaPotion(Dictionary<string, object> entry) =>
        entry.GetValueOrDefault("fromInventory") is true
            ? UseConsumableFromEntry(entry, "mana_potion")
            : UseManaPotionLocal();

    private bool UseStaminaPotion(Dictionary<string, object> entry) =>
        entry.GetValueOrDefault("fromInventory") is true
            ? UseConsumableFromEntry(entry, "stamina_potion")
            : UseStaminaPotionLocal();

    private bool UseAntidote(Dictionary<string, object> entry) =>
        entry.GetValueOrDefault("fromInventory") is true
            ? UseConsumableFromEntry(entry, "antidote")
            : UseAntidoteLocal();

    private bool UseHealthPotionLocal()
    {
        var local = FindLocalPlayer();
        if (local == null) return false;
        var info = ItemCatalog.Get("health_potion")!;
        var heal = info.Heal!.Value;
        local.Stats.Hp = MathF.Min(local.Stats.HpMax, local.Stats.Hp + heal);
        _feedback.SpawnHeal(local.Id, heal);
        _status = $"+{heal} HP from Health Potion.";
        return true;
    }

    private bool UseManaPotionLocal()
    {
        var local = FindLocalPlayer();
        if (local == null) return false;
        var info = ItemCatalog.Get("mana_potion")!;
        local.Stats.Mana = MathF.Min(local.Stats.ManaMax, local.Stats.Mana + info.ManaRestore!.Value);
        _status = $"+{info.ManaRestore:0} mana from Mana Potion.";
        return true;
    }

    private bool UseStaminaPotionLocal()
    {
        var local = FindLocalPlayer();
        if (local == null) return false;
        var info = ItemCatalog.Get("stamina_potion")!;
        local.Stats.Stamina = MathF.Min(local.Stats.StaminaMax, local.Stats.Stamina + info.StaminaRestore!.Value);
        _status = $"+{info.StaminaRestore:0} stamina from Stamina Potion.";
        return true;
    }

    private bool UseAntidoteLocal()
    {
        _status = "Antidote used — toxins cleared.";
        return true;
    }

    private void SeedHotbar()
    {
        var defaults = new[]
        {
            "shield_bash", "whirlwind", "warrior_dash", "fireball", "ice_shard",
            "battle_shout", "iron_skin", "hunter_mark", "bandage", "second_wind",
        };
        for (var i = 0; i < defaults.Length; i++)
            _hotbar.AssignSlot(i, AbilityCatalog.ToHotbarEntry(defaults[i]));
    }

    private void SeedWorldTownInteractables()
    {
        if (_interactablesSeeded) return;
        _interactablesSeeded = true;

        void AddAt(string townId, string id, string name, Vector2 offset, InteractableKind kind, Color tint, float pick = 20f)
        {
            var town = WorldZones.Get(townId);
            if (town == null) return;
            _interactables.Add(new InteractableEntity
            {
                Id = id, DisplayName = name, Position = town.Center + offset,
                Kind = kind, Tint = tint, PickRadius = pick,
            });
        }

        // Starter Town
        AddAt("starter_town", "sign_welcome", "Welcome to Starter Town", new Vector2(0, -85), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        AddAt("starter_town", "npc_guide", "Guide Aldric", new Vector2(-70, -55), InteractableKind.Npc, new Color(0.72f, 0.58f, 0.42f));
        AddAt("starter_town", "bank_starter", "Town Bank", new Vector2(70, -55), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        AddAt("starter_town", "fish_shore_1", "Fishing Spot", new Vector2(-38, -92), InteractableKind.Fishing, new Color(0.55f, 0.72f, 0.85f), 24);
        AddAt("starter_town", "fish_shore_2", "Fishing Spot", new Vector2(38, -92), InteractableKind.Fishing, new Color(0.5f, 0.68f, 0.82f), 24);
        AddAt("starter_town", "npc_fisher", "Old Fisher", new Vector2(92, -42), InteractableKind.Npc, new Color(0.65f, 0.7f, 0.75f));
        AddAt("starter_town", "tree_oak_1", "Oak Tree", new Vector2(92, 0), InteractableKind.Tree, new Color(0.25f, 0.55f, 0.28f));
        AddAt("starter_town", "tree_oak_2", "Oak Tree", new Vector2(72, 58), InteractableKind.Tree, new Color(0.22f, 0.5f, 0.26f));
        AddAt("starter_town", "anvil_starter", "Public Anvil", new Vector2(42, 82), InteractableKind.Anvil, new Color(0.38f, 0.4f, 0.44f), 22);
        AddAt("starter_town", "sign_wilderness", "South Gate - Wilderness ahead", new Vector2(88, 72), InteractableKind.Sign, new Color(0.85f, 0.35f, 0.3f), 22);
        AddAt("starter_town", "chest_starter", "Starter Chest", new Vector2(0, 92), InteractableKind.Chest, new Color(0.62f, 0.42f, 0.22f));
        AddAt("starter_town", "tree_pine_1", "Pine Tree", new Vector2(-42, 82), InteractableKind.Tree, new Color(0.18f, 0.42f, 0.32f));
        AddAt("starter_town", "sign_mine", "Mine road - danger", new Vector2(-72, 58), InteractableKind.Sign, new Color(0.78f, 0.55f, 0.35f), 22);
        AddAt("starter_town", "chest_loot_1", "Abandoned Crate", new Vector2(-92, 42), InteractableKind.Chest, new Color(0.48f, 0.32f, 0.2f));
        AddAt("starter_town", "rock_iron_1", "Iron Rock", new Vector2(-92, 0), InteractableKind.Rock, new Color(0.45f, 0.48f, 0.52f), 22);
        AddAt("starter_town", "rock_copper_1", "Copper Rock", new Vector2(-72, -28), InteractableKind.Rock, new Color(0.58f, 0.4f, 0.28f), 22);
        AddAt("starter_town", "farm_plot_1", "Town Farm Plot", new Vector2(-48, 48), InteractableKind.FarmPlot, new Color(0.32f, 0.55f, 0.28f), 24);
        AddAt("starter_town", "cooking_fire_1", "Campfire Hearth", new Vector2(48, 48), InteractableKind.CookingFire, new Color(0.85f, 0.45f, 0.2f), 22);
        AddAt("starter_town", "npc_hermit", "Hermit", new Vector2(-92, -42), InteractableKind.Npc, new Color(0.55f, 0.45f, 0.38f));

        // Northhaven
        AddAt("northhaven", "sign_northhaven", "Northhaven - Safe Haven", new Vector2(0, -78), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        AddAt("northhaven", "bank_northhaven", "Northhaven Bank", new Vector2(58, -40), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        AddAt("northhaven", "npc_castellan", "Castellan", new Vector2(-52, -36), InteractableKind.Npc, new Color(0.68f, 0.62f, 0.72f));
        AddAt("northhaven", "anvil_northhaven", "Castle Forge", new Vector2(0, 48), InteractableKind.Anvil, new Color(0.38f, 0.4f, 0.44f), 22);

        // Westmere
        AddAt("westmere", "sign_westmere", "Westmere Village", new Vector2(0, -72), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        AddAt("westmere", "bank_westmere", "Village Bank", new Vector2(48, -28), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        AddAt("westmere", "npc_elder", "Village Elder", new Vector2(-44, -24), InteractableKind.Npc, new Color(0.62f, 0.52f, 0.42f));
        AddAt("westmere", "tree_west_1", "Old Oak", new Vector2(-58, 38), InteractableKind.Tree, new Color(0.22f, 0.5f, 0.26f));
        AddAt("westmere", "farm_westmere", "Village Farm", new Vector2(0, 42), InteractableKind.FarmPlot, new Color(0.3f, 0.52f, 0.26f), 24);

        // Eastwatch
        AddAt("eastwatch", "sign_eastwatch", "Eastwatch Keep", new Vector2(0, -76), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        AddAt("eastwatch", "bank_eastwatch", "Eastwatch Bank", new Vector2(54, -38), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        AddAt("eastwatch", "npc_watcher", "Wall Watcher", new Vector2(-48, -32), InteractableKind.Npc, new Color(0.58f, 0.62f, 0.72f));

        // Southport
        AddAt("southport", "sign_southport", "Southport - Safe Haven", new Vector2(0, -74), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        AddAt("southport", "bank_southport", "Harbor Bank", new Vector2(52, -34), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        AddAt("southport", "npc_harbormaster", "Harbor Master", new Vector2(-50, -30), InteractableKind.Npc, new Color(0.55f, 0.65f, 0.75f));
        AddAt("southport", "fish_southport", "Harbor Fishing", new Vector2(0, 58), InteractableKind.Fishing, new Color(0.5f, 0.68f, 0.82f), 24);
        AddAt("southport", "cooking_southport", "Harbor Hearth", new Vector2(-20, 42), InteractableKind.CookingFire, new Color(0.82f, 0.42f, 0.18f), 22);
    }
}
