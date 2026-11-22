import json
from pathlib import Path

folder = Path("evidence/word")
pages = (folder / "extracted.txt").read_text(encoding="utf-8").split("\f")
if pages[-1] == "":
    pages.pop()
images = sorted(folder.glob("page-*.png"), key=lambda image: int(image.stem.split("-")[-1]))
assert len(pages) == len(images) and pages
report = []
for number, (page, image) in enumerate(zip(pages, images, strict=True), 1):
    lines = [line.strip() for line in page.splitlines() if line.strip()]
    report.append(dict(page=number, printed_page_number=lines[-1] if lines else None,
                       starts_with_old_orphan=bool(lines and lines[0].startswith("來源的部署錯配率")),
                        image=image.name, text_chars=len(page.strip()),
                       cjk_chars=sum("\u4e00" <= c <= "\u9fff" for c in page),
                       replacement_chars=page.count("\ufffd"),
                       visual_verdict="REVIEW_REQUIRED"))
(folder / "page-checks.json").write_text(json.dumps(report, indent=2) + "\n")
if any(row["printed_page_number"] != str(row["page"]) or row["starts_with_old_orphan"]
       for row in report):
    raise SystemExit("PDF footer sequence or former orphan-line check failed")
