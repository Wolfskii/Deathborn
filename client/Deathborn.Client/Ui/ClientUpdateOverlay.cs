using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Deathborn.Client.Net;
using Deathborn.Client.Rendering;

namespace Deathborn.Client.Ui;

/// <summary>Login-screen modal for optional client auto-updates from the game server.</summary>
public sealed class ClientUpdateOverlay
{
    private enum Phase
    {
        Hidden,
        Prompt,
        Downloading,
        Ready,
        Error,
    }

    private static readonly Color Dim = new(0, 0, 0, 170);
    private static readonly Color PanelFill = new(18, 16, 24, 245);
    private static readonly Color Gold = new(210, 170, 80);
    private static readonly Color GoldDim = new(130, 105, 55);
    private static readonly Color Text = new(235, 225, 205);
    private static readonly Color Muted = new(170, 160, 145);
    private static readonly Color ErrorCol = new(255, 130, 110);

    private readonly ClientUpdateService _updates = new();
    private Phase _phase = Phase.Hidden;
    private ClientUpdateOffer? _offer;
    private string _message = "";
    private string _downloadPath = "";
    private double _progress;
    private CancellationTokenSource? _workCts;

    private Rectangle _panel;
    private Rectangle _primaryBtn;
    private Rectangle _secondaryBtn;
    private Rectangle _progressTrack;
    private Rectangle _progressFill;
    private bool _primaryHover;
    private bool _secondaryHover;

    public bool BlocksInput => _phase is Phase.Prompt or Phase.Downloading or Phase.Ready or Phase.Error;

    public async Task CheckOnLoginAsync()
    {
        if (_phase != Phase.Hidden)
            return;

        try
        {
            var offer = await _updates.CheckForUpdateAsync();
            if (offer is null)
                return;

            _offer = offer;
            _message =
                $"A newer Deathborn awaits ({offer.Latest}).\n" +
                $"You are on {GameVersion.Current}.\n\n" +
                (string.IsNullOrWhiteSpace(offer.Notes) ? "The Reaper recommends patching." : offer.Notes);
            _phase = Phase.Prompt;
        }
        catch
        {
            // Updates are optional — never block login on a failed check.
        }
    }

    public void Update(Point mouse, bool clicked)
    {
        if (_phase == Phase.Hidden)
            return;

        Layout();
        _primaryHover = _primaryBtn.Contains(mouse);
        _secondaryHover = _secondaryBtn.Contains(mouse);

        if (!clicked || _phase == Phase.Downloading)
            return;

        if (_primaryHover)
        {
            if (_phase == Phase.Prompt)
                _ = StartDownloadAsync();
            else if (_phase == Phase.Ready)
                ApplyUpdateAndExit();
            else if (_phase == Phase.Error)
                _ = StartDownloadAsync();
            return;
        }

        if (_secondaryHover)
        {
            if (_phase is Phase.Prompt or Phase.Error)
                Dismiss();
            else if (_phase == Phase.Ready)
                Dismiss();
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font)
    {
        if (_phase == Phase.Hidden)
            return;

        Layout();
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        DrawPrimitives.FillRect(sb, new Rectangle(0, 0, vw, vh), Dim);

        DrawPrimitives.FillRect(sb, _panel, PanelFill);
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Y, _panel.Width, 2), Gold);
        DrawPrimitives.FillRect(sb, new Rectangle(_panel.X, _panel.Bottom - 2, _panel.Width, 2), GoldDim);

        var title = _phase switch
        {
            Phase.Prompt => "Update available",
            Phase.Downloading => "Looting the patch...",
            Phase.Ready => "Update ready",
            Phase.Error => "Update failed",
            _ => "Update",
        };
        var titleSize = font.MeasureString(title);
        sb.DrawString(font, title, new Vector2(_panel.Center.X - titleSize.X / 2f, _panel.Y + 18), Gold);

        DrawWrapped(sb, font, _message, new Rectangle(_panel.X + 24, _panel.Y + 48, _panel.Width - 48, 120),
            _phase == Phase.Error ? ErrorCol : Text);

