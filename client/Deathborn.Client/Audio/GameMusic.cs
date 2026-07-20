namespace Deathborn.Client.Audio;

public static class GameMusic
{
    public const string StartingArea = "starting_area";
    public const string LoginTheme = "Audio/Songs/The Reaper\u2019s Call";

    private static readonly Dictionary<string, MusicPlaylist> Playlists = new(StringComparer.Ordinal)
    {
        [StartingArea] = new MusicPlaylist
        {
            Id = StartingArea,
            Name = "Starting Area",
            Tracks =
            [
                "Audio/Songs/The Light of the Living (Instrumental)",
            ],
        },
    };

    public static MusicPlaylist Get(string id) =>
        Playlists.TryGetValue(id, out var playlist)
            ? playlist
            : throw new KeyNotFoundException($"Unknown music playlist: {id}");
}
