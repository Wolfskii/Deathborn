using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Rendering;

public static class DrawPrimitives
{
  private static Texture2D? _pixel;

  public static void Init(GraphicsDevice device)
  {
    _pixel ??= new Texture2D(device, 1, 1);
    _pixel.SetData([Color.White]);
  }

  public static void FillRect(SpriteBatch sb, Rectangle rect, Color color)
  {
    sb.Draw(_pixel!, rect, color);
  }

  public static void MaskOutsideCircle(SpriteBatch sb, Vector2 center, float radius, Color color, int segments = 48)
  {
    for (var i = 0; i < segments; i++)
    {
      var a0 = i / (float)segments * MathHelper.TwoPi;
      var a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
      var p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
      var p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;

      var mid = (a0 + a1) * 0.5f;
      var far = center + new Vector2(MathF.Cos(mid), MathF.Sin(mid)) * (radius + 80f);
      FillTriangle(sb, p0, p1, far, color);
    }
  }

  public static void FillCircle(SpriteBatch sb, Vector2 center, float radius, Color color, int segments = 32)
  {
    for (var i = 0; i < segments; i++)
    {
      var a0 = i / (float)segments * MathHelper.TwoPi;
      var a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
      var p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
      var p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
      FillTriangle(sb, center, p0, p1, color);
    }
  }

  public static void DrawCircleOutline(SpriteBatch sb, Vector2 center, float radius, Color color, int segments = 32, float thickness = 2f)
  {
    for (var i = 0; i < segments; i++)
    {
      var a0 = i / (float)segments * MathHelper.TwoPi;
      var a1 = (i + 1) / (float)segments * MathHelper.TwoPi;
      var p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
      var p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
      DrawLine(sb, p0, p1, color, thickness);
    }
  }

  public static void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thickness = 2f)
  {
    var edge = b - a;
    var len = edge.Length();
    if (len < 0.001f) return;
    var angle = MathF.Atan2(edge.Y, edge.X);
    sb.Draw(_pixel!, a, null, color, angle, Vector2.Zero, new Vector2(len, thickness), SpriteEffects.None, 0);
  }

  private static void FillTriangle(SpriteBatch sb, Vector2 a, Vector2 b, Vector2 c, Color color)
  {
    var minX = (int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X)));
    var maxX = (int)MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X)));
    var minY = (int)MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y)));
    var maxY = (int)MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y)));
    for (var y = minY; y <= maxY; y++)
    for (var x = minX; x <= maxX; x++)
    {
      var p = new Vector2(x + 0.5f, y + 0.5f);
      if (PointInTriangle(p, a, b, c))
        sb.Draw(_pixel!, new Vector2(x, y), color);
    }
  }

  private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
  {
    var d1 = Sign(p, a, b);
    var d2 = Sign(p, b, c);
    var d3 = Sign(p, c, a);
    var hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
    var hasPos = d1 > 0 || d2 > 0 || d3 > 0;
    return !(hasNeg && hasPos);
  }

  private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
    (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
}
