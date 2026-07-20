using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Diagnostics;
using Deathborn.Client.Maps;
using Deathborn.Client.Net;
using Deathborn.Client.Platform;
using Deathborn.Client.Rendering;
using Deathborn.Client.Screens;
using Deathborn.Client.Ui;

namespace Deathborn.Client;

    /// <summary>MonoGame entry point — screens, audio, and authoritative client networking.</summary>
public sealed class DeathbornGame : Game
{
    public static DeathbornGame Instance { get; private set; } = null!;

    private readonly GraphicsDeviceManager _graphics;
    private readonly FpsCounter _fps = new();
    private SpriteBatch _spriteBatch = null!;
    private ScreenManager _screens = null!;
    private KeyboardState _prevKb;
    private bool _wasActive = true;
    private bool _borderlessFullscreen;
    private bool _applyingGraphicsChanges;
    private bool _reloadingGraphics;
    private int _suppressClientSizeChanged;
    private int _windowedWidth = Config.DefaultWidth;
    private int _windowedHeight = Config.DefaultHeight;
    private Point _windowedPosition;

    public SpriteFont Font { get; private set; } = null!;
    public int Fps => _fps.Fps;
    public GameClient Client { get; } = new();

    public DeathbornGame()
    {
        Instance = this;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Config.DefaultWidth,
            PreferredBackBufferHeight = Config.DefaultHeight,
            HardwareModeSwitch = false,
            SynchronizeWithVerticalRetrace = false,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        TargetElapsedTime = TimeSpan.FromMilliseconds(1);
        InactiveSleepTime = TimeSpan.Zero;
    }

    public SpriteBatch SpriteBatch => _spriteBatch;

    protected override void Initialize()
    {
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;

        if (Config.DevInstance > 0)
        {
            Window.Title = $"DEATHBORN ({Config.DevInstance})";
            Window.Position = new Point((Config.DevInstance - 1) * (Config.DefaultWidth + 12), 40);
        }

        Window.TextInput += OnTextInput;
        _screens = new ScreenManager(this);
        ServerEndpoints.EnsureResolvedAsync().GetAwaiter().GetResult();
        GraphicsDevice.DeviceReset += (_, _) => ReloadGraphicsAssets();
        base.Initialize();
        SyncViewport();
    }

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        TextField.Active?.AppendCharacter(e.Character);
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        if (_applyingGraphicsChanges || _borderlessFullscreen || _suppressClientSizeChanged > 0) return;

        var w = Window.ClientBounds.Width;
        var h = Window.ClientBounds.Height;
        if (w < 320 || h < 240) return;
        if (w == _graphics.PreferredBackBufferWidth && h == _graphics.PreferredBackBufferHeight)
        {
            SyncViewport();
            return;
        }

