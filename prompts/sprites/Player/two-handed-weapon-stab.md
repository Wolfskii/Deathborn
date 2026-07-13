Create ONE sprite sheet.

Use the master style specification.

==================================================
ATTACK TYPE
==================================================

Two-handed weapon STAB / THRUST.

For spears, pikes, lances, halberds (thrust), and other two-handed piercing attacks.

A separate equipment layer will draw the weapon later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching.

This is NOT two separate jabs with each fist.

This is NOT a wide slashing arc.

Do NOT show fists leading the motion.

Do NOT show independent left/right strikes.

If the motion looks like double punching or a brawl, the output is WRONG.

==================================================
HAND POSE (CRITICAL)
==================================================

BOTH hands share ONE CLOSED GRIP on an invisible spear haft.

Imagine a two-handed spear thrust.

• front hand and rear hand on the same shaft line
• hands separated along the haft but moving together
• wrists aligned with the shaft
• shaft line points straight at the target

The body leans into the thrust — shoulders square to the facing direction.

Leave clear empty space IN FRONT of the forward hand along the thrust line for the weapon layer.

==================================================
ANIMATION
==================================================

Eight directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 8 rows × 8 columns (row = facing, column = animation frame, left to right).

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South — front view, character faces the camera
Row 2: South-East — diagonal front-right quarter view
Row 3: East — profile facing right
Row 4: North-East — diagonal back-right quarter view
Row 5: North — back view, character faces away from the camera
Row 6: North-West — diagonal back-left quarter view
Row 7: West — profile facing left
Row 8: South-West — diagonal front-left quarter view

Each row contains exactly 8 thrust frames in animation order.

Motion type: straight two-handed THRUST along the facing direction.

Phases across the 8 frames:

1. guard — wide stance, haft held at hip or low ready
2. chamber — rear hand pulls back, point aimed at target, slight crouch
3. lunge start — back leg drives, torso pitches forward
4. extension — both arms extend, haft slides through the front hand
5. full thrust — maximum reach, body fully committed
6. hold — brief peak extension
7. retract — hands pull haft back, body recovers upright
8. settle — return toward idle, feet planted

The motion must read as a SPEAR / PIKE THRUST, not a punch or chop.

Minimal sideways arc. Mostly forward linear motion.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any weapon, spear tip, glow, thrust trail, or VFX.

Only the base body with correct two-hand grip pose and thrust motion.

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
