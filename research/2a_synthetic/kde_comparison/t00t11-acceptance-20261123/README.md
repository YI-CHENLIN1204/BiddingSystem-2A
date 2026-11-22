# T00～T11 補測紀錄｜2026-11-23（臺灣）

基準 `3eef29061e8ab46a12da62c2a4b390f5031bfdba`。執行環境為 Windows、Python 3.12.10、.NET SDK 10.0.301。保留原 E1/E2/E3 結果及 Word 原檔。服務測試使用合成 fixture，研究批次無正式 API 或 Supabase 連線。

| 項目 | 全項狀態 | 本輪證據與仍缺項目 |
|---|---|---|
| T00 | PARTIAL | 沿用已封存廠房 TRAIN 查核。住宅快照、正式 SQL/API 等價及獨立時點資料未取得。 |
| T01 | PARTIAL | 既有合成證據保留。正式 67 詞的合法獨立廠商標單與人工核定真值未取得。 |
| T02 | PARTIAL | 已有合成曲線。真正獨立來源的訓練詞與跨住宅／廠房測試未取得。 |
| T03 | PARTIAL | 既有情境估算。真人交叉計時 NOT RUN。 |
| T04 | FAIL | 新增 13 個服務邊界案例，10 個通過，兩個零係數拋出 DivideByZeroException，負係數被當作成功且產生負數量及負單價。隔離 DB、規格／工料流程與核准流 NOT RUN。 |
| T05 | PARTIAL | 廠房合成結果已封存。住宅原始重播及真實時點盲測 NOT RUN。 |
| T06 | PARTIAL | 既有合成壓力結果保留。工程師異常真值 NOT RUN。 |
| T07 | PARTIAL | 既有 Release 解析器程式重新執行，78 條 PASS 訊息，退出碼 0。Draft→人工映射→Publish→Reopen→Audit 及正式 API 等價 NOT RUN。 |
| T08 | PARTIAL | K5 同案同樣本數的離線預測計時 PASS，見下方。全流程選案與群聚、API p50/p95、RSS、上傳含 DB NOT RUN。 |
| T09 | PARTIAL | 工作樹 Python 回歸 18/18、既有 Release 解析器 78 條 PASS 訊息、證據檔核對 PASS。隔離角色與 API 權限 NOT RUN。 |
| T10 | PARTIAL | 既有合成 TF-IDF 證據保留。工程師核定的未見名稱及實際審查速度 NOT RUN。 |
| T11 | PARTIAL | 既有廠房 B1 重建、控制變體及敏感度證據保留。獨立盲測與作者原實作／參數仍缺。 |
| Word 視覺驗收 | NOT RUN | OOXML／ZIP PASS，Word COM 啟動 FAIL（80070520）。找不到可執行的 LibreOffice，未產生 PDF。 |

## T04：換算邊界

使用研究基準上 Git blob `1f60e1ec7b12e950b844162b8637b6a2fc2da008` 的 UnitConversionService.cs 原碼編譯。測試以記憶體資料列替代 EF DbContext，會執行服務內主檔載入、正規化與換算。合法雙向重量換算維持數量乘單價，面積和體積不可互換，關閉跨單位換算仍允許同單位。零與負係數直接置入 fixture，證據只適用於服務接收到這類設定時。在既有正式 DB 唯讀快照沒有看到這類資料。

從 repo 根執行 PowerShell，`$run` 指向本目錄。

```powershell
Copy-Item 崴正/Services/UnitConversionService.cs "$run/UnitConversionService.cs"
dotnet run --project "$run/UnitSafety.csproj" -c Release
```

預期重現此輪結果為退出碼 1，[逐案證據](t04.jsonl) 列出所有個案。

## T08：K5 等工作量預測計時

使用 E2 的 30 個共同可估案項（18 結構體金額、6 混凝土、6 鋼筋），每方法每案項 5 個封存參考，各計算 P15/P50/P85。方法各用自己已選定的參考 ID。每方法先暖機 1 批，再交替順序執行 10 批，60 組輸出核對封存數值通過。

