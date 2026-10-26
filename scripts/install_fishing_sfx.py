#!/usr/bin/env python3
"""Install fishing sound effects.

Source: "Fisheefects" by Memoraphile @ You're Perfect Studio (OpenGameArt, 2018),
https://opengameart.org/content/fisheefects - licensed CC0 (also CC-BY 4.0 / OGA-BY 3.0).
Sounds from their fishing jam game "Just for the Halibut!".

The pack ships 24-bit stereo WAVs with long silent tails. This script converts them to 16-bit
mono PCM (safe for MonoGame's WavImporter), trims trailing silence, writes them under
client/Deathborn.Client/Content/Audio/Sfx/Fishing/ and registers them in Content.mgcb.

Run: py -3 scripts/install_fishing_sfx.py [path/to/FisheefectsComplete.zip]
Without a path the zip is downloaded from OpenGameArt into a temporary folder.
"""
from __future__ import annotations

import io
import sys
import tempfile
import urllib.request
import wave
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "client/Deathborn.Client/Content/Audio/Sfx/Fishing"
MGCB = REPO / "client/Deathborn.Client/Content/Content.mgcb"
ZIP_URL = "https://opengameart.org/sites/default/files/FisheefectsComplete.zip"

# pack file -> (installed name, keep trailing silence trimmed?, max seconds)
SOUNDS = {
    "splash.wav": ("fishing-cast-splash", 1.2),
    "bloop.wav": ("fishing-bite", 0.6),
    "reel.wav": ("fishing-reel-loop", 9.0),
    "point_special.wav": ("fishing-catch", 1.7),
    "uhoh.wav": ("fishing-escape", 1.4),
    "cursor_select.wav": ("fishing-hook", 0.5),
}

MGCB_BLOCK = """#begin {path}
/importer:WavImporter
/processor:SoundEffectProcessor
/processorParam:Quality=Best
/build:{path}

#end {path}
"""


def to_mono16(raw: bytes, channels: int, width: int) -> list[int]:
    """Decode little-endian PCM (16/24/32-bit) into a list of mono 16-bit samples."""
    frame = channels * width
    samples = []
    for i in range(0, len(raw) - frame + 1, frame):
        total = 0
        for c in range(channels):
            chunk = raw[i + c * width:i + (c + 1) * width]
            value = int.from_bytes(chunk, "little", signed=True)
            total += value >> (8 * (width - 2)) if width > 2 else value
        samples.append(max(-32768, min(32767, total // channels)))
    return samples


def trim(samples: list[int], rate: int, max_seconds: float, threshold: int = 300) -> list[int]:
    samples = samples[: int(rate * max_seconds)]
    end = len(samples)
    while end > 0 and abs(samples[end - 1]) < threshold:
        end -= 1
    # Keep a short fade so the cut is not a click.
    end = min(len(samples), end + int(rate * 0.03))
    out = samples[:end]
    fade = min(len(out), int(rate * 0.02))
    for k in range(fade):
        out[len(out) - fade + k] = int(out[len(out) - fade + k] * (1 - k / fade))
    return out


def write_wav(path: Path, samples: list[int], rate: int) -> None:
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(b"".join(s.to_bytes(2, "little", signed=True) for s in samples))


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
    with zipfile.ZipFile(io.BytesIO(data)) as z, tempfile.TemporaryDirectory():
        for src, (name, max_seconds) in SOUNDS.items():
            with wave.open(io.BytesIO(z.read(src))) as w:
                rate = w.getframerate()
                samples = to_mono16(w.readframes(w.getnframes()), w.getnchannels(), w.getsampwidth())
            samples = trim(samples, rate, max_seconds)
            write_wav(OUT / f"{name}.wav", samples, rate)
            rel_paths.append(f"Audio/Sfx/Fishing/{name}.wav")
            print(f"{name}.wav  {len(samples) / rate:.2f}s")
    patch_mgcb(rel_paths)
    print(f"Installed {len(rel_paths)} fishing sounds under {OUT.relative_to(REPO)}")


if __name__ == "__main__":
    main()
