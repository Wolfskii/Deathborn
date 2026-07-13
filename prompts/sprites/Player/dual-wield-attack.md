Create ONE sprite sheet.

Use the master style specification.

==================================================
ATTACK TYPE
==================================================

DUAL WIELD — two one-handed weapon SLASHES.

For two swords, two daggers, two hatchets, or any paired one-hand weapons.

Separate equipment layers will draw each weapon later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching.

This is NOT boxing.

This is NOT two fists striking.

This is NOT a two-handed greatsword swing (hands do NOT share one grip).

This is NOT sword-and-shield (neither arm stays in passive guard).

Do NOT show knuckles leading toward the target on either hand.

Do NOT show one fist jabbing while the other grips nothing.

If either hand looks like a bare fist strike, the output is WRONG.

==================================================
HAND POSES (CRITICAL)
==================================================

BOTH hands use an independent CLOSED GRIP around an invisible hilt.

Imagine the character holds a short sword in each hand.

RIGHT HAND and LEFT HAND (each):

• fingers wrapped around a cylinder grip
• thumb on the side of the hilt
• wrist aligned with a blade extending outward from that hand
• knuckles face along the blade axis (sideways), NOT toward the enemy
• each arm moves on its own arc — NOT locked together like a spear or greatsword

The two arms may slash in sequence or cross past each other, but BOTH always read as weapon grips.

Leave clear empty space beyond EACH grip where the left and right weapon layers will be composited later.

==================================================
ANIMATION
==================================================

Use the eight-direction sheet layout specification.

Each row contains exactly 8 dual-wield slash frames in animation order.

Motion type: dual SLASHING ARCS — agile, compact, offensive.

Suggested flow (both arms weapon grips throughout):

1. guard — both hilts held ready, elbows bent, blades pointed outward
2. anticipation — torso coils, both arms draw back slightly
3. primary wind-up — lead arm (usually the forward-side hand) chambers back
4. primary slash — lead arm sweeps in a wide ARC; rear arm still in grip, follows or guards
5. cross / secondary wind-up — rear arm chambers as lead arm recovers
6. secondary slash — second arm sweeps the opposite ARC across the body
7. recovery — both arms return toward center, still in grip
8. settle — return toward idle fighting stance, feet planted

The motion must read as DUAL WEAPON SWINGS, not flurry of punches.

Light footwork. Quick torso rotation. Arms alternate or cross — never box.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any weapon, sword, dagger, glow, slash trail, or VFX.

Do NOT draw blades in either hand.

Only the base body with correct dual-grip poses and slash motion.

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

After this body sheet, generate TWO separate equipment layers using future-equipment-prompt-template.md:

• Iron Sword — main-hand layer, aligned to the primary slash arc
• Iron Sword — off-hand layer, aligned to the secondary slash arc (mirror or second copy as needed)

Both weapon layers must match this sheet's frame count, frame size, timing, and poses exactly.
