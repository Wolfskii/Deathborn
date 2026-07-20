using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Deathborn.Client.Diagnostics;

/// <summary>
/// File logger for hitch diagnosis — enabled only when launched via <c>task dev:all</c>
/// (<see cref="Config.DevInstance"/> &gt; 0). Writes when a frame takes ≥ 1/30s.
/// </summary>
public static class DevPerfLog
{
    private const double DipThresholdMs = 1000.0 / 30.0;
    private const int RecentFrames = 16;

    private static readonly Stopwatch FrameWatch = new();
    private static readonly Stopwatch PhaseWatch = new();
    private static readonly double[] RecentMs = new double[RecentFrames];
    private static readonly Dictionary<string, string> Notes = new(StringComparer.Ordinal);
    private static readonly object Gate = new();

    private static StreamWriter? _writer;
    private static string? _logPath;
    private static bool _announced;
    private static int _recentIdx;
    private static int _recentFilled;
    private static double _updateMs;
    private static double _drawMs;
    private static int _gc0;
    private static int _gc1;
    private static int _gc2;
    private static long _lastDipTick;
    private static int _suppressedSinceLastWrite;

    public static bool Enabled => Config.DevInstance > 0;

    public static string? LogPath => _logPath;

    public static void BeginFrame()
    {
        if (!Enabled) return;

        EnsureWriter();
        Notes.Clear();
        _updateMs = 0;
        _drawMs = 0;
        FrameWatch.Restart();
        PhaseWatch.Restart();
        _gc0 = GC.CollectionCount(0);
        _gc1 = GC.CollectionCount(1);
        _gc2 = GC.CollectionCount(2);
    }

    public static void EndUpdate()
    {
        if (!Enabled) return;
        _updateMs = PhaseWatch.Elapsed.TotalMilliseconds;
        PhaseWatch.Restart();
    }

    public static void EndDraw()
    {
        if (!Enabled) return;

        _drawMs = PhaseWatch.Elapsed.TotalMilliseconds;
        FrameWatch.Stop();
        var frameMs = FrameWatch.Elapsed.TotalMilliseconds;

        RecentMs[_recentIdx] = frameMs;
        _recentIdx = (_recentIdx + 1) % RecentFrames;
        if (_recentFilled < RecentFrames)
            _recentFilled++;

        if (frameMs < DipThresholdMs)
            return;

        var now = Environment.TickCount64;
        // Avoid flooding the log (and disk) during a sustained hitch storm.
        if (now - _lastDipTick < 80)
        {
            _suppressedSinceLastWrite++;
            return;
        }

        var dGc0 = GC.CollectionCount(0) - _gc0;
        var dGc1 = GC.CollectionCount(1) - _gc1;
        var dGc2 = GC.CollectionCount(2) - _gc2;
        var instFps = frameMs > 0.001 ? 1000.0 / frameMs : 0;

        var sb = new StringBuilder(256);
        sb.Append(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));
        sb.Append(" DIP frame=");
        sb.Append(frameMs.ToString("0.0", CultureInfo.InvariantCulture));
        sb.Append("ms (~");
        sb.Append(instFps.ToString("0", CultureInfo.InvariantCulture));
        sb.Append("fps) rolling=");
        sb.Append(DeathbornGame.Instance.Fps);
        sb.Append(" update=");
        sb.Append(_updateMs.ToString("0.0", CultureInfo.InvariantCulture));
        sb.Append("ms draw=");
        sb.Append(_drawMs.ToString("0.0", CultureInfo.InvariantCulture));
        sb.Append("ms");
        sb.Append(" gc0=+").Append(dGc0);
        sb.Append(" gc1=+").Append(dGc1);
        sb.Append(" gc2=+").Append(dGc2);
        sb.Append(" recent=[").Append(FormatRecent()).Append(']');

        if (_suppressedSinceLastWrite > 0)
        {
            sb.Append(" suppressed=");
            sb.Append(_suppressedSinceLastWrite);
            _suppressedSinceLastWrite = 0;
        }

        foreach (var (key, value) in Notes)
        {
            sb.Append(' ').Append(key).Append('=').Append(value);
        }

        WriteLine(sb.ToString());
        _lastDipTick = now;
    }

    /// <summary>Attach a key/value shown on the next dip line (cleared each frame).</summary>
    public static void Note(string key, object? value)
    {
        if (!Enabled || string.IsNullOrEmpty(key)) return;
        Notes[key] = value?.ToString() ?? "";
    }

    public static void NoteFlag(string key, bool value)
    {
        if (!Enabled || !value) return;
        Notes[key] = "1";
    }

    private static string FormatRecent()
    {
        if (_recentFilled == 0) return "";

        var parts = new string[_recentFilled];
        for (var i = 0; i < _recentFilled; i++)
        {
            var idx = (_recentIdx - _recentFilled + i + RecentFrames) % RecentFrames;
            parts[i] = RecentMs[idx].ToString("0", CultureInfo.InvariantCulture);
        }

        return string.Join(',', parts);
    }

    private static void EnsureWriter()
    {
        if (_writer != null) return;

        lock (Gate)
        {
            if (_writer != null) return;

            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var instance = Config.DevInstance;
            _logPath = Path.Combine(dir, instance > 1 ? $"fps-dips-{instance}.log" : "fps-dips.log");
            _writer = new StreamWriter(new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true,
            };

            _writer.WriteLine();
            _writer.WriteLine($"=== FPS dip log started {DateTime.Now:yyyy-MM-dd HH:mm:ss} instance={instance} threshold={DipThresholdMs:0.0}ms ===");

            if (!_announced)
            {
                _announced = true;
                Console.WriteLine($"[DevPerfLog] Writing FPS dips (<30) to {_logPath}");
            }
        }
    }

    private static void WriteLine(string line)
    {
        lock (Gate)
        {
            _writer?.WriteLine(line);
        }
    }
}
