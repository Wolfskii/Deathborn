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

Animation:
Walking

Four diagonal directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 4 rows × 8 columns (row = facing, column = animation frame, left to right).

Output canvas: exactly 1774×887 pixels (width × height). The full image must be exactly this size.

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South-East — diagonal front-right quarter view
Row 2: North-East — diagonal back-right quarter view
Row 3: North-West — diagonal back-left quarter view
Row 4: South-West — diagonal front-left quarter view

Each row contains exactly 8 walk-cycle frames in animation order.

Natural walk cycle.

Gentle body bounce.

Relaxed arm swing.

Consistent stride length.

Perfect looping animation.

Reference Image 1

Use ONLY as the exact player character.

This character MUST remain visually identical.

Only animate it.

CHARACTER LOCK (BODY LAYER ONLY)

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
