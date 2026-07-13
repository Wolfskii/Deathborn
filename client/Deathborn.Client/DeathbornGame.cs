using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
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
        base.Initialize();
        SyncViewport();
    }

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        TextField.Active?.AppendCharacter(e.Character);
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        var w = Window.ClientBounds.Width;
        var h = Window.ClientBounds.Height;
        if (w <= 0 || h <= 0) return;

        _graphics.PreferredBackBufferWidth = w;
        _graphics.PreferredBackBufferHeight = h;
        _graphics.ApplyChanges();
        SyncViewport();
    }

    private void SyncViewport() => GameViewport.SyncFrom(GraphicsDevice);

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        DrawPrimitives.Init(GraphicsDevice);
        Font = Content.Load<SpriteFont>("Fonts/Default");
        CharacterSprites.Load(Content);
        AbilityIconAtlas.Load(Content);
        CosmeticIconAtlas.Load(Content);
        UiCursorTheme.Load(Content);
        DungeonFloorTiles.Load(Content);
        WaterTiles.Load(Content);
        TerrainLandTiles.Load(Content);
        TinySwordsTerrain.Load(Content);
        WorldFoliage.Load(Content);
        WorldClouds.Load(Content);
        TinyRpgCharacterSprites.Load(Content);
        ProjectileSprites.Load(Content);
        TinySwordsUi.Load(Content);
        MusicPlayer.ApplySavedSettings();
        _screens.Change(new LoginScreen(_screens));
        SyncViewport();
        GameWindowIcon.Apply(this);
    }

    protected override void Update(GameTime gameTime)
    {
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

        _fps.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        var textInputActive = TextField.Active != null;
        if (!textInputActive
            && ((kb.IsKeyDown(Keys.F11) && !_prevKb.IsKeyDown(Keys.F11))
                || ((kb.IsKeyDown(Keys.LeftAlt) || kb.IsKeyDown(Keys.RightAlt))
                    && kb.IsKeyDown(Keys.Enter) && !_prevKb.IsKeyDown(Keys.Enter))))
        {
            ToggleFullscreen();
        }

        MusicPlayer.Update();
        _prevKb = kb;
        _wasActive = active;
        base.Update(gameTime);
    }

    private void ToggleFullscreen()
    {
        _graphics.IsFullScreen = !_graphics.IsFullScreen;
        _graphics.SynchronizeWithVerticalRetrace = false;

        if (!_graphics.IsFullScreen)
        {
            _graphics.PreferredBackBufferWidth = Config.DefaultWidth;
            _graphics.PreferredBackBufferHeight = Config.DefaultHeight;
        }

        _graphics.ApplyChanges();
        SyncViewport();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _screens.Draw(gameTime);
        base.Draw(gameTime);
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
