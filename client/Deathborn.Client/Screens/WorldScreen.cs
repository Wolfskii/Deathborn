using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Net;

namespace Deathborn.Client.Screens;

public sealed class WorldScreen : IScreen
{
    private readonly ScreenManager _screens;
    private readonly WorldBackgroundRenderer _bg = new();
    private readonly Dictionary<long, PlayerEntity> _players = new();
    private readonly List<InteractableEntity> _interactables = [];
    private readonly Hotbar _hotbar = new();

    private Vector2 _camera;
    private Vector2 _moveDir;
    private float _inputAccum;
    private string _status = "Connected. WASD to move. Click or [E] to interact.";
    private string _hud = "";
    private string _interactPrompt = "";
    private InteractableEntity? _focused;
    private InteractableEntity? _hovered;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;

    public WorldScreen(ScreenManager screens)
    {
        _screens = screens;
        _hotbar.SlotActivated += OnHotbarSlot;
    }

    public void OnEnter()
    {
        var net = _screens.Net;
        net.Snapshot += OnSnapshot;
        net.Disconnected += OnDisconnected;

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

        SeedInteractables(new Vector2(net.SpawnX, net.SpawnY));
        SeedHotbar();
    }

    public void OnExit()
    {
        var net = _screens.Net;
        net.Snapshot -= OnSnapshot;
        net.Disconnected -= OnDisconnected;
        _hotbar.SlotActivated -= OnHotbarSlot;
    }

    public void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        _moveDir = ReadMoveDir(kb);
        _inputAccum += dt;
        if (_inputAccum >= Config.InputSendInterval || Vector2.DistanceSquared(_moveDir, _lastSentDir) > 0.0001f)
        {
            _inputAccum = 0;
            _lastSentDir = _moveDir;
            _screens.Net.SendInput(_moveDir.X, _moveDir.Y);
        }

        foreach (var p in _players.Values) p.Update(dt);
        _hotbar.Update(dt, kb, _prevKb);

        if (_players.TryGetValue(_screens.Net.LocalCharacterId, out var local))
            _camera = local.Position;

        UpdateInteractFocus(mouse.Position);
        UpdateInteractPrompt();

        if (kb.IsKeyDown(Keys.E) && !_prevKb.IsKeyDown(Keys.E))
            TryInteractNearest();

        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
            TryInteractAt(ScreenToWorld(mouse.Position));

        _hud = $"Pos: ({(int)_camera.X}, {(int)_camera.Y})  Input: ({_moveDir.X:+#0.0;-#0.0;+0.0}, {_moveDir.Y:+#0.0;-#0.0;+0.0})  " +
               $"{(_moveDir.Length() > 0.05f ? "moving" : "idle")}  id={_screens.Net.LocalCharacterId}  players={_players.Count}  " +
               $"ws={(_screens.Net.WsConnected ? "open" : "closed")}";

