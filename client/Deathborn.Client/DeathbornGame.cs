using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Audio;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;
using Deathborn.Client.Screens;
using Deathborn.Client.Ui;

namespace Deathborn.Client;

public sealed class DeathbornGame : Game
{
    public static DeathbornGame Instance { get; private set; } = null!;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private ScreenManager _screens = null!;

    public SpriteFont Font { get; private set; } = null!;
    public GameClient Client { get; } = new();

    public DeathbornGame()
    {
        Instance = this;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = Config.Width,
            PreferredBackBufferHeight = Config.Height,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public SpriteBatch SpriteBatch => _spriteBatch;

    protected override void Initialize()
    {
        if (Config.DevInstance > 0)
        {
            Window.Title = $"DEATHBORN ({Config.DevInstance})";
            Window.Position = new Point((Config.DevInstance - 1) * (Config.Width + 12), 40);
        }

        Window.TextInput += OnTextInput;
        _screens = new ScreenManager(this);
        base.Initialize();
    }

    private static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        TextField.Active?.AppendCharacter(e.Character);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        DrawPrimitives.Init(GraphicsDevice);
        Font = Content.Load<SpriteFont>("Fonts/Default");
        MusicPlayer.ApplySavedSettings();
        _screens.Change(new LoginScreen(_screens));
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _screens.Update(gameTime);
        MusicPlayer.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _screens.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Client.Dispose();
        base.Dispose(disposing);
    }
}
