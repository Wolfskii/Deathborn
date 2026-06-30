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
    public float CooldownRemaining;
    public float CooldownTotal;

    private float _flashT;

    public bool IsOnCooldown => CooldownRemaining > 0.001f;

    public void StartCooldown(float seconds)
    {
        if (seconds <= 0f) return;
        CooldownRemaining = seconds;
        CooldownTotal = seconds;
    }

    public void Update(float dt)
    {
        if (CooldownRemaining > 0f)
            CooldownRemaining = MathF.Max(0f, CooldownRemaining - dt);

        if (!Flash) return;
        _flashT += dt;
        if (_flashT > 0.2f) { Flash = false; _flashT = 0; }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Rectangle bounds)
    {
        var onCooldown = IsOnCooldown;
        var active = Flash && !onCooldown;
        var bg = active ? new Color(55, 70, 95) : new Color(20, 20, 26);
        DrawPrimitives.FillRect(sb, bounds, bg);

        var border = active ? new Color(190, 215, 255) : new Color(90, 95, 105);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
        DrawPrimitives.FillRect(sb, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);

        var icon = new Rectangle(bounds.X + 5, bounds.Y + 16, bounds.Width - 10, bounds.Height - 30);
        var spellId = Entry?.GetValueOrDefault(HotbarEntry.IdKey) as string;
        HotbarIconDraw.Draw(sb, spellId, icon);

        sb.DrawString(font, KeyLabel, new Vector2(bounds.X + 5, bounds.Y + 3), new Color(215, 215, 190));

        if (Entry != null && Entry.TryGetValue("name", out var n))
            sb.DrawString(font, n.ToString()!, new Vector2(bounds.X + 5, bounds.Bottom - 15), Color.White);

        if (onCooldown && CooldownTotal > 0f)
            DrawCooldownOverlay(sb, font, bounds);
    }

    private void DrawCooldownOverlay(SpriteBatch sb, SpriteFont font, Rectangle bounds)
    {
        var progress = 1f - CooldownRemaining / CooldownTotal;
        progress = MathHelper.Clamp(progress, 0f, 1f);

        var lineY = bounds.Bottom - progress * bounds.Height;
        var coverHeight = (int)MathF.Ceiling(lineY - bounds.Y);
        if (coverHeight > 0)
        {
            var cover = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, coverHeight);
            DrawPrimitives.FillRect(sb, cover, new Color(0, 0, 0, 0.42f));
        }

        var line = new Rectangle(bounds.X + 2, (int)lineY - 1, bounds.Width - 4, 2);
        DrawPrimitives.FillRect(sb, line, new Color(210, 185, 95, 0.85f));

        var label = FormatCooldownLabel(CooldownRemaining);
        var size = font.MeasureString(label);
        var textPos = new Vector2(bounds.Center.X - size.X / 2f, bounds.Center.Y - size.Y / 2f + 4f);
        DrawPrimitives.FillRect(sb,
            new Rectangle((int)textPos.X - 3, (int)textPos.Y - 1, (int)size.X + 6, (int)size.Y + 2),
            new Color(0, 0, 0, 0.5f));
        sb.DrawString(font, label, textPos, new Color(245, 240, 220));
    }

    private static string FormatCooldownLabel(float remaining) =>
        remaining >= 3f ? MathF.Ceiling(remaining).ToString("0")
        : remaining >= 0.05f ? remaining.ToString("0.0")
        : "0";
}

public sealed class Hotbar
{
    public const int SlotWidth = 60;
    public const int SlotHeight = 60;
    public const int SlotGap = 7;
    public const int BarPadding = 10;

    public static readonly string[] KeyLabels = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
    private static readonly Keys[] HotbarKeys =
    [
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5,
        Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0,
    ];

    public readonly HotbarSlot[] Slots = new HotbarSlot[10];
    public event Action<int, Dictionary<string, object>?>? SlotActivated;
    public event Action<int, float>? CooldownBlocked;

    public Hotbar()
    {
        for (var i = 0; i < 10; i++)
            Slots[i] = new HotbarSlot { KeyLabel = KeyLabels[i] };
    }

    public void SetSlot(int index, Dictionary<string, object>? entry) => Slots[index].Entry = entry;

    public void Update(float dt, KeyboardState kb, KeyboardState prevKb, bool acceptInput = true)
    {
        foreach (var slot in Slots) slot.Update(dt);
        if (!acceptInput) return;

        for (var i = 0; i < 10; i++)
        {
            if (kb.IsKeyDown(HotbarKeys[i]) && !prevKb.IsKeyDown(HotbarKeys[i]))
                TryActivate(i);
        }
    }

    public bool TryActivate(int index)
    {
        var slot = Slots[index];
        if (slot.IsOnCooldown)
        {
            CooldownBlocked?.Invoke(index, slot.CooldownRemaining);
            return false;
        }

        slot.Flash = true;
        SlotActivated?.Invoke(index, slot.Entry);
        return true;
    }

    public void StartCooldown(int index, float seconds) => Slots[index].StartCooldown(seconds);

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        var totalW = 10 * SlotWidth + 9 * SlotGap;
        var x0 = GameViewport.Width / 2 - totalW / 2;
        var y = GameViewport.Height - SlotHeight - 28;

        DrawPrimitives.FillRect(sb,
            new Rectangle(x0 - BarPadding, y - BarPadding, totalW + BarPadding * 2, SlotHeight + BarPadding * 2),
            new Color(12, 15, 20, 220));

        for (var i = 0; i < 10; i++)
        {
            var rect = new Rectangle(x0 + i * (SlotWidth + SlotGap), y, SlotWidth, SlotHeight);
            Slots[i].Draw(sb, font, rect);
        }
    }
}
