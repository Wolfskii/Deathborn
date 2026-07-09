# Deathborn sprite sheet reference

## Visual direction (current)

Deathborn UI icons use **cozy dark fantasy** pixel art:

- Dark fantasy lore, but bright nostalgic SNES/GBA readability
- Charming, handcrafted, slightly cute — not grim horror realism
- Colorful but controlled palette; readable on dark UI backgrounds
- Crisp pixels, soft ramps, subtle outlines, gentle highlights

**Inspirations:** Eastward, CrossCode, Moonlighter, Sea of Stars (palette), Hero Siege, Secrets of Grindea

**Avoid:** photorealism, painterly rendering, muddy blacks, excessive blood/grime, Diablo-style grim darkness

## Atlas specs

| Preset | Canvas | Grid | Cell |
|--------|--------|------|------|
| `game` (MonoGame) | 1024×819 | 5×4 | ~204×204 (integer division) |
| `canonical` (prompts) | 320×256 | 5×4 | 64×64 |

Background: soft dark purple-gray (`#1A1624`), not pure black.

Optional border: subtle tarnished bronze / worn iron.

## Ability sheet (`ability`)

| Row | Col 0 | Col 1 | Col 2 | Col 3 | Col 4 |
|-----|-------|-------|-------|-------|-------|
| 0 | slash | shield_bash | whirlwind | warrior_dash | empty |
| 1 | fireball | ice_shard | arc_bolt | blood_bolt | poison_cloud |
| 2 | battle_shout | iron_skin | hunter_mark | bandage | second_wind |
| 3 | health_potion | mana_potion | stamina_potion | antidote | house_key |

**Install:** `client/Deathborn.Client/Content/Icons/ability_sheet.png`  
**Code:** `client/Deathborn.Client/Rendering/AbilityIconAtlas.cs`

## Cosmetic sheet (`cosmetic`)

| Row | Col 0 | Col 1 | Col 2 | Col 3 | Col 4 |
|-----|-------|-------|-------|-------|-------|
| 0 | santa_hat | party_hat | jester_cap | bucket_helmet | pirate_hat |
| 1 | propeller_hat | bunny_ears | top_hat | traffic_cone | beer_helm |
| 2 | wizard_hat_torn | crown_of_bones | rubber_chicken_hat | grim_hood | gold_helm_rusty |
| 3 | fedora_of_shame | clown_nose_glasses | severed_elf_hat | empty | empty |

**Install:** `client/Deathborn.Client/Content/Icons/cosmetic_sheet.png`  
**Code:** `client/Deathborn.Client/Rendering/CosmeticIconAtlas.cs`

## Prompt template

Use this structure for every new sheet prompt. Fill in `SHEET TITLE`, `ICON LIST`, and any sheet-specific notes.

```text
Create a single sprite sheet image: a uniform grid of square [SHEET TITLE] icons for a dark-fantasy top-down MMORPG called "Deathborn".

========================================================
THEME & MOOD
========================================================

Deathborn is a dark fantasy world, but viewed through nostalgic pixel art.

The world is dangerous, cursed, and dying — yet the visuals remain charming and readable.

Mood:
- dark fantasy
- mysterious
- melancholy
- ancient
- cursed
- magical
- slightly cute
- nostalgic
- handcrafted

Avoid:
- grim realism
- horror realism
- excessive blood
- excessive darkness
- muddy colors
- photorealism
- painterly rendering

Prefer:
- brighter readable colors
- colorful but muted palette
- rich reds
- indigo blues
- moss greens
- warm amber
- bone ivory
- soft purple
- steel gray

Magic should feel mysterious rather than horrifying.
Icons should feel like they belong in a bright cozy pixel-art world with dark fantasy lore.

========================================================
PIXEL ART STYLE (MOST IMPORTANT)
========================================================

Create authentic handcrafted pixel art.

Style goals:
- charming
- nostalgic
- colorful
- highly readable
- slightly cute
- polished
- handcrafted

NOT gritty realism, horror painting, concept art, or photorealistic rendering.

Visible pixels are REQUIRED.

Use:
- crisp pixel edges
- clean pixel clusters
- soft color ramps
- subtle outlines
- selective highlights
- smooth pixel shading

Visual inspirations:
- Eastward
- CrossCode
- Moonlighter
- Sea of Stars (palette only)
- Hero Siege
- Secrets of Grindea

Dark fantasy theme, cute pixel execution.

========================================================
READABILITY (CRITICAL)
========================================================

Recognizable at 64×64, 48×48, and 32×32.
One primary object per cell, ~70–80% of tile, strong silhouette, minimal clutter.

========================================================
CONSISTENCY
========================================================

Same pixel density, shading, lighting, border style, and saturation across all cells.

========================================================
COMPOSITION
========================================================

Centered focal object, square RPG inventory icon style.

Lighting:
- soft ambient light
- gentle rim lighting
- warm highlights
- soft magical glow
- avoid harsh black shadows

========================================================
TECHNICAL REQUIREMENTS
========================================================

Grid: 5 columns × 4 rows (20 cells)
Each cell: 64×64 pixels
Canvas: 320×256 pixels
No spacing between cells
Background: soft dark purple-gray (#1A1624)
Optional subtle tarnished bronze border
No text, letters, numbers, or logos

========================================================
ICON LIST
========================================================

[ICON LIST — one line per cell, row by row]

========================================================
FINAL QUALITY GOAL
========================================================

Production-ready indie pixel RPG sprite sheet.
Cozy dark fantasy — cute, crisp, colorful, highly readable, consistent.
Ready for immediate slicing into 64×64 game assets.
```

## Installing a pasted sheet (no AI)

```bash
python scripts/slice_sprite_sheet.py path/to/new_sheet.png --atlas ability --install
python scripts/slice_sprite_sheet.py path/to/new_sheet.png --atlas cosmetic --install
```

Optional flags:

- `--target canonical` — keep 320×256 instead of game size
- `--cells-out .tile_debug/ability_cells` — custom output folder
- `--skip-empty` — skip unnamed cells
- `--sheet-out /tmp/ability_standardized.png` — custom standardized sheet path

Cell names and layout: `scripts/sprite_sheet_atlases.json`
