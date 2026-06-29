namespace Deathborn.Client.Audio;

public sealed class MusicPlaylist
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<string> Tracks { get; init; }
}
