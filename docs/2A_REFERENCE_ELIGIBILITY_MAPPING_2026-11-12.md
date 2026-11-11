# 2A 參考案資格：研究規則 vs 正式資料模型對照（2026-11-12）

> 根據使用者上傳 `eligibility_report.md` 與本分支 C# 模型／EF Core 定義進行**靜態程式碼對照**，不是已查詢 Supabase 的實際表結構或資料。不得將假設欄位誤認為現有正式欄位。

## A. 第七輪結果的正確理解

報告：合成候選 **35**，同結構限制「有」，假設忽略來源真實性後 **1** 案符合其餘示意條件，未達研究設定的最低 **5** 案；實際產學可採用的候選為 **0**，因為 35 件 `source_kind=synthetic`。

拒絕原因（可能同一案多重命中）：`wrong_structure=25`、`wrong_project_type=5`、`wrong_material=5`、`wrong_unit=5`、`incomplete_scope=5`、`not_approved=5`、`unverified_source=35`。其中不同結構比例高是**腳本注入設計**，不能解讀為崴正真實歷史中 25/35 不可比較。

這項研究只證明診斷腳本能列出拒絕原因，不代表正式 API 已具有這些防護。

## B. 靜態資料模型與規則映射

| 研究條件 | 正式程式／EF 模型可對應來源 | 可驗證程度與重大缺口 |
|---|---|---|
| 是否同一建案 | `ProjectMaster.ProjectId`、`HistoricalTransaction.ProjectMasterId` | **可推導**；需區分待估案是既有歷史 `ProjectId` 或獨立 AnalysisCase |
| 建案用途 | `ProjectMaster.ProjectType`；待估 `AnalysisCase.ProjectType` | **有欄位**；相同用途可檢查，但內容可能缺值／不一致 |
| 結構類型 | `ProjectMaster.StructureType`；`AnalysisCase.StructureType` | **有欄位**；正式相似度目前不同結構只減權重，無硬門檻 |
| 材料身份 | `HistoricalTransaction.MaterialId` → `MaterialMaster.MaterialId` | **有欄位**；是否同樣工程範圍／材料彙總口徑仍需查 |
| 材料標準單位 | `MaterialMaster.UnitId` → `UnitMaster`；`FinalBidItem.Unit` | **部分有欄位**；正式換算由 UnitConversionService 及審核流程負責；研究直接比字串 `kg`、`m³` 不等同正式換算／基準單位校驗 |
| Approved 狀態 | `ProjectMaster.HistoryStatus`、`HistoryApprovedAt/By` | **有欄位**；但 DEMO Approved 也可能存在，**Approved 不等於真實資料可信** |
| 正式最終標單來源 | `ProjectMaster.SourceFileName/Hash`、`FinalBidItem.SourceFileName/SheetName/RowNumber` | **可追檔名／Hash／列**；尚缺獨立驗證的真實來源類型與工程用途的來源資格聲明 |
| 工程範圍是否完整 | 工項 `FinalBidItem`、`MajorCategoryOverride`、`IsIgnored` | **不能可靠推導**整份結構體工程是否包含完整用量與所有計價項；目前 ProjectMaster 未見 `scope_quality` 等直接標記 |
| 真實／合成／外部來源證明 | 在已讀 `ProjectMaster`／`HistoricalTransaction` C# 模型中**未看到** `source_kind`、`is_synthetic` 之正式資格欄位 | **無法直接確認**。檔名出現 DEMO 只能提示，不是正式來源證書；未檢查 DB migration 外部版本，不得斷言資料庫絕對沒有其他欄位 |
| 有效建案個數 | `ProjectMaster.ProjectId` 的獨立案件數、`HistoricalTransaction` 材料聚合及 `EstimationSetting.MinimumSampleCount` | **可以在驗證資料完整性後計算**；交易行數不得當作建案個數；ESS 不是原始資料來源認證 |
| 價格大項 | `FinalBidItem.Amount/Qty/UnitPrice` 與大項分類 | **另行驗證**：本輪僅材料數量示意資格，不能推論結構體工程價格完整性 |

### 由此得出的差距

現有實作具備「用途、結構、材料、Approved 與數量」的基本欄位，但**真實來源認證、完整／部分工程覆蓋口徑**，並未在已讀 C# 主模型中找到可靠標記。**不得因 SourceFileHash 存在或 Approved=true 就推定資料可當正式基準**。

## C. 下一個唯讀驗證點（不修改資料庫）

1. 確認最新 `崴正/Data/ApplicationDbContext.cs` 映射與實際 PostgreSQL `information_schema.columns` 是否一致。
2. 只輸出 schema（資料表、欄位、型態、nullable）、不輸出姓名／帳號／真實標單原始明細。
3. 對每件 `ProjectMaster` 另進行**明確經授權且有資料遮罩的來源審核**：真正商務定案、工程範圍完整性、材料／單位標準化狀態與歷史事件的可信程度。
4. 應採「來源未核實 => 不具正式可用資格」的研究設計，而不是直接修改目前正式 DB 使現有查詢突然拒絕所有資料。
5. 待確認實際欄位、資料完整性與影響範圍，再提出 schema migration、UI 審核與模型服務的共同變更方案；不得只增加一個來源真假布林欄位就宣稱已完成驗證。

## D. 回溯與限制

- 研究輸出：使用者 2026-11-12 上傳的 `eligibility_report.md`。
- 研究程式：`research/2a_synthetic/eligibility_review.py`。
- 靜態核對：`崴正/Models/ProjectMaster.cs`、`FinalBidItem.cs`、`HistoricalTransaction.cs`、`MaterialMaster.cs`、`UnitMaster.cs`、`崴正/Data/ApplicationDbContext.cs`。
- 先前模型審查：`docs/2A_MODEL_AUDIT_2026-11-10.md`。
- 本輪**不進行**資料庫連線、SQL DDL/DML、正式演算法變動或真實工程估算精度宣稱。
