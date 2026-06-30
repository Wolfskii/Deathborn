using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Net;
using Deathborn.Client.Ui;

namespace Deathborn.Client.Screens;

public sealed class CharacterCreateScreen : IScreen
{
    private readonly ScreenManager _screens;
    private readonly TextField _name = new() { Placeholder = "character name" };
    private readonly Button _createBtn = new() { Label = "Enter the world" };
    private string _status = "";
    private bool _busy;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private Point _mouse;
    private int _viewportW;
    private int _viewportH;

    public CharacterCreateScreen(ScreenManager screens) => _screens = screens;

    public void OnEnter()
    {
        Layout();
        _name.Focused = true;
        _status = "";
        _busy = false;
        _createBtn.Enabled = true;

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

        if (!_busy && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            _name.Focused = _name.Bounds.Contains(_mouse);
            if (_createBtn.Contains(_mouse)) Create();
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
        var cx = GameViewport.Width / 2f;

        sb.Begin();
        sb.DrawString(font, "Create your character", new Vector2(cx - 90, 180), Color.White);
        sb.DrawString(font, "This life is permanent. Choose a name.", new Vector2(cx - 130, 210), new Color(180, 180, 190));
        _name.Draw(sb, font);
        _createBtn.Draw(sb, font, _createBtn.Contains(_mouse));
        if (!string.IsNullOrEmpty(_status))
            sb.DrawString(font, _status, new Vector2(cx - 180, 380), new Color(220, 180, 120));
        sb.End();
    }

    private void Layout()
    {
        _viewportW = GameViewport.Width;
        _viewportH = GameViewport.Height;
        var cx = GameViewport.Width / 2;
        _name.Bounds = new Rectangle(cx - 180, 260, 360, 36);
        _createBtn.Bounds = new Rectangle(cx - 180, 320, 360, 36);
    }

    private void Create()
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) return;
        _busy = true;
        _status = "Creating character...";
        _createBtn.Enabled = false;
        _screens.Net.CreateCharacter(_name.Text.Trim());
    }

    private void OnWelcome(WelcomeData _) => _screens.Change(new WorldScreen(_screens));

    private void OnError(string msg)
    {
        _status = "Error: " + msg;
        _busy = false;
        _createBtn.Enabled = true;
    }

    private void OnDisconnected()
    {
        if (!_busy) return;
        _status = "Error: lost connection to server";
        _busy = false;
        _createBtn.Enabled = true;
    }
}
