using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class SpellBookWindow : UiWindow
{
    private const int Width = 340;
    private const int Height = 400;
    private const int Cols = 4;
    private const int Rows = 3;
    private const int CellSize = 64;
    private const int CellGap = 8;

    private DragDropManager? _dragDrop;
    private int _pageIndex;
    private string? _hoverAbilityId;
    private string? _pendingDragAbilityId;
    private Point _dragStartMouse;
    private readonly List<Rectangle> _cellRects = new(Cols * Rows);
    private Rectangle _prevButton;
    private Rectangle _nextButton;

    public event Action<string>? AbilityClicked;

    public SpellBookWindow() : base("Spell Book", Width, Height, Keys.K, new Point(420, 80)) { }

    public void Bind(DragDropManager dragDrop) => _dragDrop = dragDrop;

    protected override void UpdateContent(MouseState mouse, MouseState prevMouse)
    {
        if (_dragDrop == null) return;

        LayoutCells();

        if (_dragDrop.IsDragging) return;

        _hoverAbilityId = null;
        var abilities = CurrentPageAbilities();
        for (var i = 0; i < _cellRects.Count && i < abilities.Count; i++)
        {
            if (!_cellRects[i].Contains(mouse.Position)) continue;
            _hoverAbilityId = abilities[i].Id;
            break;
        }

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (_prevButton.Contains(mouse.Position) && _pageIndex > 0)
                _pageIndex--;
            else if (_nextButton.Contains(mouse.Position) && _pageIndex < AbilityCatalog.PageOrder.Length - 1)
                _pageIndex++;

            if (_hoverAbilityId != null)
            {
                _pendingDragAbilityId = _hoverAbilityId;
                _dragStartMouse = mouse.Position;
            }
        }

        if (_pendingDragAbilityId != null && mouse.LeftButton == ButtonState.Pressed)
        {
            var dx = mouse.X - _dragStartMouse.X;
            var dy = mouse.Y - _dragStartMouse.Y;
            if (dx * dx + dy * dy > 36)
            {
                _dragDrop.BeginAbility(_pendingDragAbilityId);
                _pendingDragAbilityId = null;
            }
        }

        if (mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed)
        {
            if (_pendingDragAbilityId != null)
            {
                var dx = mouse.X - _dragStartMouse.X;
                var dy = mouse.Y - _dragStartMouse.Y;
                if (dx * dx + dy * dy <= 36)
                    AbilityClicked?.Invoke(_pendingDragAbilityId);
            }
            _pendingDragAbilityId = null;
        }
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        LayoutCells();
        var pageName = AbilityCatalog.PageOrder[_pageIndex];
        var abilities = CurrentPageAbilities();

        sb.DrawString(font, pageName, new Vector2(area.X + 8, area.Y + 4), PanelBorder);

        for (var i = 0; i < _cellRects.Count; i++)
        {
            var rect = _cellRects[i];
            var hover = _hoverAbilityId != null && i < abilities.Count && abilities[i].Id == _hoverAbilityId;
            DrawCell(sb, rect, hover);

            if (i >= abilities.Count) continue;
            var ability = abilities[i];
            var icon = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            HotbarIconDraw.Draw(sb, ability.Id, icon);
            DrawAbilityLabels(sb, font, rect, ability);
        }

        DrawPageButton(sb, font, _prevButton, "< Prev", _pageIndex > 0);
        DrawPageButton(sb, font, _nextButton, "Next >", _pageIndex < AbilityCatalog.PageOrder.Length - 1);

        var footer = $"Page {_pageIndex + 1} / {AbilityCatalog.PageOrder.Length}  -  drag to hotbar";
        SpriteFontSafe.DrawOutlined(sb, font, footer, new Vector2(area.X + 8, Bounds.Bottom - 10), Color.White, Color.Black);

        if (_hoverAbilityId != null)
        {
            var info = AbilityCatalog.Get(_hoverAbilityId);
            if (info != null)
            {
                for (var i = 0; i < abilities.Count; i++)
                {
                    if (abilities[i].Id != _hoverAbilityId) continue;
                    AbilityTooltipDraw.DrawAbility(sb, font, info, _cellRects[i],
                        new Point(GameViewport.Width, GameViewport.Height));
                    break;
                }
            }
        }
    }

    private List<AbilityInfo> CurrentPageAbilities() =>
        AbilityCatalog.ForPage(AbilityCatalog.PageOrder[_pageIndex]).ToList();

    private void LayoutCells()
    {
        _cellRects.Clear();
        var content = new Rectangle(Bounds.X + 10, Bounds.Y + 56, Bounds.Width - 20, Bounds.Height - 100);
        var gridW = Cols * CellSize + (Cols - 1) * CellGap;
        var x0 = content.X + (content.Width - gridW) / 2;
        var y0 = content.Y + 8;

        for (var row = 0; row < Rows; row++)
        for (var col = 0; col < Cols; col++)
        {
            var x = x0 + col * (CellSize + CellGap);
            var y = y0 + row * (CellSize + CellGap);
            _cellRects.Add(new Rectangle(x, y, CellSize, CellSize));
        }

        var navY = Bounds.Bottom - 52;
        _prevButton = new Rectangle(Bounds.X + 16, navY, 90, 26);
        _nextButton = new Rectangle(Bounds.Right - 106, navY, 90, 26);
    }

    private static void DrawCell(SpriteBatch sb, Rectangle rect, bool hover)
    {
        DrawPrimitives.FillRect(sb, rect, hover ? new Color(42, 38, 32) : new Color(22, 20, 18));
        DrawBorder(sb, rect, hover ? PanelBorder : GoldDim);
    }

    private static void DrawAbilityLabels(SpriteBatch sb, SpriteFont font, Rectangle rect, AbilityInfo ability)
    {
        const float costScale = 0.45f;
        const float nameScale = 0.55f;
        var cost = SpriteFontSafe.Filter(AbilityResourceCosts.FormatCostLine(ability));
        var name = SpriteFontSafe.Filter(ability.Name);
        var costH = font.MeasureString(cost).Y * costScale;
        var nameH = font.MeasureString(name).Y * nameScale;
        var barH = (int)(costH + nameH + 6);
        var bar = new Rectangle(rect.X + 1, rect.Bottom - barH - 1, rect.Width - 2, barH);
        DrawPrimitives.FillRect(sb, bar, new Color(0, 0, 0, 0.62f));

        var textX = rect.X + 4f;
        var costY = bar.Y + 2f;
        sb.DrawString(font, cost, new Vector2(textX, costY), new Color(160, 175, 195),
            0f, Vector2.Zero, costScale, SpriteEffects.None, 0f);
        sb.DrawString(font, name, new Vector2(textX, costY + costH), Color.White,
            0f, Vector2.Zero, nameScale, SpriteEffects.None, 0f);
    }

    private static void DrawPageButton(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool enabled)
    {
        var fill = enabled ? new Color(48, 40, 30) : new Color(30, 28, 26);
        DrawPrimitives.FillRect(sb, rect, fill);
        DrawBorder(sb, rect, enabled ? GoldDim : new Color(70, 65, 58));
        var size = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f),
            enabled ? Color.White : new Color(100, 100, 105));
    }

    private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color)
    {
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
