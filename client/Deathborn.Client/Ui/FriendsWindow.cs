using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Deathborn.Client;
using Deathborn.Client.Gameplay;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

public sealed class FriendsWindow : UiWindow
{
    private const int WinWidth = 440;
    private const int WinHeight = 420;
    private const int ListWidth = 168;
    private const int RowHeight = 26;
    private const int ChatInputHeight = 32;

    private PlayerFriends? _friends;
    private Func<long>? _localCharacterId;
    private Action<long, string>? _sendPm;
    private Action<long, bool>? _sendFriendRespond;
    private Action<long>? _sendFriendRemove;

    private int _listScroll;
    private int? _hoverRow;
    private readonly TextField _pmField = new() { Placeholder = "Whisper..." };
    private Rectangle _acceptBtn;
    private Rectangle _declineBtn;
    private Rectangle _removeBtn;
    private bool _awaitEnterRelease;

    public FriendsWindow() : base("Friends", WinWidth, WinHeight, Keys.G, new Point(520, 80)) { }

    public void Bind(
        PlayerFriends friends,
        Func<long> localCharacterId,
        Action<long, string> sendPm,
        Action<long, bool> sendFriendRespond,
        Action<long> sendFriendRemove)
    {
        _friends = friends;
        _localCharacterId = localCharacterId;
        _sendPm = sendPm;
        _sendFriendRespond = sendFriendRespond;
        _sendFriendRemove = sendFriendRemove;
    }

    public void OpenWhisper(long characterId, string name)
    {
        Open();
        _friends?.SelectWhisper(characterId, name);
        _pmField.Focused = true;
        _awaitEnterRelease = true;
    }

    public bool IsPmFocused => IsOpen && _pmField.Focused;

    public void TickInput(GameTime gameTime, KeyboardState kb, KeyboardState prevKb)
    {
        if (!IsOpen) return;
        UpdatePmInput(gameTime, kb, prevKb);
    }

    protected override void UpdateContent(MouseState mouse, MouseState prevMouse)
    {
        if (_friends == null) return;

        _hoverRow = null;
        var area = ContentAreaInternal();
        var listArea = new Rectangle(area.X, area.Y, ListWidth, area.Height);
        if (listArea.Contains(mouse.Position))
        {
            var maxScroll = Math.Max(0, _friends.Entries.Count * RowHeight - listArea.Height);
            var wheel = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
            if (wheel != 0)
                _listScroll = Math.Clamp(_listScroll - wheel / 120 * RowHeight, 0, maxScroll);
        }
        if (listArea.Contains(mouse.Position))
        {
            var localY = mouse.Y - listArea.Y + _listScroll;
            var index = localY / RowHeight;
            if (index >= 0 && index < _friends.Entries.Count)
                _hoverRow = index;
        }

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (_hoverRow is int row && row < _friends.Entries.Count)
                _friends.SelectFriend(_friends.Entries[row]);

            if (_acceptBtn.Contains(mouse.Position) && SelectedEntry() is { PendingIn: true } pendingIn)
                _sendFriendRespond?.Invoke(pendingIn.AccountId, true);

            if (_declineBtn.Contains(mouse.Position) && SelectedEntry() is { PendingIn: true } pendingDecline)
                _sendFriendRespond?.Invoke(pendingDecline.AccountId, false);

            if (_removeBtn.Contains(mouse.Position) && SelectedEntry() is { IsConfirmedFriend: true } friend)
                _sendFriendRemove?.Invoke(friend.AccountId);
        }

