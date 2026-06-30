using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Deathborn.Client.Gameplay;

public interface IWorldEffect
{
    bool Alive { get; }
    long OwnerId { get; }
    string AbilityId { get; }
    Vector2 Position { get; }
    bool DrawUnderEntities { get; }
    bool CanClash { get; }
    float HitRadius { get; }

    void Update(
        float dt,
        IReadOnlyDictionary<long, PlayerEntity> players,
        IReadOnlyList<InteractableEntity> interactables,
        bool reportHits,
        Action<long, int>? onPlayerHit);

    void Draw(SpriteBatch sb, Vector2 screenPos, float zoom);
    void CancelByClash();
}
