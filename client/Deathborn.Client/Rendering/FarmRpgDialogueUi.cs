using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Farm RPG dialogue box sheet — chat panels, speech bubbles, typing indicators.</summary>
public static class FarmRpgDialogueUi
{
    // dialogue_box.png — 144×96 sheet.
    public static readonly Rectangle PanelScalloped = new(0, 0, 48, 48);
    public static readonly Rectangle PanelRounded = new(0, 48, 48, 48);

    /// <summary>Empty rounded speech bubbles (narrow → wide), including bottom tail.</summary>
    public static readonly Rectangle[] SpeechRounded =
    [
        new(53, 73, 7, 7),
        new(67, 69, 10, 11),
        new(82, 67, 12, 13),
    ];

    /// <summary>Empty scalloped speech bubbles (narrow → wide), including bottom tail.</summary>
    public static readonly Rectangle[] SpeechScalloped =
    [
        new(53, 57, 7, 7),
        new(67, 53, 10, 11),
        new(82, 51, 12, 13),
        new(96, 48, 16, 16),
    ];

    /// <summary>Pre-filled ellipsis bubbles used while a player is typing.</summary>
    public static readonly Rectangle[] TypingEllipsis =
    [
        new(65, 16, 14, 15),
        new(81, 16, 14, 15),
        new(97, 16, 14, 15),
        new(113, 16, 15, 15),
    ];

    /// <summary>Small thought-trail dots above typing bubbles.</summary>
    public static readonly Rectangle[] ThoughtDots =
    [
        new(61, 12, 3, 3),
        new(76, 12, 4, 4),
        new(93, 13, 3, 3),
        new(108, 12, 4, 4),
    ];

    private const int PanelBorder = 8;
    private static Texture2D? _sheet;

    public static bool IsLoaded => _sheet != null;

    public static void Load(ContentManager content)
    {
        _sheet = null;
        try
        {
            _sheet = content.Load<Texture2D>("Ui/FarmRpg/dialogue_box");
        }
        catch
        {
            // Content may not be built yet.
        }
    }

    public static void DrawPanel(SpriteBatch sb, Rectangle dest, float alpha = 1f, bool scalloped = false)
    {
        if (_sheet == null) return;
        var src = scalloped ? PanelScalloped : PanelRounded;
        DrawNineSlice(sb, dest, src, PanelBorder, alpha);
    }

    /// <summary>Speech bubble sized to text, with Farm RPG peach panel + tip pointing at the speaker.</summary>
    public static void DrawSpeechBubble(
        SpriteBatch sb,
        Rectangle body,
        Vector2 tailTip,
        float alpha = 1f)
    {
        if (_sheet == null) return;

        DrawNineSlice(sb, body, PanelRounded, PanelBorder, alpha);

        // Tip from the largest rounded speech sprite (bottom-center of that glyph).
        var tipSrc = SpeechRounded[^1];
        var tipH = Math.Max(4, (int)(tipSrc.Height * 0.45f));
        var tipSrcRect = new Rectangle(tipSrc.X, tipSrc.Bottom - tipH, tipSrc.Width, tipH);
        var tipW = Math.Max(6, (int)(body.Width * 0.14f));
        var tipDest = new Rectangle(
            (int)(tailTip.X - tipW * 0.35f),
            body.Bottom - 1,
            tipW,
            Math.Max(5, (int)(tailTip.Y - body.Bottom + 2)));
        if (tipDest.Height < 4) tipDest.Height = 4;
        sb.Draw(_sheet, tipDest, tipSrcRect, Color.White * alpha);
    }

