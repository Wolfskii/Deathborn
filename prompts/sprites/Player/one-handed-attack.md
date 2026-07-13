Create ONE sprite sheet.

Use the master style specification.

==================================================
ATTACK TYPE
==================================================

One-handed weapon SLASH.

For swords, sticks, clubs, maces, hatchets, and other swinging one-handed weapons.

A separate equipment layer will draw the weapon later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching.

This is NOT boxing.

This is NOT an unarmed strike.

Do NOT show knuckles leading toward the target.

Do NOT show a jabbing fist.

Do NOT show a straight boxing punch.

If the hand looks like it is hitting with the fist, the output is WRONG.

==================================================
HAND POSE (CRITICAL)
==================================================

The weapon hand uses a CLOSED GRIP around an invisible hilt.

Imagine the character is holding a sword handle.

• fingers wrapped around a cylinder
• thumb on the side of the grip
• wrist aligned with a blade that extends outward from the hand
• knuckles face along the blade axis (sideways), NOT toward the enemy

The off hand may balance, guard the chest, or pull back — but must NOT punch.

Leave clear empty space beyond the grip where the blade will be composited later.

==================================================
ANIMATION
==================================================

Use the eight-direction sheet layout specification.

Each row contains exactly 8 one-handed slash frames in animation order.

Motion type: horizontal / diagonal SLASHING ARC.

Phases across the 8 frames:

1. guard — neutral ready stance, grip at hip or low guard
2. anticipation — slight lean back, grip hand pulls back
3. wind-up — elbow rises, grip hand draws back behind shoulder or hip
4. swing — arm sweeps in a wide ARC across the body
5. impact — maximum arc extension, torso rotated into the slash
6. follow-through — arm continues past the target line
7. recovery — arm returns toward center
8. settle — return toward idle, feet planted

The motion must read as a WEAPON SWING, not a punch.

Body rotation and shoulder drive sell the slash.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any weapon, stick, sword, glow, slash trail, or VFX.

Only the base body with correct grip pose and arc motion.

==================================================
REFERENCE IMAGES
==================================================

Reference Image 1

Use ONLY for:

- overall art direction
- color palette
- atmosphere
- pixel quality

Do NOT copy any characters.

Reference Image 2

Use ONLY as the exact player character.

This character MUST remain visually identical.

Only animate it.

==================================================
CHARACTER LOCK
==================================================

The attached Base Body is the canonical Deathborn player.

Every future sprite sheet must depict this exact same character.

Never redesign him.

Never reinterpret him.

Never improve him.

Never add clothing.

Never change proportions.

Never alter facial features.

Never alter hair.

Never alter colors.

Every future animation should look like frames that were drawn by the same pixel artist for the exact same sprite.
