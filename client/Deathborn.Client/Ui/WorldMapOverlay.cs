using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Full-world map overlay (M). Gameplay continues underneath.</summary>
public sealed class WorldMapOverlay
{
    private static readonly Color Dim = new(0, 0, 0, 0.62f);
    private static readonly Color PanelFill = new(22, 20, 18);
    private static readonly Color PanelBorder = new(210, 170, 80);
    private static readonly Color GoldDim = new(130, 105, 55);

    private const int Margin = 52;
    private const int TitleSpace = 40;

    private Rectangle _mapBounds;

    public bool IsOpen { get; private set; }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open() => IsOpen = true;

    public void Close() => IsOpen = false;

    public void Draw(
        SpriteBatch sb,
        SpriteFont font,
        Vector2 playerWorldPos,
        IEnumerable<WorldNpcEntity>? npcs = null,
        HousePlotZone? homestead = null)
    {
        if (!IsOpen) return;

        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, GameViewport.Width, GameViewport.Height), Dim);

        _mapBounds = ComputeMapBounds();

        var panel = new Rectangle(_mapBounds.X - 8, _mapBounds.Y - 8, _mapBounds.Width + 16, _mapBounds.Height + 16);
        DrawPrimitives.FillRect(sb, panel, PanelFill);
        DrawBorder(sb, panel, PanelBorder, 2);
        DrawBorder(sb, new Rectangle(panel.X + 5, panel.Y + 5, panel.Width - 10, panel.Height - 10), GoldDim, 1);

        WorldMap.SwaroviaMainland.DrawOverlay(sb, _mapBounds, playerWorldPos);
        DrawTownMarkers(sb, font);
        DrawHouseMarkers(sb, font, homestead);
        DrawNpcMarkers(sb, npcs);

        var title = "World Map";
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title,
            new Vector2(GameViewport.Width / 2f - titleSize.X / 2f, Margin),
            PanelBorder);

        var hint = "M or Esc to close";
        var hintSize = font.MeasureString(hint);
        sb.DrawString(font, hint,
            new Vector2(GameViewport.Width / 2f - hintSize.X / 2f, panel.Bottom + 10),
            new Color(190, 190, 200));
    }

    private void DrawHouseMarkers(SpriteBatch sb, SpriteFont font, HousePlotZone? homestead)
    {
        if (homestead == null) return;

        var map = WorldMap.SwaroviaMainland;
        var pos = WorldToMap(homestead.Center, _mapBounds, map);
        HomesteadMapIcon.Draw(sb, pos, 0.85f);

        var label = SpriteFontSafe.Filter("Your Homestead");
        var size = SpriteFontSafe.MeasureString(font, label) * 0.55f;
        var labelPos = new Vector2(pos.X - size.X / 2f, pos.Y + 8f);
        SpriteFontSafe.DrawString(sb, font, label, labelPos, new Color(180, 210, 235),
            0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
    }

    private void DrawNpcMarkers(SpriteBatch sb, IEnumerable<WorldNpcEntity>? npcs)
    {
        if (npcs == null) return;
        var map = WorldMap.SwaroviaMainland;
        foreach (var npc in npcs)
        {
            if (!npc.IsBoss && !npc.IsAttackable) continue;
            var pos = WorldToMap(npc.Position, _mapBounds, map);
            if (npc.IsBoss)
                MonsterMapIcon.Draw(sb, pos, 0.85f);
            else
            {
                DrawPrimitives.FillCircle(sb, pos, 4f, new Color(0.92f, 0.24f, 0.22f, 0.95f));
                DrawPrimitives.DrawCircleOutline(sb, pos, 4f, new Color(0.45f, 0.08f, 0.08f, 0.95f), 12, 1.5f);
            }
        }
    }

    private void DrawTownMarkers(SpriteBatch sb, SpriteFont font)
    {
        if (WorldZones.Towns.Count == 0)
            WorldZones.Initialize(WorldMap.SwaroviaMainland);

        var map = WorldMap.SwaroviaMainland;
        foreach (var town in WorldZones.Towns)
        {
            var pos = WorldToMap(town.Center, _mapBounds, map);
            DrawPrimitives.FillCircle(sb, pos, 5f, new Color(0.35f, 0.75f, 0.45f, 0.9f));
            DrawPrimitives.DrawCircleOutline(sb, pos, 5f, new Color(0.18f, 0.42f, 0.28f, 0.95f), 14, 1.5f);

            var label = SpriteFontSafe.Filter(town.Name);
            var size = SpriteFontSafe.MeasureString(font, label) * 0.55f;
            var labelPos = new Vector2(pos.X - size.X / 2f, pos.Y + 7f);
            SpriteFontSafe.DrawString(sb, font, label, labelPos, new Color(210, 225, 200),
                0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
        }
    }

    private static Vector2 WorldToMap(Vector2 world, Rectangle bounds, WorldMap map) =>
        new(
            bounds.X + world.X / map.WorldWidth * bounds.Width,
            bounds.Y + world.Y / map.WorldHeight * bounds.Height);

    private static Rectangle ComputeMapBounds()
    {
        var map = WorldMap.SwaroviaMainland;
        var availW = GameViewport.Width - Margin * 2;
        var availH = GameViewport.Height - Margin * 2 - TitleSpace - 28;
        var scale = Math.Min(availW / map.WorldWidth, availH / map.WorldHeight);
        var w = Math.Max(1, (int)(map.WorldWidth * scale));
        var h = Math.Max(1, (int)(map.WorldHeight * scale));
        var x = (GameViewport.Width - w) / 2;
        var y = Margin + TitleSpace + (availH - h) / 2;
        return new Rectangle(x, y, w, h);
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
