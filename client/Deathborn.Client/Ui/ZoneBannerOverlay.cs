using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Top-center banner for zone entry and PvP status notices.</summary>
public sealed class ZoneBannerOverlay
{
    private string _title = "";
    private string _subtitle = "";
    private float _timer;
    private float _fade = 1f;
    private bool _active;

    public string? PersistentZoneName { get; private set; }
    public bool InSafeZone { get; private set; }

    public void ShowEnter(string zoneName, string subtitle)
    {
        _title = zoneName;
        _subtitle = subtitle;
        _timer = 4.5f;
        _fade = 1f;
        _active = true;
    }

    public void SetPersistentZone(string? zoneName, bool safe)
    {
        PersistentZoneName = zoneName;
        InSafeZone = safe;
    }

    public void Update(float dt)
    {
        if (!_active) return;
        _timer -= dt;
        if (_timer <= 1.2f)
            _fade = MathF.Max(0f, _timer / 1.2f);
        if (_timer <= 0f)
        {
            _active = false;
            _fade = 0f;
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (InSafeZone && !string.IsNullOrEmpty(PersistentZoneName))
            DrawPersistentPill(sb, font);

        if (!_active || _fade <= 0.01f) return;

        var vw = GameViewport.Width;
        var titleSize = font.MeasureString(_title);
        var subSize = font.MeasureString(_subtitle);
        var padX = 18;
        var padY = 10;
        var w = (int)MathF.Max(titleSize.X, subSize.X) + padX * 2;
        var h = padY * 2 + font.LineSpacing * 2 + 4;
        var panel = new Rectangle(vw / 2 - w / 2, 18, w, h);

        var bg = new Color(14, 16, 24, (int)(220 * _fade));
        var border = new Color(200, 165, 90, (int)(255 * _fade));
        var titleColor = new Color(240, 225, 180, (int)(255 * _fade));
        var subColor = new Color(170, 210, 175, (int)(255 * _fade));

        DrawPrimitives.FillRect(sb, panel, bg);
        DrawBorder(sb, panel, border);

        sb.DrawString(font, _title,
            new Vector2(panel.Center.X - titleSize.X / 2f, panel.Y + padY),
            titleColor);
        sb.DrawString(font, _subtitle,
            new Vector2(panel.Center.X - subSize.X / 2f, panel.Y + padY + font.LineSpacing + 2),
            subColor);
    }

    private void DrawPersistentPill(SpriteBatch sb, SpriteFont font)
    {
        var label = $"{PersistentZoneName} · Safe";
        var size = font.MeasureString(label);
        var padX = 10;
        var padY = 4;
        var rect = new Rectangle(
            GameViewport.Width / 2 - (int)(size.X / 2) - padX,
            4,
            (int)size.X + padX * 2,
            (int)size.Y + padY * 2);

        DrawPrimitives.FillRect(sb, rect, new Color(18, 28, 22, 190));
        DrawBorder(sb, rect, new Color(80, 160, 110, 200));
        sb.DrawString(font, label, new Vector2(rect.X + padX, rect.Y + padY), new Color(140, 220, 160));
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }
}
