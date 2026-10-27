#!/usr/bin/env python3
"""Install the tilling sound: a trowel turning soft soil (grass and dirt, not metal).

Source: "Digging Into Soft Soil with Small Trowell" by dhallcomposer on Freesound,
https://freesound.org/people/dhallcomposer/sounds/696531/ - licensed CC0 (no attribution required).
The Cozy Farm hoe takes sounded like a metal thud, so they are replaced.

Decodes the MP3 preview with soundfile (pip install soundfile) and cuts two single scoops of
soil, one from each of two points in the recording, so the game can alternate between them.
The recording is mostly low rumble (about 40% below 150 Hz), so each scoop is high-passed and a
short crisp crumble of filtered noise is added for the lighter, Minecraft-like dig feel. Each clip
is faded in and out and normalised, then written as 16-bit mono WAV under
client/Deathborn.Client/Content/Audio/Sfx/Farming/ and registered in Content.mgcb.

Run: py -3 scripts/install_tilling_sfx.py [path/to/696531_7678208-hq.mp3]
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
OUT = REPO / "client/Deathborn.Client/Content/Audio/Sfx/Farming"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"
PREVIEW_URL = "https://cdn.freesound.org/previews/696/696531_7678208-hq.mp3"

# (installed name, start in seconds, length in seconds). Each cut holds one scoop.
SCOOPS = [
    ("farming-till-soil", 4.2, 0.7),
    ("farming-till-soil-2", 6.0, 0.6),
]
FADE_IN_S = 0.02
FADE_OUT_S = 0.15
PEAK = 0.6
HIGHPASS_HZ = 350.0
CRUMBLE_LOW_HZ = 1500.0
CRUMBLE_HIGH_HZ = 5000.0
CRUMBLE_DECAY_S = 0.03
CRUMBLE_LENGTH_S = 0.12
CRUMBLE_GAIN = 3.0

MGCB_BLOCK = """#begin {path}
/importer:WavImporter
/processor:SoundEffectProcessor
/processorParam:Quality=Best
/build:{path}

#end {path}
"""


def biquad(x: np.ndarray, rate: int, cutoff: float, kind: str) -> np.ndarray:
    """RBJ biquad (Q 0.707), 'hp' or 'lp', applied in one pass."""
    w0 = 2 * np.pi * cutoff / rate
    cos, alpha = np.cos(w0), np.sin(w0) / (2 * 0.707)
    if kind == "hp":
        b = np.array([(1 + cos) / 2, -(1 + cos), (1 + cos) / 2])
    else:
        b = np.array([(1 - cos) / 2, 1 - cos, (1 - cos) / 2])
    a = np.array([1 + alpha, -2 * cos, 1 - alpha])
    b, a = b / a[0], a / a[0]
    y = np.zeros_like(x, dtype=np.float64)
    x1 = x2 = y1 = y2 = 0.0
    for i, xi in enumerate(x.astype(np.float64)):
        yi = b[0] * xi + b[1] * x1 + b[2] * x2 - a[1] * y1 - a[2] * y2
        y[i] = yi
        x2, x1, y2, y1 = x1, xi, y1, yi
    return y.astype(np.float32)


def crumble(rate: int, seed: int) -> np.ndarray:
    """A short burst of band-limited noise with a fast decay: the crisp part of a dig."""
    n = int(CRUMBLE_LENGTH_S * rate)
    noise = np.random.default_rng(seed).standard_normal(n).astype(np.float32)
    band = biquad(biquad(noise, rate, CRUMBLE_LOW_HZ, "hp"), rate, CRUMBLE_HIGH_HZ, "lp")
    t = np.arange(n) / rate
    return band * np.exp(-t / CRUMBLE_DECAY_S).astype(np.float32)


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
        source = Path(sys.argv[1])
    else:
        source = Path(tempfile.mkdtemp()) / "soil.mp3"
        with urllib.request.urlopen(PREVIEW_URL) as resp:  # noqa: S310 - fixed https URL
            source.write_bytes(resp.read())

    data, rate = sf.read(str(source), dtype="float32")
    mono = data.mean(axis=1) if data.ndim > 1 else data

    OUT.mkdir(parents=True, exist_ok=True)
    rel_paths = []
    for name, start_s, length_s in SCOOPS:
        start = int(start_s * rate)
        clip = biquad(mono[start:start + int(length_s * rate)].copy(), rate, HIGHPASS_HZ, "hp")
        burst = CRUMBLE_GAIN * crumble(rate, seed=len(rel_paths))
        clip[:len(burst)] += burst
        fade_in = int(FADE_IN_S * rate)
        fade_out = int(FADE_OUT_S * rate)
        clip[:fade_in] *= np.linspace(0.0, 1.0, fade_in, dtype=np.float32)
        clip[-fade_out:] *= np.linspace(1.0, 0.0, fade_out, dtype=np.float32)
        clip *= PEAK / max(1e-6, float(np.abs(clip).max()))
        path = OUT / f"{name}.wav"
        sf.write(str(path), clip, rate, subtype="PCM_16")
        rel_paths.append(f"Audio/Sfx/Farming/{name}.wav")
        print(f"{name}.wav  {len(clip) / rate:.2f}s at {rate} Hz")
    patch_mgcb(rel_paths)
    print(f"Installed {len(rel_paths)} tilling sounds under {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    main()
