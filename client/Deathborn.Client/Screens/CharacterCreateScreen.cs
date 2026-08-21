using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Deathborn.Client.Rendering.Characters;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class CharacterCreateScreen : IScreen
{
    private enum PreviewAnimation { Idle, Walk, Run, Attack }

    private static readonly Color Panel = new(17, 22, 28);
    private static readonly Color PanelInner = new(12, 17, 22);
    private static readonly Color Gold = new(213, 174, 91);
    private static readonly Color GoldDim = new(117, 91, 49);
    private static readonly Color Muted = new(170, 177, 187);

    private readonly ScreenManager _screens;
    private readonly TextField _name = new() { Placeholder = "Enter your character's name..." };
    private readonly Button _nextBtn = new() { Label = "CONFIRM" };
    private readonly CharacterVisual _preview = new();
    private readonly CharacterVisual[] _racePortraits = [new(), new(), new(), new(), new(), new()];
    private readonly Rectangle[] _raceBounds = new Rectangle[CharacterCreationCatalog.Races.Length];
    private readonly Rectangle[] _skinBounds = new Rectangle[4];
    private readonly Rectangle[] _eyeBounds = new Rectangle[4];
    private readonly Rectangle[] _hairBounds = new Rectangle[4];
    private readonly Rectangle[] _paletteBounds = new Rectangle[6];
    private readonly Rectangle[] _animationBounds = new Rectangle[4];
    private readonly Rectangle[] _directionBounds = new Rectangle[4];
    private readonly Rectangle[] _genderBounds = new Rectangle[2];
    private readonly Rectangle[] _hairStyleNavBounds = new Rectangle[2];
    private string _status = "";
    private bool _busy;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private Point _mouse;
    private int _viewportW;
    private int _viewportH;
    private int _raceIndex;
    private string _gender = "male";
    private SkinTone _skinTone = SkinTone.Fair;
    private EyeColor _eyeColor = EyeColor.Brown;
    private int _hairStyleIndex;
    private int _hairIndex;
    private CharacterPalette _skinPalette = CharacterPalette.Identity;
    private CharacterPalette _eyePalette = CharacterPalette.Identity;
    private CharacterPalette _hairPalette = CharacterPalette.Identity;
    private PreviewAnimation _previewAnimation;
    private Vector2 _previewFacing = Vector2.UnitY;
    private Rectangle _mainPanel;
    private Rectangle _previewPanel;
    private Rectangle _contentPanel;
    private Rectangle _appearancePanel;
    private Rectangle _namePanel;
    private Rectangle _raceDetailBounds;
    private Rectangle _randomizeBounds;
    private Rectangle _nameRandomizeBounds;
    private int _draggingPaletteIndex = -1;

    public CharacterCreateScreen(ScreenManager screens) => _screens = screens;

    public void OnEnter()
    {
        Layout();
        _name.Focused = true;
        _status = "";
        _busy = false;
        _nextBtn.Enabled = true;
        UpdateButtons();
        SyncPreview();
        SyncRacePortraits();

        var net = _screens.Net;
        net.Welcome += OnWelcome;
        net.ServerError += OnError;
        net.Disconnected += OnDisconnected;
    }

    public void OnExit()
    {
        _name.Focused = false;

        var net = _screens.Net;
        net.Welcome -= OnWelcome;
        net.ServerError -= OnError;
        net.Disconnected -= OnDisconnected;
    }

    public void Update(GameTime gameTime)
    {
        if (_viewportW != GameViewport.Width || _viewportH != GameViewport.Height)
            Layout();

        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        _mouse = mouse.Position;

        _name.Update(gameTime, kb, _prevKb);
        _preview.UpdateAnimation((float)gameTime.ElapsedGameTime.TotalSeconds, new AnimationInput
        {
            FacingDir = _previewFacing,
            LocomotionDir = _previewFacing,
            IsMoving = _previewAnimation is PreviewAnimation.Walk or PreviewAnimation.Run,
            IsRunning = _previewAnimation == PreviewAnimation.Run,
        });
        if (_previewAnimation == PreviewAnimation.Attack && !_preview.Controller.IsAttackPlaying)
            _preview.StartAttack(_previewFacing);

        if (_draggingPaletteIndex >= 0)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
                SetPaletteFromPointer(_draggingPaletteIndex, _mouse.X);
            else
                _draggingPaletteIndex = -1;
        }

        if (!_busy && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            _name.Focused = _name.Bounds.Contains(_mouse);
            HandleClick(_mouse);
        }

        if (!_busy && kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))
            Create();

        _prevKb = kb;
        _prevMouse = mouse;
    }

    public void Draw(GameTime gameTime)
    {
        var game = DeathbornGame.Instance;
        var sb = game.SpriteBatch;
        var font = game.Font;
        sb.Begin(samplerState: SamplerState.PointClamp);
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), new Color(7, 13, 20));
        DrawPanel(sb, _mainPanel);
        DrawHeader(sb, font);
        DrawPreview(sb, font);
        DrawContent(sb, font);
        DrawAppearance(sb, font);
        DrawName(sb, font);
        var confirmHover = _nextBtn.Contains(_mouse);
        DrawRoundedButton(sb, _nextBtn.Bounds, confirmHover ? new Color(57, 93, 119) : new Color(37, 67, 91), Gold);
        DrawFittedCentered(sb, font, _nextBtn.Label, _nextBtn.Bounds.Center.ToVector2(), _nextBtn.Bounds.Width - 16, Color.White, 0.85f);
        if (!string.IsNullOrEmpty(_status))
            DrawCentered(sb, font, _status, new Vector2(_mainPanel.Center.X, _mainPanel.Bottom - 30), _status.StartsWith("Error") ? new Color(255, 130, 110) : new Color(220, 180, 120));
        sb.End();
    }

    private void Layout()
    {
        _viewportW = GameViewport.Width;
        _viewportH = GameViewport.Height;
        var horizontalMargin = Math.Max(24, GameViewport.Width / 24);
        var verticalMargin = Math.Max(18, GameViewport.Height / 24);
        var width = GameViewport.Width - horizontalMargin * 2;
        var height = GameViewport.Height - verticalMargin * 2;
        _mainPanel = new Rectangle(horizontalMargin, verticalMargin, width, height);
        var innerMargin = Math.Max(24, width / 36);
        var panelGap = Math.Max(16, width / 72);
        var availableWidth = width - innerMargin * 2 - panelGap * 2;
        var sideWidth = availableWidth * 25 / 100;
        var previewWidth = availableWidth - sideWidth * 2;
        var upperY = _mainPanel.Y + Math.Max(78, height / 9);
        var upperHeight = height - (upperY - _mainPanel.Y) - Math.Max(116, height / 6);
        _contentPanel = new Rectangle(_mainPanel.X + innerMargin, upperY, sideWidth, upperHeight);
        _previewPanel = new Rectangle(_contentPanel.Right + panelGap, upperY, previewWidth, upperHeight);
        _appearancePanel = new Rectangle(_previewPanel.Right + panelGap, upperY, sideWidth, upperHeight);
        _namePanel = new Rectangle(_previewPanel.X, _mainPanel.Bottom - Math.Max(92, height / 7), _previewPanel.Width, Math.Max(70, height / 10));
        _name.Bounds = new Rectangle(_namePanel.X + 18, _namePanel.Y + 32, _namePanel.Width - 58, 36);
        _nameRandomizeBounds = new Rectangle(_namePanel.Right - 34, _namePanel.Y + 32, 22, 36);
        _nextBtn.Bounds = new Rectangle(_mainPanel.Right - 156, _mainPanel.Bottom - 72, 128, 42);

        var cardHeight = Math.Max(44, upperHeight / 10);
        for (var i = 0; i < _raceBounds.Length; i++)
            _raceBounds[i] = new Rectangle(_contentPanel.X + 14, _contentPanel.Y + 43 + i * (cardHeight + 14), _contentPanel.Width - 28, cardHeight);
        _raceDetailBounds = new Rectangle(
            _contentPanel.X + 14,
            _raceBounds[^1].Bottom + 14,
            _contentPanel.Width - 28,
            Math.Max(54, _contentPanel.Bottom - _raceBounds[^1].Bottom - 28));
        for (var i = 0; i < 4; i++)
        {
            _skinBounds[i] = new Rectangle(_appearancePanel.X + 18 + i * 52, _appearancePanel.Y + 90, 38, 26);
            _eyeBounds[i] = new Rectangle(_appearancePanel.X + 18 + i * 52, _appearancePanel.Y + 152, 38, 26);
        }
        for (var i = 0; i < _hairBounds.Length; i++)
            _hairBounds[i] = new Rectangle(_appearancePanel.X + 18 + i * 52, _appearancePanel.Y + 258, 38, 26);
        var genderWidth = (_appearancePanel.Width - 46) / 2;
        _genderBounds[0] = new Rectangle(_appearancePanel.X + 18, _appearancePanel.Y + 38, genderWidth, 30);
        _genderBounds[1] = new Rectangle(_appearancePanel.X + 28 + genderWidth, _appearancePanel.Y + 38, genderWidth, 30);
        _randomizeBounds = new Rectangle(_appearancePanel.X + 22, _appearancePanel.Bottom - 38, _appearancePanel.Width - 44, 26);
        _hairStyleNavBounds[0] = new Rectangle(_appearancePanel.X + 18, _appearancePanel.Y + 205, 28, 28);
        _hairStyleNavBounds[1] = new Rectangle(_appearancePanel.Right - 46, _appearancePanel.Y + 205, 28, 28);
        var paletteStartY = _appearancePanel.Y + 302;
        var paletteStep = Math.Clamp((_randomizeBounds.Y - paletteStartY - 29) / 5, 20, 42);
        for (var i = 0; i < _paletteBounds.Length; i++)
            _paletteBounds[i] = new Rectangle(_appearancePanel.X + 22, paletteStartY + i * paletteStep, _appearancePanel.Width - 44, 17);
        var pedestal = new Vector2(_previewPanel.Center.X, _previewPanel.Center.Y - 14);
        const int directionSize = 24;
        _directionBounds[0] = new Rectangle((int)pedestal.X - directionSize / 2, (int)pedestal.Y - 124, directionSize, directionSize);
        _directionBounds[1] = new Rectangle((int)pedestal.X - directionSize / 2, (int)pedestal.Y + 100, directionSize, directionSize);
        _directionBounds[2] = new Rectangle((int)pedestal.X - 124, (int)pedestal.Y - directionSize / 2, directionSize, directionSize);
        _directionBounds[3] = new Rectangle((int)pedestal.X + 100, (int)pedestal.Y - directionSize / 2, directionSize, directionSize);
        var animationY = _previewPanel.Bottom - 44;
        for (var i = 0; i < _animationBounds.Length; i++)
            _animationBounds[i] = new Rectangle(_previewPanel.Center.X - 150 + i * 76, animationY, 70, 24);
    }

    private void HandleClick(Point mouse)
    {
        if (_nextBtn.Contains(mouse))
        {
            Create();
            return;
        }
        for (var i = 0; i < _directionBounds.Length; i++)
        {
            if (!_directionBounds[i].Contains(mouse)) continue;
            _previewFacing = i switch
            {
                0 => new Vector2(0, -1),
                1 => Vector2.UnitY,
                2 => -Vector2.UnitX,
                _ => Vector2.UnitX,
            };
            if (_previewAnimation == PreviewAnimation.Attack)
                _preview.StartAttack(_previewFacing);
            else if (_previewAnimation == PreviewAnimation.Idle)
                _preview.HoldIdlePose(_previewFacing);
            return;
        }
        if (_nameRandomizeBounds.Contains(mouse))
        {
            _name.Text = MythicNameGenerator.Next(_gender);
            _name.Focused = true;
            return;
        }
        if (_randomizeBounds.Contains(mouse))
        {
            Randomize();
            return;
        }
        for (var i = 0; i < _animationBounds.Length; i++)
        {
            if (!_animationBounds[i].Contains(mouse)) continue;
            _previewAnimation = (PreviewAnimation)i;
            if (_previewAnimation == PreviewAnimation.Attack)
                _preview.StartAttack(_previewFacing);
            else if (_previewAnimation == PreviewAnimation.Idle)
                _preview.HoldIdlePose(_previewFacing);
            return;
        }

        for (var i = 0; i < _raceBounds.Length; i++)
            if (_raceBounds[i].Contains(mouse))
                _raceIndex = i;
        for (var i = 0; i < _genderBounds.Length; i++)
            if (_genderBounds[i].Contains(mouse))
            {
                _gender = i == 0 ? "male" : "female";
                _hairStyleIndex = i == 0 ? 0 : 1;
            }
        for (var i = 0; i < _hairStyleNavBounds.Length; i++)
        {
            if (!_hairStyleNavBounds[i].Contains(mouse)) continue;
            _hairStyleIndex = i == 0
                ? (_hairStyleIndex - 1 + CharacterCreationCatalog.HairStyleNames.Length) % CharacterCreationCatalog.HairStyleNames.Length
                : (_hairStyleIndex + 1) % CharacterCreationCatalog.HairStyleNames.Length;
            _hairPalette = CharacterPalette.Identity;
            SyncPreview();
            return;
        }

        for (var i = 0; i < _skinBounds.Length; i++)
            if (_skinBounds[i].Contains(mouse))
            {
                _skinTone = (SkinTone)i;
                _skinPalette = CharacterPalette.Identity;
            }
        for (var i = 0; i < _eyeBounds.Length; i++)
            if (_eyeBounds[i].Contains(mouse))
            {
                _eyeColor = i switch
                {
                    1 => EyeColor.Blue,
                    2 => EyeColor.Green,
                    3 => EyeColor.Gray,
                    _ => EyeColor.Brown,
                };
                _eyePalette = CharacterPalette.Identity;
            }
        for (var i = 0; i < _hairBounds.Length; i++)
            if (_hairBounds[i].Contains(mouse))
            {
                _hairIndex = i;
                _hairPalette = CharacterPalette.Identity;
            }

        for (var i = 0; i < _paletteBounds.Length; i++)
        {
            if (!_paletteBounds[i].Contains(mouse)) continue;
            _draggingPaletteIndex = i;
            SetPaletteFromPointer(i, mouse.X);
            return;
        }
        SyncPreview();
    }

    private void Create()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            _status = "Choose a character name before entering the world.";
            _name.Focused = true;
            return;
        }
        _busy = true;
        _status = "Creating character...";
        _nextBtn.Enabled = false;
        var appearance = CharacterAppearanceDataMapper.ToData(_preview.Appearance);
        _screens.Net.CreateCharacter(_name.Text.Trim(), CharacterCreationCatalog.Races[_raceIndex].Id, appearance);
    }

    private void OnWelcome(WelcomeData _) => _screens.Change(new WorldScreen(_screens));

    private void OnError(string msg)
    {
        _status = "Error: " + msg;
        _busy = false;
        _nextBtn.Enabled = true;
    }

    private void OnDisconnected()
    {
        var msg = _screens.Net.DisconnectMessage;
        if (string.IsNullOrWhiteSpace(msg))
            msg = "Lost connection to the server.";
        _status = msg;
        _busy = false;
        _nextBtn.Enabled = true;
        _screens.Net.ClearWorldSession();
        _screens.Change(new LoginScreen(_screens));
    }

    private void DrawHeader(SpriteBatch sb, SpriteFont font)
    {
        DrawCentered(sb, font, "CREATE YOUR ADVENTURER", new Vector2(_mainPanel.Center.X, _mainPanel.Y + 22), Gold);
        DrawCentered(sb, font, "Choose your race, shape your appearance, and begin your legend.", new Vector2(_mainPanel.Center.X, _mainPanel.Y + 50), Muted);
    }

    private void DrawPreview(SpriteBatch sb, SpriteFont font)
    {
        DrawPanel(sb, _previewPanel);
        DrawCentered(sb, font, "PREVIEW", new Vector2(_previewPanel.Center.X, _previewPanel.Y + 18), Gold);
        var pedestal = new Vector2(_previewPanel.Center.X, _previewPanel.Center.Y - 14);
        DrawPrimitives.FillCircle(sb, pedestal, 92, new Color(35, 42, 43));
        DrawPrimitives.DrawCircleOutline(sb, pedestal, 92, GoldDim, 24, 2);
        // Character frames use a foot pivot. Offset that pivot down by the
        // opaque sprite half-height so the artwork, not its feet, is centered.
        _preview.Draw(sb, pedestal + new Vector2(0, 78), Color.White, 4f);
        for (var i = 0; i < _directionBounds.Length; i++)
        {
            var active = _previewFacing == (i switch
            {
                0 => new Vector2(0, -1),
                1 => Vector2.UnitY,
                2 => -Vector2.UnitX,
                _ => Vector2.UnitX,
            });
            DrawRoundedButton(sb, _directionBounds[i], active ? new Color(36, 75, 101) : PanelInner, active ? Gold : GoldDim);
            DrawDirectionArrow(sb, _directionBounds[i], i, active ? Gold : Muted);
        }
        var animationLabels = new[] { "IDLE", "WALK", "RUN", "ATTACK" };
        for (var i = 0; i < _animationBounds.Length; i++)
        {
            var active = i == (int)_previewAnimation;
            DrawRoundedButton(sb, _animationBounds[i], active ? new Color(36, 75, 101) : PanelInner, active ? Gold : GoldDim);
            DrawFittedCentered(sb, font, animationLabels[i], _animationBounds[i].Center.ToVector2(), _animationBounds[i].Width - 8, active ? Gold : Muted, 0.7f);
        }
    }

    private void DrawContent(SpriteBatch sb, SpriteFont font)
    {
        DrawPanel(sb, _contentPanel);
        DrawCentered(sb, font, "CHOOSE YOUR RACE", new Vector2(_contentPanel.Center.X, _contentPanel.Y + 17), Gold);
        for (var i = 0; i < _raceBounds.Length; i++)
        {
            var selected = i == _raceIndex;
            DrawRoundedButton(sb, _raceBounds[i], selected ? new Color(30, 59, 82) : PanelInner, selected ? Gold : new Color(45, 57, 65));
            var race = CharacterCreationCatalog.Races[i];
            _racePortraits[i].Draw(sb, new Vector2(_raceBounds[i].X + 28, _raceBounds[i].Bottom - 3), Color.White, 1.1f);
            var textX = _raceBounds[i].X + 54;
            DrawFitted(sb, font, race.Name.ToUpperInvariant(), new Vector2(textX, _raceBounds[i].Y + 6), _raceBounds[i].Right - textX - 8, selected ? Gold : Color.White, 0.9f);
            DrawFitted(sb, font, race.Description, new Vector2(textX, _raceBounds[i].Y + 23), _raceBounds[i].Right - textX - 8, Muted, 0.75f);
        }
        DrawRaceDetails(sb, font);
    }

    private void DrawAppearance(SpriteBatch sb, SpriteFont font)
    {
        DrawPanel(sb, _appearancePanel);
        DrawCentered(sb, font, "APPEARANCE", new Vector2(_appearancePanel.Center.X, _appearancePanel.Y + 17), Gold);
        for (var i = 0; i < _genderBounds.Length; i++)
        {
            var selected = _gender == (i == 0 ? "male" : "female");
            DrawRoundedButton(sb, _genderBounds[i], selected ? new Color(36, 75, 101) : PanelInner, selected ? Gold : GoldDim);
            DrawGenderSymbol(sb, _genderBounds[i].Center.ToVector2(), i == 0, selected ? Gold : Muted);
        }
        DrawPicker(sb, font, "SKIN TONE", _skinBounds, (int)_skinTone, new[] { new Color(250, 211, 170), new Color(204, 145, 103), new Color(143, 96, 69), new Color(83, 53, 39) });
        DrawPicker(sb, font, "EYE COLOR", _eyeBounds, _eyeColor switch { EyeColor.Blue => 1, EyeColor.Green => 2, EyeColor.Gray => 3, _ => 0 }, new[] { new Color(91, 57, 34), new Color(65, 123, 190), new Color(73, 137, 80), new Color(170, 175, 180) });
        DrawFitted(sb, font, "HAIR STYLE", new Vector2(_appearancePanel.X + 18, _hairStyleNavBounds[0].Y - 16), _appearancePanel.Width - 36, Gold, 0.75f);
        DrawRoundedButton(sb, _hairStyleNavBounds[0], PanelInner, GoldDim);
        DrawRoundedButton(sb, _hairStyleNavBounds[1], PanelInner, GoldDim);
        DrawDirectionArrow(sb, _hairStyleNavBounds[0], 2, Muted);
        DrawDirectionArrow(sb, _hairStyleNavBounds[1], 3, Muted);
        DrawFittedCentered(sb, font, CharacterCreationCatalog.HairStyleNames[_hairStyleIndex].ToUpperInvariant(),
            new Vector2(_appearancePanel.Center.X, _hairStyleNavBounds[0].Center.Y), _appearancePanel.Width - 110, Color.White, 0.8f);
        DrawPicker(sb, font, "HAIR BASE", _hairBounds, _hairIndex, new[] { new Color(95, 57, 38), new Color(25, 24, 25), new Color(221, 184, 99), new Color(178, 70, 35) });
        DrawPaletteSlider(sb, font, "SKIN HUE", _paletteBounds[0], _skinPalette.Hue, 360);
        DrawPaletteSlider(sb, font, "SKIN BRIGHTNESS", _paletteBounds[1], _skinPalette.Brightness, 100);
        DrawPaletteSlider(sb, font, "EYE HUE", _paletteBounds[2], _eyePalette.Hue, 360);
        DrawPaletteSlider(sb, font, "EYE BRIGHTNESS", _paletteBounds[3], _eyePalette.Brightness, 100);
        DrawPaletteSlider(sb, font, "HAIR HUE", _paletteBounds[4], _hairPalette.Hue, 360);
        DrawPaletteSlider(sb, font, "HAIR BRIGHTNESS", _paletteBounds[5], _hairPalette.Brightness, 100);
        DrawRoundedButton(sb, _randomizeBounds, new Color(42, 51, 57), GoldDim);
        DrawFittedCentered(sb, font, "RANDOMIZE", _randomizeBounds.Center.ToVector2(), _randomizeBounds.Width - 12, Gold, 0.75f);
    }

    private void DrawPicker(SpriteBatch sb, SpriteFont font, string label, Rectangle[] bounds, int selected, Color[] colors)
    {
        sb.DrawString(font, label, new Vector2(_appearancePanel.X + 18, bounds[0].Y - 18), Gold);
        for (var i = 0; i < bounds.Length; i++)
        {
            DrawPrimitives.FillRect(sb, bounds[i], colors[i]);
            DrawPrimitives.DrawRectOutline(sb, bounds[i], i == selected ? Gold : new Color(58, 67, 76), i == selected ? 3 : 1);
        }
    }

    private static void DrawPaletteSlider(SpriteBatch sb, SpriteFont font, string label, Rectangle bounds, int value, int max)
    {
        DrawFitted(sb, font, label, new Vector2(bounds.X, bounds.Y - 14), bounds.Width, new Color(192, 199, 205), 0.72f);
        DrawPrimitives.FillRect(sb, bounds, new Color(36, 44, 51));
        var filled = new Rectangle(bounds.X, bounds.Y, Math.Max(2, bounds.Width * value / max), bounds.Height);
        DrawPrimitives.FillRect(sb, filled, new Color(83, 126, 154));
        DrawPrimitives.DrawRectOutline(sb, bounds, GoldDim);
        DrawPrimitives.FillCircle(sb, new Vector2(bounds.X + bounds.Width * value / max, bounds.Center.Y), 5, Gold);
    }

    private void DrawName(SpriteBatch sb, SpriteFont font)
    {
        DrawPanel(sb, _namePanel);
        DrawCentered(sb, font, "CHOOSE A NAME", new Vector2(_namePanel.Center.X, _namePanel.Y + 10), Gold);
        _name.Draw(sb, font);
        DrawPrimitives.FillRect(sb, _nameRandomizeBounds, new Color(42, 51, 57));
        DrawPrimitives.DrawRectOutline(sb, _nameRandomizeBounds, GoldDim);
        DrawCentered(sb, font, "?", new Vector2(_nameRandomizeBounds.Center.X, _nameRandomizeBounds.Y + 9), Gold);
    }

    private void UpdateButtons()
    {
        _nextBtn.Label = "CONFIRM";
    }

    private void DrawRaceDetails(SpriteBatch sb, SpriteFont font)
    {
        DrawPrimitives.FillRect(sb, _raceDetailBounds, PanelInner);
        DrawPrimitives.DrawRectOutline(sb, _raceDetailBounds, GoldDim);
        var race = CharacterCreationCatalog.Races[_raceIndex];
        DrawFitted(sb, font, race.Name.ToUpperInvariant(), new Vector2(_raceDetailBounds.X + 10, _raceDetailBounds.Y + 7), _raceDetailBounds.Width - 42, Gold, 0.9f);
        DrawFitted(sb, font, "A traveler of old legends.", new Vector2(_raceDetailBounds.X + 10, _raceDetailBounds.Y + 23), _raceDetailBounds.Width - 18, Muted, 0.7f);
        DrawPrimitives.FillCircle(sb, new Vector2(_raceDetailBounds.Right - 20, _raceDetailBounds.Y + 19), 11, new Color(37, 68, 86));
        DrawPrimitives.DrawCircleOutline(sb, new Vector2(_raceDetailBounds.Right - 20, _raceDetailBounds.Y + 19), 11, GoldDim, 12);
        if (_raceDetailBounds.Height > 48)
            DrawFitted(sb, font, "+2 STR    +2 DEX    +2 VIT", new Vector2(_raceDetailBounds.X + 10, _raceDetailBounds.Bottom - 17), _raceDetailBounds.Width - 18, new Color(118, 209, 134), 0.7f);
    }

    private void Randomize()
    {
        _raceIndex = Random.Shared.Next(CharacterCreationCatalog.Races.Length);
        _gender = Random.Shared.Next(2) == 0 ? "male" : "female";
        _skinTone = (SkinTone)Random.Shared.Next(_skinBounds.Length);
        _eyeColor = Random.Shared.Next(4) switch
        {
            1 => EyeColor.Blue,
            2 => EyeColor.Green,
            3 => EyeColor.Gray,
            _ => EyeColor.Brown,
        };
        _hairIndex = Random.Shared.Next(_hairBounds.Length);
        _hairStyleIndex = Random.Shared.Next(CharacterCreationCatalog.HairStyleNames.Length);
        _skinPalette = RandomPalette(55, 100);
        _eyePalette = RandomPalette(65, 100);
        _hairPalette = RandomPalette(25, 100);
        _name.Text = MythicNameGenerator.Next(_gender);
        SyncPreview();
    }

    private static CharacterPalette RandomPalette(int minBrightness, int maxBrightness) => new(
        Random.Shared.Next(361),
        Random.Shared.Next(45, 101),
        Random.Shared.Next(minBrightness, maxBrightness + 1));

    private void SyncRacePortraits()
    {
        for (var i = 0; i < _racePortraits.Length; i++)
        {
            _racePortraits[i].Appearance = new CharacterAppearance
            {
                RaceId = CharacterCreationCatalog.Races[i].Id,
                GenderId = "male",
                BodyTypeId = CharacterAnimationCatalog.FarmRpg,
                SkinTone = (SkinTone)(i % 4),
                EyeColor = (i % 3) switch { 1 => EyeColor.Blue, 2 => EyeColor.Green, _ => EyeColor.Brown },
                HairColor = HairColor.Brown,
                HairStyleId = CharacterCreationCatalog.HairStyleId(i % CharacterCreationCatalog.HairStyleNames.Length, i % 4),
                SkinPalette = CharacterPalette.Identity,
                EyePalette = CharacterPalette.Identity,
                HairPalette = CharacterPalette.Identity,
            };
            _racePortraits[i].HoldIdlePose(Vector2.UnitY);
        }
    }

    private void SyncPreview()
    {
        _preview.Appearance = new CharacterAppearance
        {
            RaceId = CharacterCreationCatalog.Races[_raceIndex].Id,
            GenderId = _gender,
            BodyTypeId = CharacterAnimationCatalog.FarmRpg,
            SkinTone = _skinTone,
            EyeColor = _eyeColor,
            HairColor = HairColor.Brown,
            HairStyleId = CharacterCreationCatalog.HairStyleId(_hairStyleIndex, _hairIndex),
            SkinPalette = _skinPalette,
            EyePalette = _eyePalette,
            HairPalette = _hairPalette,
        };
    }

    private static CharacterPalette SetPaletteValue(CharacterPalette palette, int component, int value) => component switch
    {
        0 => new CharacterPalette(value, palette.Saturation == 0 ? 100 : palette.Saturation, palette.Brightness).Clamp(),
        1 => new CharacterPalette(palette.Hue, value, palette.Brightness).Clamp(),
        _ => new CharacterPalette(palette.Hue, palette.Saturation, value).Clamp(),
    };

    private void SetPaletteFromPointer(int paletteIndex, int pointerX)
    {
        var bounds = _paletteBounds[paletteIndex];
        var isHue = paletteIndex % 2 == 0;
        var max = isHue ? 360 : 100;
        var value = (int)MathF.Round(Math.Clamp(pointerX - bounds.X, 0, bounds.Width) * max / (float)bounds.Width);
        if (paletteIndex < 2)
            _skinPalette = SetPaletteValue(_skinPalette, isHue ? 0 : 2, value);
        else if (paletteIndex < 4)
            _eyePalette = SetPaletteValue(_eyePalette, isHue ? 0 : 2, value);
        else
            _hairPalette = SetPaletteValue(_hairPalette, isHue ? 0 : 2, value);
        SyncPreview();
    }

    private static void DrawPanel(SpriteBatch sb, Rectangle panel)
    {
        FillRoundedRect(sb, panel, GoldDim, 6);
        FillRoundedRect(sb, new Rectangle(panel.X + 2, panel.Y + 2, panel.Width - 4, panel.Height - 4), Panel, 5);
        DrawPrimitives.DrawRectOutline(sb, new Rectangle(panel.X + 5, panel.Y + 5, panel.Width - 10, panel.Height - 10), new Color(33, 45, 54));
    }

    private static void DrawRoundedButton(SpriteBatch sb, Rectangle bounds, Color fill, Color border)
    {
        FillRoundedRect(sb, bounds, border, 4);
        FillRoundedRect(sb, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), fill, 3);
    }

    private static void FillRoundedRect(SpriteBatch sb, Rectangle rect, Color color, int radius)
    {
        radius = Math.Clamp(radius, 0, Math.Min(rect.Width, rect.Height) / 2);
        if (radius == 0)
        {
            DrawPrimitives.FillRect(sb, rect, color);
            return;
        }
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X + radius, rect.Y, rect.Width - radius * 2, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y + radius, rect.Width, rect.Height - radius * 2), color);
        DrawPrimitives.FillCircle(sb, new Vector2(rect.X + radius, rect.Y + radius), radius, color, 12);
        DrawPrimitives.FillCircle(sb, new Vector2(rect.Right - radius, rect.Y + radius), radius, color, 12);
        DrawPrimitives.FillCircle(sb, new Vector2(rect.X + radius, rect.Bottom - radius), radius, color, 12);
        DrawPrimitives.FillCircle(sb, new Vector2(rect.Right - radius, rect.Bottom - radius), radius, color, 12);
    }

    private static void DrawDirectionArrow(SpriteBatch sb, Rectangle bounds, int direction, Color color)
    {
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);
        var (tip, tail, wingA, wingB) = direction switch
        {
            0 => (center + new Vector2(0, -7), center + new Vector2(0, 7), center + new Vector2(-5, -1), center + new Vector2(5, -1)),
            1 => (center + new Vector2(0, 7), center + new Vector2(0, -7), center + new Vector2(-5, 1), center + new Vector2(5, 1)),
            2 => (center + new Vector2(-7, 0), center + new Vector2(7, 0), center + new Vector2(-1, -5), center + new Vector2(-1, 5)),
            _ => (center + new Vector2(7, 0), center + new Vector2(-7, 0), center + new Vector2(1, -5), center + new Vector2(1, 5)),
        };
        DrawPrimitives.DrawLine(sb, tail, tip, color, 2);
        DrawPrimitives.DrawLine(sb, tip, wingA, color, 2);
        DrawPrimitives.DrawLine(sb, tip, wingB, color, 2);
    }

    private static void DrawGenderSymbol(SpriteBatch sb, Vector2 center, bool male, Color color)
    {
        var circleCenter = male ? center + new Vector2(-2, 2) : center + new Vector2(0, -3);
        DrawPrimitives.DrawCircleOutline(sb, circleCenter, 6, color, 12, 2);
        if (male)
        {
            var tip = circleCenter + new Vector2(10, -10);
            DrawPrimitives.DrawLine(sb, circleCenter + new Vector2(4, -4), tip, color, 2);
            DrawPrimitives.DrawLine(sb, tip, tip + new Vector2(-6, 0), color, 2);
            DrawPrimitives.DrawLine(sb, tip, tip + new Vector2(0, 6), color, 2);
        }
        else
        {
            var stemStart = circleCenter + new Vector2(0, 6);
            var stemEnd = stemStart + new Vector2(0, 9);
            DrawPrimitives.DrawLine(sb, stemStart, stemEnd, color, 2);
            DrawPrimitives.DrawLine(sb, stemEnd + new Vector2(-4, -3), stemEnd + new Vector2(4, -3), color, 2);
        }
    }

    private static void DrawFitted(SpriteBatch sb, SpriteFont font, string text, Vector2 position, float maxWidth, Color color, float preferredScale = 1f)
    {
        var filtered = SpriteFontSafe.Filter(text);
        var measured = font.MeasureString(filtered).X;
        var scale = measured <= 0 ? preferredScale : Math.Min(preferredScale, maxWidth / measured);
        sb.DrawString(font, filtered, position, color, 0f, Vector2.Zero, Math.Max(0.5f, scale), SpriteEffects.None, 0f);
    }

    private static void DrawFittedCentered(SpriteBatch sb, SpriteFont font, string text, Vector2 center, float maxWidth, Color color, float preferredScale)
    {
        var filtered = SpriteFontSafe.Filter(text);
        var size = font.MeasureString(filtered);
        var scale = size.X <= 0 ? preferredScale : Math.Min(preferredScale, maxWidth / size.X);
        scale = Math.Max(0.5f, scale);
        sb.DrawString(font, filtered, center - size * scale / 2f, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static void DrawCentered(SpriteBatch sb, SpriteFont font, string text, Vector2 anchor, Color color)
    {
        var size = font.MeasureString(SpriteFontSafe.Filter(text));
        sb.DrawString(font, SpriteFontSafe.Filter(text), anchor - size / 2, color);
    }
}
