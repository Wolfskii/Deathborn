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

One-handed weapon SLASH with SHIELD.

For sword-and-board, mace-and-shield, axe-and-shield, and similar one-hand weapon + off-hand shield combat.

Separate equipment layers will draw the weapon and shield later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching.

This is NOT boxing.

This is NOT shield bashing (no striking motion with the off hand).

This is NOT two-handed weapon use.

Do NOT show knuckles leading toward the target on the weapon hand.

Do NOT show the off hand swinging forward like a fist.

Do NOT show the off arm tucked behind the back like a one-handed-only slash.

If the shield arm drops or punches, the output is WRONG.

==================================================
HAND POSES (CRITICAL)
==================================================

WEAPON HAND (primary strike hand):

• CLOSED GRIP around an invisible hilt
• fingers wrapped around a cylinder
• thumb on the side of the grip
• wrist aligned with a blade extending from the hand
• knuckles face along the blade axis (sideways), NOT toward the enemy
• performs a compact SLASHING ARC around the shield line

SHIELD HAND (off hand — always guarding):

• CLOSED GRIP on an invisible shield grip / strap
• forearm raised in front of the torso at a defensive angle
• elbow bent, upper arm close to the body
• hand positioned where the center-inside of a round or kite shield would attach
• forearm stays between the body and the threat through the whole animation
• the shield arm may shift slightly to absorb the swing — but never drops, never punches, never reaches outward to strike

Leave clear empty space:

• beyond the weapon grip where the blade layer will be composited
• in front of the shield forearm where the shield layer will be composited

==================================================
ANIMATION
==================================================

Four cardinal directions — exactly one row each. No duplicate directions. No missing directions.

Sheet layout: 4 rows × 8 columns (row = facing, column = animation frame, left to right).

Row order is mandatory — do not reorder, skip, merge, or repeat any row:

Row 1: South — front view, character faces the camera
Row 2: East — profile facing right
Row 3: North — back view, character faces away from the camera
Row 4: West — profile facing left

Each row contains exactly 8 sword-and-shield slash frames in animation order.

Motion type: compact sword-and-board SLASH.

The character fights from behind the shield — shorter arc than a naked one-handed swing.

Phases across the 8 frames:

1. guard — shield forearm up, weapon hand at hip or tight ready behind the shield line
2. anticipation — slight lean, weapon hand draws back while shield arm holds position
3. wind-up — weapon elbow rises, grip chambers behind shoulder; shield arm braced forward
4. swing — weapon arm sweeps in a TIGHT ARC past the shield edge (not a wide wild swing)
5. impact — peak extension, torso rotates slightly; shield arm still guarding
6. follow-through — weapon arm continues a short distance past the target line
7. recovery — weapon hand returns toward guard; shield arm still raised
8. settle — return toward idle fighting stance, feet planted

The motion must read as SWORD-AND-SHIELD combat, not punch, not greatsword, not shield bash.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any weapon, sword, mace, shield, buckler, glow, slash trail, or VFX.

Do NOT draw a shield silhouette, wooden disc, or metal plate on the off hand.

Only the base body with correct weapon-grip and shield-guard poses.

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

==================================================
EQUIPMENT FOLLOW-UP
==================================================

After this body sheet, generate two separate equipment layers using future-equipment-prompt-template.md:

• Iron Sword (or mace / axe) — aligned to the weapon-hand grip and slash arc
• Wooden Shield — aligned to the shield-arm forearm guard pose

Both must match this sheet's frame count, frame size, timing, and poses exactly.
