using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Deathborn.Client.Audio;

public static class MusicPlayer
{
    private static readonly Dictionary<string, Song> SongCache = new(StringComparer.Ordinal);

    private static Song? _current;
    private static MusicPlaylist? _activePlaylist;
    private static IReadOnlyList<Song>? _playlistSongs;
    private static int _playlistIndex;
    private static float _volume = 0.55f;
    private static bool _trackPlaying;
    private static bool _stopRequested;

    public static bool IsMuted { get; private set; }
    public static float Volume => _volume;
    /// <summary>Volume shown on the slider; 0 while muted.</summary>
    public static float DisplayVolume => IsMuted ? 0f : _volume;

    public static void ApplySavedSettings()
    {
        IsMuted = AudioSettings.LoadMuted();
        _volume = AudioSettings.LoadVolume();
    }

    public static void SetMuted(bool muted, bool save = true)
    {
        if (IsMuted == muted) return;

        IsMuted = muted;
        ApplyVolume();

        if (save)
            AudioSettings.Save(IsMuted, _volume);
    }

    public static void SetVolume(float volume, bool save = true)
    {
        _volume = Math.Clamp(volume, 0f, 1f);
        if (_volume > 0f && IsMuted)
            IsMuted = false;
        ApplyVolume();

        if (save)
            AudioSettings.Save(IsMuted, _volume);
    }

    private static void ApplyVolume()
    {
        if (_trackPlaying)
            MediaPlayer.Volume = IsMuted ? 0f : _volume;
    }

    public static void Play(Song song, float volume = 0.55f, bool loop = true)
    {
        ClearPlaylistState();
        _volume = volume;
        _stopRequested = false;
        _trackPlaying = true;

        if (_current == song && MediaPlayer.State == MediaState.Playing)
            return;

        _current = song;
        MediaPlayer.IsRepeating = loop;
        ApplyVolume();
        MediaPlayer.Play(song);
    }

    public static void PlayPlaylist(ContentManager content, MusicPlaylist playlist, float volume = 0.55f)
    {
        if (playlist.Tracks.Count == 0)
            return;

        _volume = volume;
        _stopRequested = false;
        _trackPlaying = true;
        _activePlaylist = playlist;
        _playlistSongs = playlist.Tracks.Select(path => LoadSong(content, path)).ToArray();
        _playlistIndex = 0;
        PlayPlaylistTrack(_playlistIndex);
    }

    public static void Update()
    {
        if (_stopRequested || !_trackPlaying || _activePlaylist is null || _playlistSongs is null)
            return;

        if (_playlistSongs.Count <= 1)
            return;

        if (MediaPlayer.State != MediaState.Stopped)
            return;

        _playlistIndex = (_playlistIndex + 1) % _playlistSongs.Count;
        PlayPlaylistTrack(_playlistIndex);
    }

    public static void Stop()
    {
        _stopRequested = true;
        _trackPlaying = false;
        ClearPlaylistState();

        if (MediaPlayer.State == MediaState.Stopped)
        {
            _current = null;
            return;
        }

        MediaPlayer.Stop();
        _current = null;
    }

    private static void PlayPlaylistTrack(int index)
    {
        var song = _playlistSongs![index];
        _current = song;
        MediaPlayer.IsRepeating = _playlistSongs.Count == 1;
        ApplyVolume();
        MediaPlayer.Play(song);
    }

    private static Song LoadSong(ContentManager content, string path)
    {
        if (!SongCache.TryGetValue(path, out var song))
        {
            song = content.Load<Song>(path);
            SongCache[path] = song;
        }

        return song;
    }

    private static void ClearPlaylistState()
    {
        _activePlaylist = null;
        _playlistSongs = null;
        _playlistIndex = 0;
    }
}
