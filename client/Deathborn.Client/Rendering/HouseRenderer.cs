using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Gameplay;

namespace Deathborn.Client.Rendering;

/// <summary>Draws player homestead plots, gardens, houses, and interior furniture.</summary>
public static class HouseRenderer
{
    private static readonly Color Fence = new(0.42f, 0.32f, 0.22f);
    private static readonly Color GardenSoil = new(0.38f, 0.28f, 0.16f);
    private static readonly Color GardenCrop = new(0.28f, 0.52f, 0.24f);
    private static readonly Color Plaster = new(0.88f, 0.82f, 0.72f);
    private static readonly Color PlasterDark = new(0.72f, 0.66f, 0.58f);
    private static readonly Color Roof = new(0.55f, 0.28f, 0.22f);
    private static readonly Color RoofLight = new(0.68f, 0.38f, 0.28f);
    private static readonly Color Door = new(0.35f, 0.22f, 0.14f);

    public static void Draw(
        SpriteBatch sb,
        Vector2 camera,
        Vector2 screenCenter,
        float zoom,
        IEnumerable<HousePlotZone> houses)
    {
        foreach (var house in houses)
        {
            DrawPlot(sb, house, camera, screenCenter, zoom);
            DrawHouseStructure(sb, house.Center, camera, screenCenter, zoom);
            foreach (var item in house.Furniture)
                DrawFurniture(sb, item, camera, screenCenter, zoom);
        }
    }

    private static void DrawPlot(
        SpriteBatch sb, HousePlotZone house,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var c = house.Center;
        var hw = HousingConstants.PlotHalfW;
        var hh = HousingConstants.PlotHalfH;
        var tl = WorldToScreen(c + new Vector2(-hw, -hh), camera, screenCenter, zoom);
        var br = WorldToScreen(c + new Vector2(hw, hh), camera, screenCenter, zoom);
        var plot = new Rectangle((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));

        DrawPrimitives.FillRect(sb, plot, new Color(0.22f, 0.38f, 0.2f, 0.35f));
        DrawBorder(sb, plot, Fence, Math.Max(1, (int)(2 * zoom)));

        foreach (var offset in HousingConstants.GardenCropOffsets)
        {
            var world = c + offset;
            var screen = WorldToScreen(world, camera, screenCenter, zoom);
            var w = 28f * zoom;
            var h = 16f * zoom;
            var soil = new Rectangle((int)(screen.X - w / 2), (int)(screen.Y - h / 2), (int)w, (int)h);
            DrawPrimitives.FillRect(sb, soil, GardenSoil);
            DrawPrimitives.FillRect(sb,
                new Rectangle(soil.X + 2, soil.Y + 2, soil.Width - 4, (int)(h * 0.45f)),
                GardenCrop);
        }
    }

    private static void DrawHouseStructure(
        SpriteBatch sb, Vector2 world,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var w = HousingConstants.HouseHalfW * 2f;
        var h = HousingConstants.HouseHalfH * 2f;
        var screen = WorldToScreen(world, camera, screenCenter, zoom);
        var body = new Rectangle(
            (int)(screen.X - w * 0.5f * zoom),
            (int)(screen.Y - h * 0.35f * zoom),
            (int)(w * zoom),
            (int)(h * 0.65f * zoom));
        DrawPrimitives.FillRect(sb, body, Plaster);
        DrawPrimitives.FillRect(sb, new Rectangle(body.X, body.Y, body.Width, 2), PlasterDark);

        var doorW = (int)(14 * zoom);
        var doorH = (int)(22 * zoom);
        DrawPrimitives.FillRect(sb,
            new Rectangle(body.Center.X - doorW / 2, body.Bottom - doorH, doorW, doorH),
            Door);

        var roofH = h * 0.4f * zoom;
        var roofBase = new Vector2(body.Center.X, body.Y);
        DrawPrimitives.FillTriangle(sb,
            roofBase + new Vector2(-body.Width * 0.55f, 0),
            roofBase + new Vector2(body.Width * 0.55f, 0),
            roofBase + new Vector2(0, -roofH),
            Roof);
        DrawPrimitives.DrawLine(sb,
            roofBase + new Vector2(-body.Width * 0.55f, 0),
            roofBase + new Vector2(0, -roofH),
            RoofLight, 2f);
    }

    private static void DrawFurniture(
        SpriteBatch sb, FurnitureItemState item,
        Vector2 camera, Vector2 screenCenter, float zoom)
    {
        var screen = WorldToScreen(item.Position, camera, screenCenter, zoom);
        var z = zoom;
        switch (item.Type)
        {
            case "bed":
                DrawPrimitives.FillRect(sb, CenteredRect(screen, 36 * z, 18 * z), new Color(0.55f, 0.42f, 0.62f));
                break;
            case "table":
                DrawPrimitives.FillRect(sb, CenteredRect(screen, 28 * z, 14 * z), new Color(0.48f, 0.32f, 0.18f));
                break;
            case "chair":
                DrawPrimitives.FillRect(sb, CenteredRect(screen, 14 * z, 14 * z), new Color(0.42f, 0.28f, 0.16f));
                break;
            case "chest":
                DrawPrimitives.FillRect(sb, CenteredRect(screen, 18 * z, 12 * z), new Color(0.52f, 0.34f, 0.18f));
                break;
            case "fireplace":
                DrawPrimitives.FillRect(sb, CenteredRect(screen, 16 * z, 20 * z), new Color(0.35f, 0.32f, 0.3f));
                DrawPrimitives.FillRect(sb, CenteredRect(screen + new Vector2(0, -4 * z), 10 * z, 8 * z),
                    new Color(0.92f, 0.42f, 0.12f, 0.85f));
                break;
            case "rug":
                DrawPrimitives.FillRect(sb, CenteredRect(screen, 40 * z, 24 * z), new Color(0.62f, 0.22f, 0.22f, 0.75f));
                break;
        }
    }

    private static Rectangle CenteredRect(Vector2 center, float w, float h) =>
        new((int)(center.X - w / 2), (int)(center.Y - h / 2), (int)w, (int)h);

    private static Vector2 WorldToScreen(Vector2 world, Vector2 camera, Vector2 screenCenter, float zoom) =>
        screenCenter + (world - camera) * zoom;

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
