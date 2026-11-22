"""Verify the delivered DOCX's embedded images against the evidence ZIP."""
import hashlib
import json
import sys
from pathlib import Path
from xml.etree import ElementTree as ET
from zipfile import ZipFile

evidence = Path(sys.argv[1])
report = "Factory-Fairness-Report-ZH.docx"

with ZipFile(evidence) as package:
    assert package.testzip() is None
    manifest = json.loads(package.read("SHA256SUMS.json"))
    assert set(package.namelist()) == set(manifest) | {"SHA256SUMS.json"}
    for path, expected in manifest.items():
        assert hashlib.sha256(package.read(path)).hexdigest() == expected

    import io
    with ZipFile(io.BytesIO(package.read(report))) as docx:
        assert docx.testzip() is None
        document = ET.fromstring(docx.read("word/document.xml"))
        links = ET.fromstring(docx.read("word/_rels/document.xml.rels"))
        image_targets = {r.attrib["Id"]: r.attrib["Target"]
                         for r in links if r.attrib["Type"].endswith("/image")}
        drawings = document.findall(".//{*}drawing")
        assert len(drawings) == len(image_targets) == 8
        used = set()
        for drawing in drawings:
            blip = drawing.find(".//{*}blip")
            relation_id, = blip.attrib.values()
            relative_path = image_targets[relation_id]
            image = Path(relative_path).name
            assert docx.read(f"word/{relative_path}") == package.read(f"figures/{image}")
            used.add(relation_id)
        assert used == set(image_targets)
        text = [node.text or "" for node in document.findall(".//{*}t")]
        assert all(sum(t.startswith(f"圖 {n}　") for t in text) == 1 for n in range(1, 9))
        print(json.dumps(dict(
            evidence_sha256=hashlib.sha256(evidence.read_bytes()).hexdigest(),
            verified_members=len(manifest) + 1, embedded_drawings=len(drawings),
            caption_numbers=list(range(1, 9)), visual_status="NOT RUN"
        ), indent=2))
