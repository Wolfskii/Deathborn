---
name: sprite-sheet-prompts
description: >-
  Generate Deathborn cozy-dark-fantasy pixel-art sprite sheet prompts and
  process pasted icon sheets without AI. Use when creating or updating
  ability_sheet.png, cosmetic_sheet.png, hotbar icons, inventory icons,
  icon atlas prompts, or when the user pastes a new sprite sheet to install.
---

# Deathborn sprite sheet prompts

## When to use

- User asks for an image-generation prompt for ability, cosmetic, item, or UI icon sheets
- User pastes or attaches a new sprite sheet to install in the game
- User wants icons to match Deathborn's cozy dark-fantasy pixel style

## Style (always apply)

Deathborn icon art = **dark fantasy lore + bright nostalgic pixel execution**.

- Cozy, charming, readable SNES/GBA indie RPG icons
- Colorful but controlled; not grim Diablo horror realism
- Crisp handcrafted pixels, soft ramps, subtle outlines
- See [reference.md](reference.md) for palette, inspirations, and cell layouts

## Workflow A — generate a prompt

1. Read [reference.md](reference.md) for the target atlas (`ability` or `cosmetic`).
2. Copy the prompt template from reference.md.
3. Fill in:
   - sheet title (e.g. "ability/item hotbar", "cosmetic headwear")
   - full icon list in row order (5 columns × 4 rows)
   - any sheet-specific notes (whimsical cosmetics, spell themes, empty cells)
4. Output the finished prompt inside a single fenced code block for easy copy/paste.
5. Mention technical targets: prompt canvas `320×256` (64px cells); game install size `1024×819`.

Do **not** generate images unless the user explicitly asks. Default deliverable is the text prompt.

## Workflow B — user pasted a sprite sheet

When the user provides a new PNG sheet, install it with the slicer (no AI):

```bash
python scripts/slice_sprite_sheet.py "<path-to-sheet>" --atlas ability --install
python scripts/slice_sprite_sheet.py "<path-to-sheet>" --atlas cosmetic --install
```

Steps:

1. Identify atlas type from context or ask if unclear (`ability` vs `cosmetic`).
2. Save the attached image to a temp or content path if needed.
3. Run `scripts/slice_sprite_sheet.py` with `--install`.
4. Report: input size, standardized size, install path, number of sliced cells.
5. Remind user to rebuild client content / restart game to see changes.

Optional inspection:

```bash
python scripts/slice_sprite_sheet.py sheet.png --atlas ability --cells-out .tile_debug/ability_cells --skip-empty
```

## Adding a new atlas type

1. Add layout + cell names to `scripts/sprite_sheet_atlases.json`.
2. Add C# atlas class under `client/Deathborn.Client/Rendering/`.
3. Register PNG in `client/Deathborn.Client/Content/Content.mgcb`.
4. Document the new sheet in [reference.md](reference.md).

## Files

| File | Purpose |
|------|---------|
| [reference.md](reference.md) | Style guide, cell maps, prompt template |
| `scripts/slice_sprite_sheet.py` | Standardize + slice + install sheets |
| `scripts/sprite_sheet_atlases.json` | Atlas layouts and install paths |
