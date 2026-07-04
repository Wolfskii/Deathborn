using Deathborn.Client.Net;

namespace Deathborn.Client.Gameplay;

public sealed class PmLine
{
    public string Text = "";
    public bool Outgoing;
    public string FromName = "";
}

public sealed class WhisperTarget
{
    public long CharacterId;
    public string Name = "";
    public long AccountId;
}

public sealed class PlayerFriends
{
    private readonly List<FriendEntryState> _entries = [];
    private readonly Dictionary<long, List<PmLine>> _messagesByCharacter = new();

    public IReadOnlyList<FriendEntryState> Entries => _entries;
    public WhisperTarget? SelectedWhisper { get; private set; }

    public void Apply(FriendsData data)
    {
        _entries.Clear();
        if (data.Friends is { Count: > 0 })
            _entries.AddRange(data.Friends);

        if (SelectedWhisper is { AccountId: > 0 } sel)
        {
            var match = _entries.FirstOrDefault(e => e.AccountId == sel.AccountId);
            if (match != null && match.CharacterId > 0)
            {
                SelectedWhisper = new WhisperTarget
                {
                    CharacterId = match.CharacterId,
                    Name = match.Name,
                    AccountId = match.AccountId,
                };
            }
        }
    }

    public void SelectWhisper(long characterId, string name, long accountId = 0)
    {
        if (characterId <= 0) return;
        if (accountId <= 0)
        {
            var friend = _entries.FirstOrDefault(e => e.CharacterId == characterId);
            accountId = friend?.AccountId ?? 0;
            if (string.IsNullOrEmpty(name) && friend != null)
                name = friend.Name;
        }
        SelectedWhisper = new WhisperTarget
        {
            CharacterId = characterId,
            Name = name,
            AccountId = accountId,
        };
    }

    public void SelectFriend(FriendEntryState entry)
    {
        if (entry.CharacterId > 0)
            SelectWhisper(entry.CharacterId, entry.Name, entry.AccountId);
        else
            SelectedWhisper = new WhisperTarget { Name = entry.Name, AccountId = entry.AccountId };
    }

    public void AddMessage(PmData pm, long localCharacterId)
    {
        long peerId;
        string peerName;
        if (pm.Outgoing)
        {
            peerId = SelectedWhisper?.CharacterId ?? 0;
            peerName = SelectedWhisper?.Name ?? pm.FromName;
        }
        else
        {
            peerId = pm.FromCharacterId;
            peerName = pm.FromName;
            if (SelectedWhisper == null || SelectedWhisper.CharacterId <= 0)
                SelectWhisper(peerId, peerName, pm.FromAccountId);
        }

        if (peerId <= 0)
            return;

        if (!_messagesByCharacter.TryGetValue(peerId, out var lines))
        {
            lines = [];
            _messagesByCharacter[peerId] = lines;
        }

        lines.Add(new PmLine
        {
            Text = pm.Text,
            Outgoing = pm.Outgoing || pm.FromCharacterId == localCharacterId,
            FromName = pm.FromName,
        });

        while (lines.Count > 80)
            lines.RemoveAt(0);
    }

    public IReadOnlyList<PmLine> MessagesFor(long characterId) =>
        characterId > 0 && _messagesByCharacter.TryGetValue(characterId, out var lines)
            ? lines
            : [];

    public bool IsFriendCharacter(long characterId) =>
        _entries.Any(e => e.IsConfirmedFriend && e.CharacterId == characterId);

    public bool HasPendingOutCharacter(long characterId) =>
        _entries.Any(e => e.PendingOut && e.CharacterId == characterId);

    public FriendEntryState? FindByCharacterId(long characterId) =>
        _entries.FirstOrDefault(e => e.CharacterId == characterId);

    public FriendEntryState? FindByAccountId(long accountId) =>
        _entries.FirstOrDefault(e => e.AccountId == accountId);
}
