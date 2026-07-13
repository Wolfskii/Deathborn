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

One-handed UNARMED punch / strike.

For bare-fist combat, brawling, and monk-style attacks.

No weapon will be composited on this animation.

==================================================
THIS IS PUNCHING (INTENTIONAL)
==================================================

Unlike the weapon attack prompts, this animation IS an unarmed fist strike.

• closed fist
• knuckles lead toward the target
• short forward jab OR wide hook arc (choose one style and keep it consistent across all frames)
• no grip pose, no invisible hilt

Use the RIGHT hand as the primary striking hand unless the direction requires mirroring.

The off hand may guard the jaw, chamber for a follow-up, or pull back for balance.

==================================================
CHOOSE ONE VARIANT (pick before generating)
==================================================

Variant A — JAB:

straight forward punch, elbow extends, fist travels in a short linear line

Variant B — HOOK:

horizontal arcing punch, elbow bent, fist sweeps sideways

State which variant you are generating in your workflow notes.
Do not mix jab and hook frames in the same sheet.

==================================================
ANIMATION
==================================================

Four diagonal directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 4 rows × 8 columns (row = facing, column = animation frame, left to right).

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South-East — diagonal front-right quarter view
Row 2: North-East — diagonal back-right quarter view
Row 3: North-West — diagonal back-left quarter view
Row 4: South-West — diagonal front-left quarter view

Each row contains exactly 8 punch frames in animation order.

Phases across the 8 frames:

1. guard — fists up, elbows bent, bouncy ready stance
2. chamber — striking fist draws back, torso coils
3. extension start — fist begins forward / arc motion
4. impact — full extension or peak hook arc, weight shifts into the strike
5. follow-through — slight overshoot
6. recovery — fist pulls back
7. guard return — both hands return to defensive position
8. settle — return toward idle

Snappy, readable, cute — not realistic martial arts mocap.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw weapons, gloves, knuckle dusters, or strike VFX.

Body and empty hands only.

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
