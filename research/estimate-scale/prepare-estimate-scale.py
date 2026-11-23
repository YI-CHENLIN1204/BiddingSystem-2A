"""Instrument the disposable runner checkout only."""
import os
from pathlib import Path
n=int(os.environ["RESEARCH_SCALE_ROWS"])
assert n in (500,1000) and os.environ.get("GITHUB_ACTIONS")=="true"
root=Path("research/acceptance-ci")
fixture=root/"Program.cs"
source=fixture.read_text(encoding="utf-8-sig")
anchor="new[] { 2, 20, 100 }"
assert source.count(anchor)==1
fixture.write_text(source.replace(anchor,f"new[] {{ {n} }}"),encoding="utf-8")
service=Path("崴正/Services/AnalysisCaseService.cs")
source=service.read_text(encoding="utf-8-sig")
start=source.index("CreateFromUploadAsync(")
end=source.index("AddVersionFromOneDAsync(",start)
block=source[start:end]
save="await _context.SaveChangesAsync(cancellationToken);"
commit="await transaction.CommitAsync(cancellationToken);"
assert block.count(save)==3 and block.count(commit)==1
block=block.replace("var analysisCase =", "var dbElapsed = new System.Diagnostics.Stopwatch();\n                var analysisCase =")
for statement in (save,commit):
    block=block.replace(statement,"dbElapsed.Start();\n                "+statement+"\n                dbElapsed.Stop();")
block=block.replace("return ServiceResult.Ok(new", 'Console.WriteLine(FormattableString.Invariant($"T08_DB {analysisCase.AnalysisCaseId} {dbElapsed.Elapsed.TotalMilliseconds:F6}"));\n\n                return ServiceResult.Ok(new')
service.write_text(source[:start]+block+source[end:],encoding="utf-8")
(root/"benchmark-estimate-api.py").write_bytes(Path("research/estimate-scale/benchmark-estimate-scale.py").read_bytes())