        _applyingGraphicsChanges = true;
        try
        {
            _graphics.PreferredBackBufferWidth = w;
            _graphics.PreferredBackBufferHeight = h;
            _graphics.ApplyChanges();
            RefreshGraphicsResources();
            SyncViewport();
        }
        finally
        {
            _applyingGraphicsChanges = false;
        }
    }

    private void SyncViewport() => GameViewport.SyncFrom(GraphicsDevice);

    public bool IsMouseOverClient(Point clientPosition) =>
        clientPosition.X >= 0 && clientPosition.Y >= 0
        && clientPosition.X < GameViewport.Width && clientPosition.Y < GameViewport.Height;

    /// <summary>
    /// True when MonoGame considers the game focused. Prefer this for keyboard.
    /// Mouse world clicks also use <see cref="WindowInputFocus.IsPointerOverClient"/>.
    /// </summary>
    public bool HasGameplayInputFocus => IsActive;

    /// <summary>
    /// Left-click edge on the focused game client. Requires MonoGame focus, cursor in the
    /// client bounds, and OS hit-test over this process (blocks clicks into other apps).
    /// </summary>
    public bool IsWorldMouseClick(MouseState mouse, MouseState prevMouse) =>
        IsActive
        && IsMouseOverClient(mouse.Position)
        && WindowInputFocus.IsPointerOverClient(Window, GameViewport.Width, GameViewport.Height)
        && mouse.LeftButton == ButtonState.Pressed
        && prevMouse.LeftButton == ButtonState.Released;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        DrawPrimitives.Init(GraphicsDevice);
        Font = Content.Load<SpriteFont>("Fonts/Default");
        LoadGraphicsAssets();
        AudioSettings.Load();
        MusicPlayer.ApplySavedSettings();
        SfxPlayer.Load(Content);
        SfxPlayer.ApplySavedSettings();
        _screens.Change(Config.DevAutoRestore && SavedLogin.HasRemembered()
            ? new DevReconnectScreen(_screens)
            : new LoginScreen(_screens));
        SyncViewport();
        GameWindowIcon.Apply(this);
    }

    private void LoadGraphicsAssets()
    {
        Rendering.Characters.FarmRpgCharacterSprites.Load(Content);
        AbilityIconAtlas.Load(Content);
        CosmeticIconAtlas.Load(Content);
        UiCursorTheme.Load(Content);
        DungeonFloorTiles.Load(Content);
        WaterTiles.Load(Content);
        TerrainLandTiles.Load(Content);
        FarmRpgTerrain.Load(Content);
        FarmRpgGrassProps.Load(Content);
        WorldFoliage.Load(Content);
        WorldClouds.Load(Content);
        TinyRpgCharacterSprites.Load(Content);
        FarmRpgSlimeSprites.Load(Content);
        ProjectileSprites.Load(Content);
        TinySwordsUi.Load(Content);
        FarmRpgInventoryUi.Load(Content);
        TiledMapCatalog.Load(Content, GraphicsDevice);
    }

    private void ReloadGraphicsAssets()
    {
        if (_reloadingGraphics) return;

        _reloadingGraphics = true;
        try
        {
            Rendering.Characters.FarmRpgCharacterSprites.ClearCache();
            TiledMapCatalog.Clear();
            SfxPlayer.ClearCache();
            Content.Unload();
            Font = Content.Load<SpriteFont>("Fonts/Default");
            LoadGraphicsAssets();
            SfxPlayer.Load(Content);
            RefreshGraphicsResources();
            SyncViewport();
        }
        finally
        {
            _reloadingGraphics = false;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        DevPerfLog.BeginFrame();
        SyncViewport();

        var kb = Keyboard.GetState();
        var active = IsActive;

        // Swallow keyboard edges when the window regains focus (same as WorldScreen mouse swallow).
        if (active && !_wasActive)
            _prevKb = kb;

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        if (kb.IsKeyDown(Keys.Escape) && !_prevKb.IsKeyDown(Keys.Escape) && !_screens.TryHandleEscape())
            Exit();

        _screens.Update(gameTime);

        if (_suppressClientSizeChanged > 0)
            _suppressClientSizeChanged--;

        _fps.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        var textInputActive = TextField.Active != null;
        if (!textInputActive
            && ((kb.IsKeyDown(Keys.F11) && !_prevKb.IsKeyDown(Keys.F11))
                || ((kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt))
                    && kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))))
        {
            ToggleFullscreen();
        }

        DevPerfLog.Mark("audio");
        MusicPlayer.Update();
        SfxPlayer.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        _prevKb = kb;
        _wasActive = active;
        base.Update(gameTime);
        DevPerfLog.EndUpdate();
    }

    private void ToggleFullscreen()
    {
        _applyingGraphicsChanges = true;
        _suppressClientSizeChanged = 3;
        try
        {
            if (!_borderlessFullscreen)
            {
                _windowedWidth = Math.Max(320, _graphics.PreferredBackBufferWidth);
                _windowedHeight = Math.Max(240, _graphics.PreferredBackBufferHeight);
                _windowedPosition = Window.Position;

                var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                Window.AllowUserResizing = false;
                _borderlessFullscreen = true;
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = display.Width;
                _graphics.PreferredBackBufferHeight = display.Height;
                _graphics.ApplyChanges();
                Window.Position = Point.Zero;
            }
            else
            {
                _borderlessFullscreen = false;
                Window.AllowUserResizing = true;
                _graphics.IsFullScreen = false;
                _graphics.PreferredBackBufferWidth = _windowedWidth;
                _graphics.PreferredBackBufferHeight = _windowedHeight;
                _graphics.ApplyChanges();
                Window.Position = _windowedPosition;
            }

            RefreshGraphicsResources();
            SyncViewport();
        }
        finally
        {
            _applyingGraphicsChanges = false;
        }
    }

    private void RefreshGraphicsResources()
    {
        DrawPrimitives.Init(GraphicsDevice);
        _spriteBatch.Dispose();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _screens.Draw(gameTime);
        base.Draw(gameTime);
        DevPerfLog.EndDraw();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.ClientSizeChanged -= OnClientSizeChanged;
            Client.Dispose();
        }
        base.Dispose(disposing);
    }
}