        if (_phase == Phase.Downloading || _phase == Phase.Ready)
        {
            DrawPrimitives.FillRect(sb, _progressTrack, new Color(32, 28, 24));
            DrawPrimitives.FillRect(sb, _progressFill, new Color(120, 170, 90));
            var pct = (int)Math.Round(_progress * 100);
            var pctText = $"{pct}%";
            var pctSize = font.MeasureString(pctText) * 0.85f;
            sb.DrawString(font, pctText,
                new Vector2(_progressTrack.Center.X - pctSize.X / 2f, _progressTrack.Bottom + 6),
                Muted, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        }

        DrawButton(sb, font, _primaryBtn, PrimaryLabel(), _primaryHover, enabled: _phase != Phase.Downloading);
        if (_phase != Phase.Downloading)
            DrawButton(sb, font, _secondaryBtn, SecondaryLabel(), _secondaryHover, enabled: true);
    }

    private string PrimaryLabel() => _phase switch
    {
        Phase.Prompt => "Update now",
        Phase.Ready => _offer?.Artifact.Kind == "installer" ? "Install & close" : "Open download",
        Phase.Error => "Retry",
        _ => "OK",
    };

    private string SecondaryLabel() => _phase switch
    {
        Phase.Prompt => "Not today",
        Phase.Ready => "Later",
        Phase.Error => "Cancel",
        _ => "Cancel",
    };

    private async Task StartDownloadAsync()
    {
        if (_offer is null)
            return;

        CancelWork();
        _workCts = new CancellationTokenSource();
        var ct = _workCts.Token;
        _phase = Phase.Downloading;
        _progress = 0;
        _message = "Sharpening files from the server...";

        try
        {
            var path = await _updates.DownloadAsync(
                _offer,
                new Progress<double>(p => _progress = p),
                ct);
            _downloadPath = path;
            _progress = 1;
            _phase = Phase.Ready;
            _message = _offer.Artifact.Kind switch
            {
                "installer" => "Installer downloaded.\nThe game will close so the wizard can finish the job.",
                "dmg" => "Disk image saved.\nWe will open it — drag Deathborn.app to Applications.",
                _ => "Archive downloaded.\nWe will open the folder. Run install.sh if you used the Linux package.",
            };
        }
        catch (OperationCanceledException)
        {
            Dismiss();
        }
        catch (Exception ex)
        {
            _phase = Phase.Error;
            _message = $"Could not download the update.\n{ex.Message}";
        }
    }

    private void ApplyUpdateAndExit()
    {
        if (string.IsNullOrWhiteSpace(_downloadPath) || _offer is null)
            return;

        try
        {
            ClientUpdateService.LaunchInstaller(_downloadPath, _offer.Artifact.Kind);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _phase = Phase.Error;
            _message = $"Could not launch the update.\n{ex.Message}";
        }
    }

    private void Dismiss()
    {
        CancelWork();
        _phase = Phase.Hidden;
        _offer = null;
        _downloadPath = "";
        _progress = 0;
        _message = "";
    }

    private void CancelWork()
    {
        _workCts?.Cancel();
        _workCts?.Dispose();
        _workCts = null;
    }

    private void Layout()
    {
        var vw = GameViewport.Width;
        var vh = GameViewport.Height;
        _panel = new Rectangle(vw / 2 - 230, vh / 2 - 150, 460, 300);
        _progressTrack = new Rectangle(_panel.X + 32, _panel.Y + 178, _panel.Width - 64, 16);
        _progressFill = new Rectangle(_progressTrack.X, _progressTrack.Y,
            Math.Max(0, (int)(_progressTrack.Width * _progress)), _progressTrack.Height);
        _primaryBtn = new Rectangle(_panel.X + 32, _panel.Bottom - 52, 190, 36);
        _secondaryBtn = new Rectangle(_panel.Right - 222, _panel.Bottom - 52, 190, 36);
    }

    private static void DrawWrapped(SpriteBatch sb, SpriteFont font, string text, Rectangle area, Color color)
    {
        var y = (float)area.Y;
        foreach (var line in text.Replace("\r", "").Split('\n'))
        {
            sb.DrawString(font, SpriteFontSafe.Filter(line), new Vector2(area.X, y), color);
            y += font.LineSpacing;
        }
    }

    private static void DrawButton(SpriteBatch sb, SpriteFont font, Rectangle rect, string label, bool hover, bool enabled)
    {
        var fill = !enabled
            ? new Color(40, 36, 32)
            : hover ? new Color(72, 58, 42) : new Color(48, 40, 30);
        DrawPrimitives.FillRect(sb, rect, fill);
        DrawPrimitives.FillRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, 2), enabled ? Gold : GoldDim);
        var size = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f),
            enabled ? Text : Muted);
    }
}
