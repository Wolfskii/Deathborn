#!/usr/bin/env python3
"""Install farming sound effects.

Source: "Cozy Farm SFX" by Bramble & Byte (OpenGameArt, https://opengameart.org/content/cozy-farm-sfx-50-farming-game-sound-effects-tools-harvest-shop-ui-jingles),
licensed CC-BY 4.0 - credit "Cozy Farm SFX" by Bramble & Byte when shipping.

The pack ships 16-bit mono 44.1 kHz WAVs, which WavImporter accepts as they are, so they are copied
unchanged into client/Deathborn.Client/Content/Audio/Sfx/Farming/ and registered in Content.mgcb.

Run: py -3 scripts/install_farming_sfx.py [path/to/cozy-farm-sfx-cc-by_0.zip]
Without a path the zip is downloaded from OpenGameArt into a temporary folder.
"""
from __future__ import annotations

import io
import sys
import urllib.request
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "client/Deathborn.Client/Content/Audio/Sfx/Farming"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"
ZIP_URL = "https://opengameart.org/sites/default/files/cozy-farm-sfx-cc-by_0.zip"
ZIP_PREFIX = "cozy-farm-sfx/sfx/"

# pack file -> installed name
SOUNDS = {
    "seed-plant.wav": "farming-seed-plant",
    "harvest-pop.wav": "farming-harvest-pop",
}

MGCB_BLOCK = """#begin {path}
/importer:WavImporter
/processor:SoundEffectProcessor
/processorParam:Quality=Best
/build:{path}

#end {path}
"""


def patch_mgcb(rel_paths: list[str]) -> None:
    text = MGCB.read_text(encoding="utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    added = [MGCB_BLOCK.format(path=r).replace("\n", nl) for r in rel_paths if f"#begin {r}" not in text]
    if added:
        if not text.endswith(nl):
            text += nl
        MGCB.write_text(text + nl.join(added), encoding="utf-8", newline="")


def main() -> None:
    if len(sys.argv) > 1:
        data = Path(sys.argv[1]).read_bytes()
    else:
        with urllib.request.urlopen(ZIP_URL) as resp:  # noqa: S310 - fixed https URL
            data = resp.read()

    OUT.mkdir(parents=True, exist_ok=True)
    rel_paths = []
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        for src, name in SOUNDS.items():
            wav = z.read(ZIP_PREFIX + src)
            (OUT / f"{name}.wav").write_bytes(wav)
            rel_paths.append(f"Audio/Sfx/Farming/{name}.wav")
            print(f"{name}.wav  {len(wav)} bytes")
    patch_mgcb(rel_paths)
    print(f"Installed {len(rel_paths)} farming sounds under {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    main()
