# T00～T11 研究驗收補測｜2026-11-23

以 `cbf7cbeaae27ea3a8b754bc34cbc0f1ada2c6372` 為驗收來源。本機正式分支工作樹原有修改保留。此批測試、修正副本及紀錄在研究目錄。正式分支、A0 參數與 Supabase 無寫入。

## 項目與下個驗收步驟

| 項目 | 全項狀態 | 本輪證據或下一步 |
|---|---|---|
| T00 | PARTIAL | 本輪沿用原始結果。取得原住宅 CSV／manifest、獨立時點資料及隔離 API 後重播及核對 SQL 候選。 |
| T01 | PARTIAL | 本輪無新資料。取得獨立廠商標單與人工核定標準材料，盲測正式 67 詞。 |
| T02 | PARTIAL | 本輪無新資料。分離 train-only 詞彙，用跨來源住宅／廠房測學習曲線。 |
| T03 | PARTIAL | 真人計時 NOT RUN。招募受測員，以難度相近的授權標單執行 AB/BA。 |
| T04 | PARTIAL | 修正副本 PASS，原版 13/23，修正版 23/23。尚缺隔離 DB 的真實 EF 載入、工法／規格安全及相關流程驗收。 |
| T05 | PARTIAL | 沿用廠房封存結果。取得住宅原始包與新工程可得時間後重播及做盲測。 |
| T06 | PARTIAL | 沿用合成壓力結果。另請工程師獨立標註正常、異常及不確定並覆核歧義。 |
| T07 | PARTIAL | 乾淨 Parser 78 條 PASS。隔離 DB E2E NOT RUN。取得可丟棄 PostgreSQL，核對 Draft、人工映射、發佈、重開、稽核、再次讀取與重複提交。 |
| T08 | PARTIAL | 離線同案同 K 全流程階段計時 PASS。完整 B1、HTTP/API 計時及上傳入庫 NOT RUN。 |
| T09 | PARTIAL | 乾淨 Parser 與提交交接檢查 PASS。角色／未授權操作 DB E2E NOT RUN。取得隔離環境後，以測試角色覆蓋讀寫、拒絕、狀態及 audit。 |
| T10 | PARTIAL | 尚缺跨來源未見同義名、真值及實際覆核速度。 |
| T11 | PARTIAL | 沿用 B1 重建與 B1-K 控制變體結果。取得獨立工程及作者實作與參數再驗。 |
| Word | NOT RUN | Word COM 80070520，未取得逐頁渲染。需可用的 Word 登入工作階段或 LibreOffice headless，再輸出 PDF、逐頁記錄中文、圖表、頁碼、目錄及數值。 |

## T04 研究副本

`dotnet build UnitSafety.csproj -c Release`（.NET SDK 10.0.301）後執行 `dotnet run --project UnitSafety.csproj -c Release --no-build`。使用同一套 fixture 替換成基準提交中 `崴正/Services/UnitConversionService.cs`，原版退出 1，修正版退出 0。基準服務 Git blob 為 `1f60e1ec7b12e950b844162b8637b6a2fc2da008`。程式與逐案輸出以 SHA256SUMS.json 為準。

`UnitMasterService.cs` 建立與更新已檢查啟用類別的正係數。研究副本在換算執行路徑額外拒絕缺值、不大於零、比值溢位或捨入至零。換算結果溢位或非零輸入捨入至零時回傳既有失敗 DTO，保留實際零值及同單位通過。金額守恆覆蓋可精確表示的雙向測例，任意十進位比率仍可能發生正常捨入。

正式後續候選變更檔是 `崴正/Services/UnitConversionService.cs`，先對照當時正式 HEAD 再移植兩段數值防護。模型 `UnitMaster`、`UnitCategory` 及 `UnitMasterService` 無需因本修正改 schema。呼叫端 `FinalBidService`、`FinalBidQueryService`、`DetectionCategoryService`、`DetectionCategoryRangeService`、`TwoAPreprocessService`、`TwoAQuantityAnalysisService` 需核對換算失敗是否正確顯示、跳過或阻止發布。CanConvert 只檢查單位因子，具體數量／單價的表示範圍須由 Convert 判定。

## T08 離線計時

```powershell
python benchmark_pipeline.py <repo>/research/2a_synthetic/kde_comparison/replay_factory_equal_n.py <凍結廠房來源目錄> <封存equal_sample_per_case.csv> <新的輸出目錄>
```

同一批 30 件共同可估案項、相同工料條件、各方法各自選回固定 5 筆參考，執行 3 批並互換順序。180 個案項／方法／批次輸出與封存參考 ID 和 P15/P50/P85 核對通過。來源載入與校驗 153.2853 ms。共用 Python 進程峰值 RSS 62,373,888 B，不能歸到個別方法。每批清 B1-K 群聚快取，批內按候選池共用。單案項百分位混合批內快取狀態，三批 p95 為最大值。

| 方法 | 單案項 p50／p95 ms | 30 案項批次 p50／p95 ms | 選案 p50 ms | 群聚 p50 ms | 帶寬 p50 ms |
|---|---:|---:|---:|---:|---:|
| A0-K | 0.1227／0.3335 | 4.2740／6.5262 | 0.08485 | 不適用 | 不適用 |
| B1-K | 179.4084／237.8833 | 5617.6285／5818.4816 | 0.29400 | 1.7472 | 166.6137 |

完整 B1 的全群重建存在，保留為原 E3 證據。其選擇及實際參考數可能不同，未作為嚴格同 K 計時對手。本輪無 API 延遲或每方法獨立峰值 RSS。逐案各階段輸出見 pipeline-cases.csv。

## 環境與外部驗收條件

Docker daemon 拒絕存取，WSL 列舉也回傳 Wsl/E_ACCESSDENIED。未發現原生 PostgreSQL server 工具。Word COM 80070520，其他可用 renderer 未找到，LibreOffice 下載端點無法連線。錯誤保留於 environment.log。正式與雲端資料庫均未連線。

外部所需資料與最小欄位見[前輪補測](../t00t11-acceptance-20261123/README.md#尚缺的最小輸入)。本輪沒有新授權住宅原始快照、獨立工料真值、工程師異常標註或真人工時。原始 T00～T11 驗收門檻維持。

## T07／T09 乾淨 Parser 重建

Git fetch 連線受阻。從 GitHub 指定提交逐檔取回 Parser 與所參照後端專案的 83 個編譯來源，逐一校驗 Git blob ID（parser-source-blobs.json）。獨立空目錄未帶入任何 bin／obj 或既有 Release DLL。建置以本機 NuGet 快取還原。這是驗收提交的編譯來源快照。

```powershell
python verify_parser_snapshot.py <乾淨來源根目錄> parser-source-blobs.json
dotnet build <來源根目錄>/tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release -p:NuGetAudit=false --ignore-failed-sources
dotnet run --project <來源根目錄>/tests/WorkbookParserTests/WorkbookParserTests.csproj -c Release --no-build
```

建置 0 警告／0 錯誤，測試退出碼 0，78 條 PASS。新 DLL SHA-256 `89cfbfdc1fcbe80968199ec451362496862d9970013efe136c139505de54f04a`。parser-build.log 與 parser-run.log 為本次輸出。
