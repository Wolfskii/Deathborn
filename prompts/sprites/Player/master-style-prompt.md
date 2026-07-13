You are creating production-ready pixel art assets for an upcoming MMORPG called "Deathborn".

Note: Each animation folder under `prompts/sprites/Player/<animation>/` contains self-contained `cardinals.md` and `diagonals.md` prompts with this style block inlined for copy-paste. Edit this file first, then regenerate those prompts if the style changes.

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
MODULAR LAYERS
==================================================

Body animation prompts produce a **bald mannequin** only (skin + underwear + face).

Hair, clothing, armor, weapons, and shields are separate composited sprite layers.

Layer catalog: `prompts/sprites/Player/layers/manifest.json`

Layer prompt template: `prompts/sprites/Player/layers/layer-prompt-template.md`

Canonical body palette (remapped at runtime for skin tone / eye color):

• Skin base #D8B48C, shadow #B08460
• Eye white #F0F0F0, iris #3C5078

Hair layers use canonical hair palette (remapped at runtime for hair color).