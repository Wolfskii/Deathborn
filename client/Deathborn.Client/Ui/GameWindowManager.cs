using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Deathborn.Client.Ui;

public sealed class GameWindowManager
{
    private readonly List<UiWindow> _windows = [];

    public CharacterWindow Character { get; }

    public GameWindowManager()
    {
        Character = new CharacterWindow();
        Register(Character);
    }

    public void Register(UiWindow window)
    {
        window.OnBringToFront = () => BringToFront(window);
        _windows.Add(window);
    }

    private void BringToFront(UiWindow window)
    {
        _windows.Remove(window);
        _windows.Add(window);
    }

    public void OpenCharacter() => Character.Open();

    /// <summary>True when an open window is under the mouse (blocks world clicks).</summary>
    public bool Update(MouseState mouse, MouseState prevMouse, KeyboardState kb, KeyboardState prevKb, bool allowShortcuts)
    {
        var captures = false;
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            if (_windows[i].Update(mouse, prevMouse, kb, prevKb, allowShortcuts))
                captures = true;
        }
        return captures;
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        foreach (var window in _windows)
            window.Draw(sb, font);
    }

    public bool AnyOpen
    {
        get
        {
            foreach (var w in _windows)
                if (w.IsOpen) return true;
            return false;
        }
    }
}