        UpdatePmInput(mouse, prevMouse);
    }

    private void UpdatePmInput(GameTime gameTime, KeyboardState kb, KeyboardState prevKb)
    {
        var chatArea = ChatArea();
        var inputRect = new Rectangle(chatArea.X, chatArea.Bottom - ChatInputHeight, chatArea.Width, ChatInputHeight);
        _pmField.Bounds = inputRect;

        if (!_pmField.Focused) return;

        _pmField.Update(gameTime, kb, prevKb);

        if (_awaitEnterRelease)
        {
            if (!InputKeys.IsEnterDown(kb))
                _awaitEnterRelease = false;
        }
        else if (InputKeys.EnterPressed(kb, prevKb))
            SubmitPm();
    }

    private void UpdatePmInput(MouseState mouse, MouseState prevMouse)
    {
        var chatArea = ChatArea();
        var inputRect = new Rectangle(chatArea.X, chatArea.Bottom - ChatInputHeight, chatArea.Width, ChatInputHeight);
        _pmField.Bounds = inputRect;

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (inputRect.Contains(mouse.Position))
            {
                _pmField.Focused = true;
                _awaitEnterRelease = true;
            }
            else if (!inputRect.Contains(mouse.Position))
                _pmField.Focused = false;
        }
    }

    private void SubmitPm()
    {
        var target = _friends?.SelectedWhisper;
        var text = _pmField.Text.Trim();
        if (target == null || target.CharacterId <= 0 || text.Length == 0) return;
        _sendPm?.Invoke(target.CharacterId, text);
        _pmField.Text = "";
    }

    protected override void DrawContent(SpriteBatch sb, SpriteFont font, Rectangle area)
    {
        if (_friends == null)
        {
            sb.DrawString(font, "Loading...", new Vector2(area.X + 8, area.Y + 8), GoldDim);
            return;
        }

        var listArea = new Rectangle(area.X, area.Y, ListWidth, area.Height);
        FarmRpgUi.DrawInsetPanel(sb, listArea);

        var chatArea = new Rectangle(area.X + ListWidth + 8, area.Y, area.Width - ListWidth - 8, area.Height);
        DrawChatPanel(sb, font, chatArea);

        var y = listArea.Y - _listScroll;
        for (var i = 0; i < _friends.Entries.Count; i++)
        {
            var entry = _friends.Entries[i];
            var row = new Rectangle(listArea.X + 2, y, listArea.Width - 4, RowHeight);
            if (row.Y < listArea.Y || row.Bottom > listArea.Bottom)
            {
                y += RowHeight;
                continue;
            }

            var selected = _friends.SelectedWhisper?.AccountId == entry.AccountId
                || (_friends.SelectedWhisper?.CharacterId > 0 && _friends.SelectedWhisper.CharacterId == entry.CharacterId);
            var hover = _hoverRow == i;
            if (selected || hover)
            {
                FarmRpgUi.DrawButton(sb, row, pressed: selected || hover);
            }

            var dotColor = entry.Online ? new Color(80, 200, 90) : new Color(90, 90, 95);
            DrawPrimitives.FillCircle(sb, new Vector2(row.X + 10, row.Y + RowHeight * 0.5f), 4f, dotColor);

            var label = entry.Name;
            if (entry.PendingIn) label += " (request)";
            else if (entry.PendingOut) label += " (pending)";
            sb.DrawString(font, label, new Vector2(row.X + 20, row.Y + 4),
                entry.Online ? FarmRpgUi.Ink : FarmRpgUi.InkMuted);

            y += RowHeight;
        }

        if (_friends.Entries.Count == 0)
            sb.DrawString(font, "No friends yet.", new Vector2(listArea.X + 8, listArea.Y + 8), GoldDim);
    }

    private void DrawChatPanel(SpriteBatch sb, SpriteFont font, Rectangle chatArea)
    {
        var target = _friends?.SelectedWhisper;
        var header = target != null ? $"Whisper: {target.Name}" : "Select a friend or whisper a player";
        sb.DrawString(font, header, new Vector2(chatArea.X, chatArea.Y), FarmRpgUi.Ink);

        var entry = SelectedEntry();
        var btnY = chatArea.Y + font.LineSpacing + 4;
        if (entry is { PendingIn: true })
        {
            _acceptBtn = new Rectangle(chatArea.X, btnY, 72, 24);
            _declineBtn = new Rectangle(chatArea.X + 78, btnY, 72, 24);
            DrawActionButton(sb, font, _acceptBtn, "Accept", _acceptBtn.Contains(Mouse.GetState().Position));
            DrawActionButton(sb, font, _declineBtn, "Decline", _declineBtn.Contains(Mouse.GetState().Position));
            btnY += 30;
        }
        else if (entry is { IsConfirmedFriend: true })
        {
            _removeBtn = new Rectangle(chatArea.X, btnY, 88, 24);
            DrawActionButton(sb, font, _removeBtn, "Remove", _removeBtn.Contains(Mouse.GetState().Position));
            btnY += 30;
        }

        var logTop = btnY;
        var logHeight = chatArea.Height - (logTop - chatArea.Y) - ChatInputHeight - 8;
        if (logHeight < 20) logHeight = 20;
        var logRect = new Rectangle(chatArea.X, logTop, chatArea.Width, logHeight);
        FarmRpgUi.DrawInsetPanel(sb, logRect);

        if (target is { CharacterId: > 0 })
        {
            var lines = _friends!.MessagesFor(target.CharacterId);
            var y = (float)logRect.Bottom - 4f;
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                var prefix = line.Outgoing ? "You: " : $"{line.FromName}: ";
                var text = prefix + line.Text;
                var size = font.MeasureString(text);
                y -= size.Y;
                if (y < logRect.Y + 2) break;
                sb.DrawString(font, text, new Vector2(logRect.X + 6, y),
                    line.Outgoing ? new Color(92, 80, 145) : FarmRpgUi.Ink);
            }
        }
        else if (target != null)
        {
            sb.DrawString(font, "Player is offline.", new Vector2(logRect.X + 8, logRect.Y + 8), GoldDim);
        }

        var inputRect = new Rectangle(chatArea.X, chatArea.Bottom - ChatInputHeight, chatArea.Width, ChatInputHeight);
        _pmField.Bounds = inputRect;
        _pmField.Draw(sb, font);
    }

    private FriendEntryState? SelectedEntry()
    {
        if (_friends?.SelectedWhisper is not { } sel) return null;
        if (sel.AccountId > 0)
            return _friends.FindByAccountId(sel.AccountId);
        if (sel.CharacterId > 0)
            return _friends.FindByCharacterId(sel.CharacterId);
        return null;
    }

    private Rectangle ContentAreaInternal()
    {
        return ContentBounds;
    }

    private Rectangle ChatArea()
    {
        var area = ContentAreaInternal();
        return new Rectangle(area.X + ListWidth + 8, area.Y, area.Width - ListWidth - 8, area.Height);
    }

    private static void DrawActionButton(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover)
    {
        DrawThemedButton(sb, font, rect, label, hover, danger: label is "Decline" or "Remove");
    }
}
