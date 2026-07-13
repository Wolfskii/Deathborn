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

Four cardinal directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 4 rows × 8 columns (row = facing, column = animation frame, left to right).

Output canvas: exactly 1774×887 pixels (width × height). The full image must be exactly this size.

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South — front view, character faces the camera
        Slash plane: VERTICAL downward heavy chop toward the ground in front of the body.
        Both hands on the haft travel from high (above shoulder) down to low (hip/thigh).
        Motion in the FRONTAL plane (toward camera), NOT a sideways profile swing.
        WRONG: East-style horizontal side-chop while the torso faces the camera.

Row 2: East — profile facing right
        Slash plane: horizontal heavy arc in the RIGHT profile plane (edge-on).
        Haft sweeps across the body left-to-right on screen.

Row 3: North — back view, character faces away from the camera
        Slash plane: vertical upward / overhead chop in the REAR plane (away from camera).

Row 4: West — profile facing left
        Slash plane: horizontal heavy arc in the LEFT profile plane (mirror of East).

Each row contains exactly 8 slash frames in animation order.

==================================================
SLASH DIRECTION (CRITICAL)
==================================================

Each row uses a DIFFERENT slash plane matched to that row's facing.
Do NOT copy the same two-handed swing into every row.

• South: downward vertical chop in the frontal plane (high → low toward viewer)
• East / West: horizontal side swings in the profile planes
• North: upward / overhead chop in the rear plane

Phases across the 8 frames (adapt the arc to each row's plane above):

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
REFERENCE IMAGE
==================================================

Reference Image 1

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
