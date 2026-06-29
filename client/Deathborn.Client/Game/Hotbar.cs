using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Gameplay;

public sealed class HotbarSlot
{
    public string KeyLabel = "?";
    public Dictionary<string, object>? Entry;
    public bool Flash;
    private float _flashT;

    public void Update(float dt)
    {
        if (!Flash) return;
        _flashT += dt;
        if (_flashT > 0.2f) { Flash = false; _flashT = 0; }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Rectangle bounds)
    {
        var active = Flash;
        var bg = active ? new Color(55, 70, 95) : new Color(20, 20, 26);
        DrawPrimitives.FillRect(sb, bounds, bg);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), bg);
        var border = active ? new Color(190, 215, 255) : new Color(90, 95, 105);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);

        var icon = bounds with { X = bounds.X + 4, Y = bounds.Y + 4, Width = bounds.Width - 8, Height = bounds.Height - 22 };
        var iconCol = Entry != null && Entry.TryGetValue("color", out var c) && c is Color col
            ? col : new Color(30, 30, 36);
        DrawPrimitives.FillRect(sb, icon, iconCol);

        if (Entry != null && Entry.TryGetValue("name", out var n))
            sb.DrawString(font, n.ToString()!, new Vector2(bounds.X + 4, bounds.Bottom - 16), Color.White);

        sb.DrawString(font, KeyLabel, new Vector2(bounds.X + 4, bounds.Y + 2), new Color(215, 215, 190));
    }
}

public sealed class Hotbar
{
    public static readonly string[] KeyLabels = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
    private static readonly Keys[] HotbarKeys =
    [
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5,
        Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0,
    ];

    public readonly HotbarSlot[] Slots = new HotbarSlot[10];
    public event Action<int, Dictionary<string, object>?>? SlotActivated;

    public Hotbar()
    {
        for (var i = 0; i < 10; i++)
        {
            Slots[i] = new HotbarSlot { KeyLabel = KeyLabels[i] };
        }
    }

    public void SetSlot(int index, Dictionary<string, object>? entry) => Slots[index].Entry = entry;

    public void Update(float dt, KeyboardState kb, KeyboardState prevKb)
    {
        foreach (var slot in Slots) slot.Update(dt);
        for (var i = 0; i < 10; i++)
        {
            if (kb.IsKeyDown(HotbarKeys[i]) && !prevKb.IsKeyDown(HotbarKeys[i]))
                Activate(i);
        }
    }

    public void Activate(int index)
    {
        Slots[index].Flash = true;
        SlotActivated?.Invoke(index, Slots[index].Entry);
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        const int slotW = 52, slotH = 52, gap = 6;
        var totalW = 10 * slotW + 9 * gap;
        var x0 = Config.Width / 2 - totalW / 2;
        var y = Config.Height - 88;

        DrawPrimitives.FillRect(sb, new Rectangle(x0 - 8, y - 8, totalW + 16, slotH + 16), new Color(12, 15, 20, 220));

        for (var i = 0; i < 10; i++)
        {
            var rect = new Rectangle(x0 + i * (slotW + gap), y, slotW, slotH);
            Slots[i].Draw(sb, font, rect);
        }
    }
}
