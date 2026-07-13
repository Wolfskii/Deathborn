Generate a modular equipment sprite sheet for Deathborn.

Reference the supplied **bald base body** sprite sheet for the same animation clip.

The layer must align PERFECTLY with the body mannequin.

Do not redraw the body.

Only generate pixels for this layer item:

**{LAYER_NAME}**

Layer slot: **{LAYER_ID}**

Every frame must match the body sheet:

- Cardinals and diagonals are separate sheets (same folder layout as body animations)
- Cardinals row order: South, East, North, West
- Diagonals row order: South-East, North-East, North-West, South-West
- Frame count, frame size, animation timing, perspective, lighting, pixel density, pose, pivot point

The layer should animate naturally with the body's motion.

Output only this layer's pixels on a solid #00FF00 chroma-key background.

Do not include skin, underwear, or other equipment — only this item.

If this is a **hair** layer, use the canonical hair palette from `layers/manifest.json` (base #503728, highlight #6E503C, shadow #322318) so hair color can be remapped at runtime.

If this is **starter-chest**, **starter-legs**, or **starter-boots**, draw simple medieval starter gear that covers the underwear on the body layer.

If this is a **weapon** or **shield**, align to the body's grip pose and leave correct depth ordering (weapon in front of body where appropriate).
