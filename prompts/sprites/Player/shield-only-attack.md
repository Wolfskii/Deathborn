Create ONE sprite sheet.

Use the master style specification.

==================================================
ATTACK TYPE
==================================================

SHIELD ONLY — shield bash / shield strike.

For characters with a shield equipped and NO weapon in the other hand.

A separate equipment layer will draw the shield later.
This sheet is BODY ONLY.

==================================================
NOT THIS ANIMATION
==================================================

This is NOT punching with the empty hand.

This is NOT boxing.

This is NOT a one-handed sword slash.

This is NOT two-handed weapon use.

Do NOT show the free hand striking forward like a jab or hook.

Do NOT show the free hand gripping an invisible sword.

Do NOT draw a fist driving into the target with the off hand — the SHIELD ARM performs the strike.

If the empty hand punches while the shield arm stays passive, the output is WRONG.

==================================================
HAND POSES (CRITICAL)
==================================================

SHIELD HAND (strike hand — primary motion):

• CLOSED GRIP on an invisible shield grip / strap behind the shield center
• forearm starts raised in guard in front of the torso
• the whole shield arm pushes forward in a short BASH / RAM motion
• elbow extends, shoulder drives forward, body leans into the strike
• imagine the invisible shield face slamming into the enemy
• hand stays fixed on the shield grip throughout — do not open into a palm strike

FREE HAND (no weapon):

• EMPTY — relaxed open hand OR loose fist held at the chest, hip, or slightly back
• must NOT strike, jab, or swing
• may balance the body during the bash but stays out of the attack line
• clearly reads as "no weapon equipped"

Leave clear empty space IN FRONT of the shield forearm where the shield layer will be composited.

==================================================
ANIMATION
==================================================

Use the eight-direction sheet layout specification.

Each row contains exactly 8 shield-bash frames in animation order.

Motion type: forward SHIELD BASH along the facing direction.

Phases across the 8 frames:

1. guard — shield forearm up, free hand relaxed at chest or hip
2. chamber — shield arm pulls back slightly, torso coils, free hand stays passive
3. lunge start — knees bend, weight shifts forward
4. bash — shield arm extends forcefully straight ahead along the facing line
5. impact — maximum forward reach, body committed into the bash
6. recoil — shield arm bends back from the hit, slight stagger in the shoulders
7. recovery — shield arm returns to guard height, free hand still passive
8. settle — return toward idle, feet planted

The motion must read as a SHIELD BASH, not a punch from the empty hand.

Short, heavy, forward ram — not a wide slashing arc.

==================================================
MODULAR LAYER RULE
==================================================

Do NOT draw any shield, buckler, wooden disc, metal plate, weapon, glow, impact VFX, or motion lines.

Do NOT draw a shield silhouette on the striking arm.

Only the base body with correct shield-grip bash motion and empty off-hand.

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

After this body sheet, generate one equipment layer using future-equipment-prompt-template.md:

• Wooden Shield (or buckler / kite shield) — aligned to the shield-arm bash motion

Must match this sheet's frame count, frame size, timing, and poses exactly.

No weapon layer is needed for this loadout.
