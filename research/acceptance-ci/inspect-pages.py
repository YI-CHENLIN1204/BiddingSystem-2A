import json
from pathlib import Path

folder = Path("evidence/word")
pages = (folder / "extracted.txt").read_text(encoding="utf-8").split("\f")
if pages[-1] == "":
    pages.pop()
images = sorted(folder.glob("page-*.png"))
assert len(pages) == len(images) and pages
report = []
for number, (page, image) in enumerate(zip(pages, images, strict=True), 1):
    report.append(dict(page=number, image=image.name, text_chars=len(page.strip()),
                       cjk_chars=sum("\u4e00" <= c <= "\u9fff" for c in page),
                       replacement_chars=page.count("\ufffd"),
                       visual_verdict="REVIEW_REQUIRED"))
(folder / "page-checks.json").write_text(json.dumps(report, indent=2) + "\n")
