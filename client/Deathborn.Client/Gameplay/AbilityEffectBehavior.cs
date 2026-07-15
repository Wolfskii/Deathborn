namespace Deathborn.Client.Gameplay;

public static class AbilityEffectBehavior
{
    public static void SyncOwnerPosition(
        IWorldEffect effect,
        IReadOnlyDictionary<long, PlayerEntity> players)
    {
        if (AbilityCatalog.Get(effect.AbilityId) is not { EffectPlacement: AbilityEffectPlacement.FollowOwner })
            return;
        if (players.TryGetValue(effect.OwnerId, out var owner))
            effect.Position = owner.Position;
    }
}
