#!/usr/bin/env python3
"""Copy shared/*.json into Go embed paths so server cannot drift from shared/.

Authoritative files live under shared/. Server packages use //go:embed on local
copies (Go forbids .. in embed paths). Run before server build / via Taskfile.
"""
from __future__ import annotations

import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

COPIES = [
    (ROOT / "shared" / "abilities.json", ROOT / "server" / "internal" / "game" / "abilities" / "abilities.json"),
    (ROOT / "shared" / "protocol.json", ROOT / "server" / "internal" / "protocol" / "protocol.json"),
]


def main() -> int:
    for src, dst in COPIES:
        if not src.is_file():
            print(f"missing source: {src}", file=sys.stderr)
            return 1
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, dst)
        print(f"synced {src.relative_to(ROOT)} -> {dst.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
