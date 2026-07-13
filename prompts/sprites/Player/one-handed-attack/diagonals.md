You are creating production-ready pixel art assets for an upcoming MMORPG called "Deathborn".

==================================================
STYLE
==================================================

Deathborn is a hardcore permadeath MMORPG.

Despite its brutal gameplay, the visual style is cute, charming and highly readable.

Think:

• Ragnarok Online
• Moonlighter
• Children of Morta
• The Swords of Ditto
• classic SNES RPGs

NOT anime.

NOT realistic.

NOT painterly.

Everything must look handcrafted pixel art.

No anti-aliasing.

No blurry edges.

No AI painting.

No gradients outside pixel shading.

Every pixel should look intentionally placed.

==================================================
VIEW
==================================================

Top-down RPG perspective.

Approximately 45°.

Consistent perspective.

Consistent scale.

Consistent lighting from upper-left.

==================================================
OUTPUT
==================================================

Produce ONE sprite sheet only.

Uniform grid.

Every frame perfectly aligned.

Equal spacing.

No frame overlaps.

No cropping.

No missing frames.

No labels.

No guides.

No borders.

No shadows outside the sprite.

==================================================
BACKGROUND
==================================================

The entire background MUST be solid chroma-key green.

Exactly RGB(0,255,0).

Exactly #00FF00.

No gradients.

No shadows.

No noise.

No compression artifacts.

This background will be removed automatically by software.

==================================================
ANIMATION PROMPT
==================================================

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

Four diagonal directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 4 rows × 8 columns (row = facing, column = animation frame, left to right).

Output canvas: exactly 1774×887 pixels (width × height). The full image must be exactly this size.

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South-East — diagonal front-right quarter view
        Slash plane: diagonal DOWNWARD chop toward the front-right ground quadrant.
        Grip hand travels high (front-right) to low (near right hip / front foot).
        WRONG: horizontal profile side-swing while facing SE.

Row 2: North-East — diagonal back-right quarter view
        Slash plane: diagonal upward / back-right chop in the NE plane (away from camera, toward back-right).
        Grip hand drives from low-front to high-back-right.

Row 3: North-West — diagonal back-left quarter view
        Slash plane: diagonal upward / back-left chop in the NW plane (mirror of NE).

Row 4: South-West — diagonal front-left quarter view
        Slash plane: diagonal DOWNWARD chop toward the front-left ground quadrant.
        Grip hand travels high (front-left) to low (near left hip / front foot).
        WRONG: horizontal profile side-swing while facing SW.

Each row contains exactly 8 one-handed slash frames in animation order.

==================================================
SLASH DIRECTION (CRITICAL)
==================================================

Each row uses a DIFFERENT slash plane matched to that row's diagonal facing.
Do NOT copy the same arm motion into every row.

• South-East & South-West: downward diagonal chops toward the front ground (front-facing diagonals)
• North-East & North-West: upward / back diagonal chops away from the camera (rear-facing diagonals)

The invisible blade follows the grip hand through that row's plane only.

Phases across the 8 frames (adapt the arc to each row's plane above):

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
REFERENCE IMAGE
==================================================

Reference Image 1

Use ONLY as the exact player character.

This character MUST remain visually identical.

Only animate it.

==================================================
CHARACTER LOCK (BODY LAYER ONLY)
==================================================

The attached Base Body is the canonical Deathborn player mannequin.

Every body animation sheet must depict this exact same bald character.

Never redesign him.

Never reinterpret him.

Never improve him.

Never add hair — hair is a separate composited layer.

Never add clothing, armor, boots, gloves, weapons, or shields — equipment is separate.

Never change proportions.

Never alter facial structure.

Skin and eye colors use the canonical palette from base-body.md — remapped at runtime.

Every future body animation should look like frames drawn by the same pixel artist for the exact same sprite.
