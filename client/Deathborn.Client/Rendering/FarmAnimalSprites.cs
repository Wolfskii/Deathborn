using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

/// <summary>Homestead animals — idle/walk strips, right-facing; flip for left.</summary>
public static class FarmAnimalSprites
{
    public sealed class Sheet
    {
        public required Texture2D Idle { get; init; }
        public required Texture2D Walk { get; init; }
        public int Cell { get; init; }
        public int WalkFrames { get; init; }
        public Vector2 FootAnchor => new(Cell * 0.5f, Cell - 1f);
    }

    private static readonly Dictionary<string, Sheet> Sheets = new(StringComparer.Ordinal);

    public static void Load(ContentManager content)
    {
        Sheets.Clear();
        TryLoad(content, "chicken", 16);
        TryLoad(content, "cow", 32);
        TryLoad(content, "sheep", 32);
        TryLoad(content, "pig", 32);
    }

    private static void TryLoad(ContentManager content, string id, int expectedCell)
    {
        try
        {
            var idle = content.Load<Texture2D>($"Characters/Animals/farm/{id}/idle");
            var walk = content.Load<Texture2D>($"Characters/Animals/farm/{id}/walk");
            var cell = idle.Height;
            if (cell <= 0) cell = expectedCell;
            Sheets[id] = new Sheet
            {
                Idle = idle,
                Walk = walk,
                Cell = cell,
                WalkFrames = Math.Max(1, walk.Width / cell),
            };
        }
        catch (ContentLoadException)
        {
        }
    }

    public static Sheet? Get(string? type) =>
        type != null && Sheets.TryGetValue(type, out var s) ? s : null;

    public static void Draw(
        SpriteBatch sb,
        string type,
        Vector2 screenPos,
        Color tint,
        float scale,
        Vector2 facing,
        bool moving,
        float animTime)
    {
        var sheet = Get(type);
        if (sheet == null) return;

        var walk = moving && sheet.WalkFrames > 1;
        var tex = walk ? sheet.Walk : sheet.Idle;
        var frames = walk ? sheet.WalkFrames : 1;
        var frame = walk ? (int)(animTime / 0.12f) % frames : 0;
        var src = new Rectangle(frame * sheet.Cell, 0, sheet.Cell, sheet.Cell);
        var flip = facing.X < -0.12f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        sb.Draw(tex, screenPos, src, tint, 0f, sheet.FootAnchor, scale, flip, 0f);
    }
}
