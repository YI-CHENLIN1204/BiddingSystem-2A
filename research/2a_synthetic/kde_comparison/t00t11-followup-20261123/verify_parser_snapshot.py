"""Validate reconstructed GitHub source blobs before compiling the Parser."""
import hashlib
import json
import sys
from pathlib import Path

root, manifest = map(Path, sys.argv[1:])
entries = json.loads(manifest.read_text(encoding="utf8"))


def blob_id(content):
    return hashlib.sha1(f"blob {len(content)}\0".encode() + content).hexdigest()


for name, sha in entries.items():
    path = root / name
    raw = path.read_bytes()
    lf = raw.replace(b"\r\n", b"\n")
    candidates = (raw, raw.removesuffix(b"\n"), lf, lf.removesuffix(b"\n"))
    exact = next((data for data in candidates if blob_id(data) == sha), None)
    if exact is None:
        raise SystemExit(f"Git blob mismatch: {name}")
    if raw != exact:
        path.write_bytes(exact)

assert {path.relative_to(root).as_posix() for path in root.rglob("*") if path.is_file()} == set(entries)
print(f"Verified {len(entries)} source blobs.")
