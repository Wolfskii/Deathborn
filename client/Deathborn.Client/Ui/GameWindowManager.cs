using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Deathborn.Client.Ui;

public sealed class GameWindowManager
{
    private readonly List<UiWindow> _windows = [];

    public CharacterWindow Character { get; }
    public SpellBookWindow SpellBook { get; }
    public InventoryWindow Inventory { get; }
    public SkillsWindow Skills { get; }
    public FriendsWindow Friends { get; }

    public GameWindowManager()
    {
        Character = new CharacterWindow();
        SpellBook = new SpellBookWindow();
        Inventory = new InventoryWindow();
        Skills = new SkillsWindow();
        Friends = new FriendsWindow();
        Register(Character);
        Register(SpellBook);
        Register(Inventory);
        Register(Skills);
        Register(Friends);
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
    public void OpenSpellBook() => SpellBook.Open();
    public void OpenInventory() => Inventory.Open();
    public void OpenSkills() => Skills.Open();
    public void OpenFriends() => Friends.Open();

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

    public bool IsPointOverOpenWindow(Point p)
    {
        foreach (var w in _windows)
            if (w.IsOpen && w.Bounds.Contains(p))
                return true;
        return false;
    }
}
