#!/usr/bin/env python3
"""Install the watering-can pour sound.

Source: "Watering with a Watering Can" by Bricklover on Freesound,
https://freesound.org/people/Bricklover/sounds/532890/ - licensed CC0 (no attribution required).
The Cozy Farm pour sounded like a fountain, so this steady pour is used instead.

Decodes the MP3 preview with soundfile (pip install soundfile), keeps a short steady slice of the
pour, fades it in and out, normalises it and writes 16-bit mono 44.1 kHz WAV under
client/Deathborn.Client/Content/Audio/Sfx/Farming/farming-watering.wav. The file is already listed
in Content.mgcb from install_farming_sfx.py, so the content list is not changed.

Run: py -3 scripts/install_watering_sfx.py [path/to/532890_8919922-hq.mp3]
Without a path the preview is downloaded from Freesound into a temporary folder.
"""
from __future__ import annotations

import sys
import tempfile
import urllib.request
from pathlib import Path

import numpy as np
import soundfile as sf

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "client/Deathborn.Client/Content/Audio/Sfx/Farming/farming-watering.wav"
PREVIEW_URL = "https://cdn.freesound.org/previews/532/532890_8919922-hq.mp3"

# Steady part of the pour (the clip is quiet for the first 0.75 s and stops at 10.6 s).
START_S = 0.9
LENGTH_S = 0.64  # matches the Water clip: 8 frames x 0.08 s
FADE_IN_S = 0.08
FADE_OUT_S = 0.12
PEAK = 0.7


def main() -> None:
    if len(sys.argv) > 1:
        source = Path(sys.argv[1])
    else:
        source = Path(tempfile.mkdtemp()) / "watering.mp3"
        with urllib.request.urlopen(PREVIEW_URL) as resp:  # noqa: S310 - fixed https URL
            source.write_bytes(resp.read())

    data, rate = sf.read(str(source), dtype="float32")
    mono = data.mean(axis=1) if data.ndim > 1 else data
    start = int(START_S * rate)
    clip = mono[start:start + int(LENGTH_S * rate)].copy()

    fade_in = int(FADE_IN_S * rate)
    fade_out = int(FADE_OUT_S * rate)
    clip[:fade_in] *= np.linspace(0.0, 1.0, fade_in, dtype=np.float32)
    clip[-fade_out:] *= np.linspace(1.0, 0.0, fade_out, dtype=np.float32)
    clip *= PEAK / max(1e-6, float(np.abs(clip).max()))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    sf.write(str(OUT), clip, rate, subtype="PCM_16")
    print(f"Wrote {OUT.relative_to(REPO)}  {len(clip) / rate:.2f}s at {rate} Hz")


if __name__ == "__main__":
    main()
