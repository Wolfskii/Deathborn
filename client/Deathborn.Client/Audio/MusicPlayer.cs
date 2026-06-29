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
        MediaPlayer.Volume = volume;
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
        MediaPlayer.Volume = _volume;
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