| 方法 | 批次 p50 | 批次 p95（nearest rank） |
|---|---:|---:|
| A0-K | 0.1225 ms | 0.2356 ms |
| B1-K | 4787.9139 ms | 5011.4075 ms |

B1-K 包含每案項臨界帶寬求解。此值只反映單機 Python 預測階段，排除資料載入、選案、群聚、API 和 DB。10 次試驗的 p95 即最大值。B1-K 是控制變體。完整方法 B1 未在本輪計時。

```powershell
$env:PYTHONPATH = (Resolve-Path research/2a_synthetic/kde_comparison).Path
python "$run/benchmark_equal_workload.py" <廠房來源目錄> <封存等樣本逐案CSV> <新結果JSON>
```

使用封存證據包的 `equal_sample_per_case.csv` 與 `factory-synthetic-v1-20261122.zip` 解出的原始目錄。計時原值與來源雜湊見 [t08.json](t08.json)。

## T07、T09 與 Word

- `docker info --format '{{.ServerVersion}}'` 對 `npipe:////./pipe/docker_engine` 回傳 permission denied。PATH 與 Program Files 探查未找到可用 PostgreSQL 工具。隔離 DB host 未建立，因此未對任何資料庫執行寫入。
- `python -m unittest discover -s research/2a_synthetic -p "test_*.py" -v` 在本機工作樹執行 18 個案例。解析器以 `dotnet tests/WorkbookParserTests/bin/Release/net10.0/WorkbookParserTests.dll` 執行既有二進位，SHA256 `cf9c35154ef93b2704a7f4dafbd944385b91cfbaa7e308cc51028e394fab202b`。本機 checkout 為 `ba050b2c13c1e36b0034985de7ae15cc40b184f1` 且有既存變更，兩項只能作本機回歸，未證實基準提交的乾淨建置。
- `python "$run/verify_word_package.py" <Factory-Fairness-V2-Evidence.zip>` 核對 64 個成員及 SHA-256、8 張嵌圖位元及圖號。Word COM 80070520，系統未提供可用 `soffice`，故無字型、跨頁表頭、裁切或分頁人工判定。

## 尚缺的最小輸入

檢查本機研究資料和 Library 的工作區相關搜尋與精確檔名。找到的可重現建案資料為合成廠房。未定位到下列可合法使用且足以驗收的原始輸入。正式 DB 已有的住宅 TRAIN 和合成廠房 TRAIN 無法替代獨立測試。

| 項目 | 來源與必要欄位 |
|---|---|
| T00／T05 住宅重播 | 原住宅資料保管人提供 `graduation-src-extreme-v3` 原始包，含 manifest、三份 manifest 所列 CSV、雜湊、split、建案／明細鍵、原始時點和目標真值。 |
| T01／T02／T05／T10／T11 | 工程資料權利人授權的去識別住宅／廠房最終標單，具不同廠商或來源批次、穩定案號、版次、來源時間、估算可得時間、實際決標／發布／入庫日期、章節、原始品名、規格、工料範圍、單位、數量、單價、金額、工程師核准的標準材料／單位和真值。由保管人分派互斥的訓練、驗證、盲測集。T11 若要驗原法完全一致，另需作者釋出的原碼和參數。 |
| T06 | 工程師獨立標註，含匿名案號、估算時點、工料範圍、單位、材料、正常／異常／不確定、判定理由、來源 SHA、審查員識別及歧義覆核。使用 [異常標註模板](../input_templates/expert_anomaly_labels_TEMPLATE.csv)。 |
| T03／T10 人工效率 | 授權的至少兩份難度相配且去重的標單及受測員。記匿名受測員與案／檔雜湊、AB/BA 順序、流程、設定／對應／覆核／修正和總秒數、核定正確／錯配／未解列數。使用 [工時模板](../input_templates/human_time_trials_TEMPLATE.csv)。 |
| T04／T07／T08／T09 | 可丟棄的本地 PostgreSQL 與獨立 API，合成測試角色、含合法與非法案例的 fixture、資料庫端點與 DB 名稱證據。另需可渲染 Word 的執行環境完成逐頁驗收。 |
