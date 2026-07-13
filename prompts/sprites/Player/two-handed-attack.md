Create ONE sprite sheet.

Use the master style specification.

==================================================
ATTACK TYPE
==================================================

Two-handed weapon SLASH.

For greatswords, battle axes, war hammers, halberds (swing), and other heavy two-handed swings.

A separate equipment layer will draw the weapon later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching.

This is NOT boxing.

This is NOT an unarmed strike.

Do NOT show either fist striking forward alone.

Do NOT separate the hands into two independent punches.

If the motion looks like double punching, the output is WRONG.

==================================================
HAND POSE (CRITICAL)
==================================================

BOTH hands share ONE CLOSED GRIP on an invisible weapon haft.

Imagine gripping a greatsword or axe handle with both hands close together.

• hands stacked or slightly offset on the same handle line
• fingers wrapped around the haft
• wrists aligned with the weapon shaft
• hands move together as a single unit throughout the swing

The arms, shoulders, and torso power a heavy ARC — not a push or punch.

Leave clear empty space beyond the hands where the weapon head/blade will be composited later.

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

Each row contains exactly 8 slash frames in animation order.

Motion type: heavy two-handed SLASHING ARC.

Phases across the 8 frames:

1. guard — wide stance, haft grip held low or at shoulder
2. anticipation — weight shifts back, grip pulls away from target
3. wind-up — weapon-side shoulder coils back, grip rises
4. swing start — hips and shoulders begin rotation into the arc
5. impact — maximum rotation, arms extended through the slash plane
6. follow-through — large overshoot, strong body twist
7. recovery — hands pull back toward center, torso unwinds
8. settle — return toward idle, feet planted

Heavy movement.

Strong body rotation.

Large follow-through.

The motion must read as a HEAVY WEAPON SWING, not unarmed striking.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any weapon, axe head, hammer head, glow, slash trail, or VFX.

Only the base body with correct two-hand grip pose and arc motion.

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