    /// <summary>Animated ellipsis typing bubble from the sheet.</summary>
    public static void DrawTyping(
        SpriteBatch sb,
        Vector2 anchor,
        float zoom,
        float anim,
        float alpha = 1f)
    {
        if (_sheet == null || TypingEllipsis.Length == 0) return;

        var frame = ((int)(anim * 4f)) % TypingEllipsis.Length;
        var src = TypingEllipsis[frame];
        var scale = 2.2f * zoom;
        var w = src.Width * scale;
        var h = src.Height * scale;
        var dest = new Rectangle(
            (int)(anchor.X - w * 0.5f),
            (int)(anchor.Y - h * 0.85f),
            (int)MathF.Ceiling(w),
            (int)MathF.Ceiling(h));
        sb.Draw(_sheet, dest, src, Color.White * alpha);

        // Small trail dots under the bubble toward the speaker.
        if (ThoughtDots.Length >= 2)
        {
            var d0 = ThoughtDots[0];
            var d1 = ThoughtDots[1];
            var s = 1.6f * zoom;
            sb.Draw(_sheet,
                new Rectangle((int)(anchor.X - 2f * zoom), (int)(anchor.Y + 2f * zoom), (int)(d1.Width * s), (int)(d1.Height * s)),
                d1, Color.White * alpha);
            sb.Draw(_sheet,
                new Rectangle((int)(anchor.X + 2f * zoom), (int)(anchor.Y + 10f * zoom), (int)(d0.Width * s), (int)(d0.Height * s)),
                d0, Color.White * (alpha * 0.9f));
        }
    }

    private static void DrawNineSlice(
        SpriteBatch sb, Rectangle dest, Rectangle src, int border, float alpha)
    {
        if (_sheet == null) return;

        var left = Math.Min(border, Math.Max(1, dest.Width / 2));
        var right = Math.Min(border, Math.Max(1, dest.Width - left));
        var top = Math.Min(border, Math.Max(1, dest.Height / 2));
        var bottom = Math.Min(border, Math.Max(1, dest.Height - top));
        var centerW = Math.Max(0, dest.Width - left - right);
        var centerH = Math.Max(0, dest.Height - top - bottom);
        var color = Color.White * alpha;

        var sl = Math.Min(border, src.Width / 2);
        var sr = Math.Min(border, src.Width - sl);
        var st = Math.Min(border, src.Height / 2);
        var sbord = Math.Min(border, src.Height - st);
        var scw = Math.Max(1, src.Width - sl - sr);
        var sch = Math.Max(1, src.Height - st - sbord);

        void Tile(Rectangle d, Rectangle s) => sb.Draw(_sheet, d, s, color);

        Tile(new Rectangle(dest.X, dest.Y, left, top),
            new Rectangle(src.X, src.Y, sl, st));
        if (centerW > 0)
            Tile(new Rectangle(dest.X + left, dest.Y, centerW, top),
                new Rectangle(src.X + sl, src.Y, scw, st));
        Tile(new Rectangle(dest.Right - right, dest.Y, right, top),
            new Rectangle(src.Right - sr, src.Y, sr, st));

        if (centerH > 0)
        {
            Tile(new Rectangle(dest.X, dest.Y + top, left, centerH),
                new Rectangle(src.X, src.Y + st, sl, sch));
            if (centerW > 0)
                Tile(new Rectangle(dest.X + left, dest.Y + top, centerW, centerH),
                    new Rectangle(src.X + sl, src.Y + st, scw, sch));
            Tile(new Rectangle(dest.Right - right, dest.Y + top, right, centerH),
                new Rectangle(src.Right - sr, src.Y + st, sr, sch));
        }

        Tile(new Rectangle(dest.X, dest.Bottom - bottom, left, bottom),
            new Rectangle(src.X, src.Bottom - sbord, sl, sbord));
        if (centerW > 0)
            Tile(new Rectangle(dest.X + left, dest.Bottom - bottom, centerW, bottom),
                new Rectangle(src.X + sl, src.Bottom - sbord, scw, sbord));
        Tile(new Rectangle(dest.Right - right, dest.Bottom - bottom, right, bottom),
            new Rectangle(src.Right - sr, src.Bottom - sbord, sr, sbord));
    }
}