        _prevKb = kb;
        _prevMouse = mouse;
    }

    private Vector2 _lastSentDir;

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;

        sb.Begin();
        _bg.Draw(sb, _camera, ScreenCenter);

        foreach (var obj in _interactables)
            obj.Draw(sb, font, WorldToScreen(obj.Position));

        foreach (var p in _players.Values)
            p.Draw(sb, font, WorldToScreen(p.Position));

        sb.DrawString(font, _status, new Vector2(12, 12), Color.White);
        sb.DrawString(font, _hud, new Vector2(12, 36), new Color(200, 200, 210));

        if (!string.IsNullOrEmpty(_interactPrompt))
            sb.DrawString(font, _interactPrompt, new Vector2(Config.Width / 2f - 200, Config.Height - 108), new Color(220, 220, 180));

        _hotbar.Draw(sb, font);
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

    private Vector2 ScreenCenter => new(Config.Width / 2f, Config.Height / 2f);

    private Vector2 WorldToScreen(Vector2 world) => world - _camera + ScreenCenter;

    private Vector2 ScreenToWorld(Point screen) =>
        new Vector2(screen.X, screen.Y) - ScreenCenter + _camera;

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

    private void TryInteractAt(Vector2 worldPos)
    {
        var target = FindAtPoint(worldPos);
        if (target == null) return;
        if (!target.IsInRange(_camera))
        {
            _status = $"Too far to interact with {target.DisplayName}.";
            return;
        }
        PerformInteract(target);
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
        _status = entry == null
            ? $"Hotbar slot {key} is empty."
            : $"Used slot {key}: {entry.GetValueOrDefault("name")} ({entry.GetValueOrDefault("kind")})";
    }

    private void SeedHotbar()
    {
        _hotbar.SetSlot(0, new Dictionary<string, object> { ["id"] = "strike", ["name"] = "Strike", ["kind"] = "skill", ["color"] = new Color(0.85f, 0.35f, 0.3f) });
        _hotbar.SetSlot(1, new Dictionary<string, object> { ["id"] = "heal", ["name"] = "Heal", ["kind"] = "spell", ["color"] = new Color(0.35f, 0.75f, 0.45f) });
        _hotbar.SetSlot(2, new Dictionary<string, object> { ["id"] = "bandage", ["name"] = "Bandage", ["kind"] = "item", ["color"] = new Color(0.75f, 0.65f, 0.35f) });
    }

    private void SeedInteractables(Vector2 origin)
    {
        void Add(string id, string name, Vector2 offset, InteractableKind kind, Color tint, float pick = 20f)
        {
            _interactables.Add(new InteractableEntity
            {
                Id = id, DisplayName = name, Position = origin + offset,
                Kind = kind, Tint = tint, PickRadius = pick,
            });
        }

        Add("sign_welcome", "Welcome to Starter Town", new Vector2(0, -58), InteractableKind.Sign, new Color(0.75f, 0.68f, 0.38f), 22);
        Add("npc_guide", "Guide Aldric", new Vector2(-48, -38), InteractableKind.Npc, new Color(0.72f, 0.58f, 0.42f));
        Add("bank_starter", "Town Bank", new Vector2(48, -38), InteractableKind.Bank, new Color(0.55f, 0.62f, 0.78f), 24);
        Add("fish_shore_1", "Fishing Spot", new Vector2(-22, -62), InteractableKind.Fishing, new Color(0.55f, 0.72f, 0.85f), 24);
        Add("fish_shore_2", "Fishing Spot", new Vector2(22, -62), InteractableKind.Fishing, new Color(0.5f, 0.68f, 0.82f), 24);
        Add("npc_fisher", "Old Fisher", new Vector2(62, -28), InteractableKind.Npc, new Color(0.65f, 0.7f, 0.75f));
        Add("tree_oak_1", "Oak Tree", new Vector2(62, 0), InteractableKind.Tree, new Color(0.25f, 0.55f, 0.28f));
        Add("tree_oak_2", "Oak Tree", new Vector2(48, 38), InteractableKind.Tree, new Color(0.22f, 0.5f, 0.26f));
        Add("anvil_starter", "Public Anvil", new Vector2(28, 55), InteractableKind.Anvil, new Color(0.38f, 0.4f, 0.44f), 22);
        Add("sign_wilderness", "Wilderness - PvP enabled", new Vector2(58, 48), InteractableKind.Sign, new Color(0.85f, 0.35f, 0.3f), 22);
        Add("chest_starter", "Starter Chest", new Vector2(0, 62), InteractableKind.Chest, new Color(0.62f, 0.42f, 0.22f));
        Add("tree_pine_1", "Pine Tree", new Vector2(-28, 55), InteractableKind.Tree, new Color(0.18f, 0.42f, 0.32f));
        Add("sign_mine", "Mine entrance - danger", new Vector2(-48, 38), InteractableKind.Sign, new Color(0.78f, 0.55f, 0.35f), 22);
        Add("chest_loot_1", "Abandoned Crate", new Vector2(-62, 28), InteractableKind.Chest, new Color(0.48f, 0.32f, 0.2f));
        Add("rock_iron_1", "Iron Rock", new Vector2(-62, 0), InteractableKind.Rock, new Color(0.45f, 0.48f, 0.52f), 22);
        Add("rock_copper_1", "Copper Rock", new Vector2(-48, -18), InteractableKind.Rock, new Color(0.58f, 0.4f, 0.28f), 22);
        Add("npc_hermit", "Hermit", new Vector2(-62, -28), InteractableKind.Npc, new Color(0.55f, 0.45f, 0.38f));
    }
}
