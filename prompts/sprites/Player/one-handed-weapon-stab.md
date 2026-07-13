Create ONE sprite sheet.

Use the master style specification.

==================================================
ATTACK TYPE
==================================================

One-handed weapon STAB / THRUST.

For daggers, short swords (thrust), spears (one hand), rapiers, and jabbing weapons.

A separate equipment layer will draw the weapon later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching.

This is NOT a boxing jab.

This is NOT a wide slashing arc.

Do NOT show knuckles driving straight forward like a fist strike.

Do NOT show a roundhouse or sweeping arm motion.

If the motion looks like a punch, the output is WRONG.

==================================================
HAND POSE (CRITICAL)
==================================================

The weapon hand uses a CLOSED GRIP around an invisible hilt.

Imagine thrusting a dagger or short sword with the blade in line with the forearm.

• fingers wrapped around the handle
• thumb locked on the grip side
• forearm and invisible blade point in the SAME straight line toward the target
• elbow drives the thrust, not a flicked fist

The off hand may guard, pull back, or balance — but must NOT punch.

Leave clear empty space IN FRONT of the grip along the thrust line for the blade layer.

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

Motion type: straight THRUST along the facing direction.

Phases across the 8 frames:

1. guard — compact stance, grip at hip or low chest
2. chamber — elbow draws back, grip near ribs, blade line aimed at target
3. lunge start — front foot steps, torso leans in
4. extension — arm extends fully, grip leads along a straight line
5. full thrust — maximum reach, body committed forward
6. hold — brief peak extension (weapon layer will show blade)
7. retract — elbow bends, grip pulls back toward guard
8. settle — return toward idle, feet planted

The motion must read as a STAB / THRUST, not a punch or slash.

Minimal sideways arc. Mostly forward linear motion.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any weapon, blade, glow, thrust trail, or VFX.

Only the base body with correct grip pose and thrust motion.

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
