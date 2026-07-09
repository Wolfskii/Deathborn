---
name: sprite-sheet-prompts
description: >-
  Generate Deathborn cozy-dark-fantasy pixel-art sprite sheet prompts and
  process pasted icon sheets without AI. Use when creating or updating
  ability_sheet.png, cosmetic_sheet.png, hotbar icons, inventory icons,
  icon atlas prompts, green-screen chroma key sheets, or when the user pastes
  a new sprite sheet to install.
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
- **No baked UI borders** — game draws slot frames in UI
- **Flat chroma-key background** (`#00FF00`) for transparency in post
- See [reference.md](reference.md) for palette, inspirations, and cell layouts

## Workflow A — generate a prompt

1. Read [reference.md](reference.md) for the target atlas (`ability` or `cosmetic`).
2. Copy the prompt template from reference.md.
3. Fill in:
   - sheet title (e.g. "ability/item hotbar", "cosmetic headwear")
   - full icon list in row order (5 columns × 4 rows)
   - any sheet-specific notes (whimsical cosmetics, spell themes, empty cells)
4. **Always include** the chroma-key technical block:
   - flat `#00FF00` cell background
   - no decorative borders/frames
   - empty cells = solid green only
   - avoid `#00FF00` inside icon art
5. Output the finished prompt inside a single fenced code block for easy copy/paste.
6. Mention: prompt canvas `320×256`; process with `--chroma-key`; game install via `--install`.

Do **not** generate images unless the user explicitly asks. Default deliverable is the text prompt.

## Workflow B — user pasted a sprite sheet

When the user provides a new PNG sheet, install it with the slicer (no AI):

```bash
python scripts/slice_sprite_sheet.py "<path-to-sheet>" --atlas ability --chroma-key --install
python scripts/slice_sprite_sheet.py "<path-to-sheet>" --atlas cosmetic --chroma-key --install
```

Steps:

1. Identify atlas type from context or ask if unclear (`ability` vs `cosmetic`).
2. Save the attached image to a temp or content path if needed.
3. Run `scripts/slice_sprite_sheet.py` with `--chroma-key` and `--install`.
4. If the sheet has no green-screen bg (legacy art), omit `--chroma-key`.
5. If icons contain lots of green, use `--chroma-key "#FF00FF"`.
6. Report: input size, processed size, chroma settings, install path, sliced cell count.
7. Remind user to rebuild client content / restart game to see changes.

Optional inspection:

```bash
python scripts/slice_sprite_sheet.py sheet.png --atlas ability --chroma-key --cells-out .tile_debug/ability_cells --skip-empty
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
| `scripts/slice_sprite_sheet.py` | Standardize, chroma-key, slice, install |
| `scripts/sprite_sheet_atlases.json` | Atlas layouts, install paths, default key color |
