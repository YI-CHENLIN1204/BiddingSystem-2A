# 量價估算與標單異常偵測功能整合說明

本文件提供給主系統整合人員，用來快速理解本專案目前完成的功能、資料庫對應方式、程式碼位置、整合順序與注意事項。

本專案目前是以 ASP.NET MVC 形式完成「量價估算、標單異常偵測、決標回寫」等功能原型，尚未完全拆成獨立模組。因此整合主系統時，建議先處理資料庫對應，再逐步整合 Controller、Service、Model、View 與 Layout。

---

# 1. 專案功能定位

本專案主要提供營造／營建標單處理相關輔助功能，包含：

1. 標單異常偵測
2. 工程估算／用量率概算
3. 決標資料回寫
4. 建案列表
5. 相似建案查詢
6. 參數管理
7. 未對應工項處理
8. Audit Log 操作紀錄基礎模型

本系統定位為：

> 系統初判 + 人工複核

本專案目前不支援從圖面自動算量，也不取代正式估算人員。  
目前功能主要是針對既有標單、歷史成交資料與標準工項進行估算輔助與異常檢核。

---

# 2. 目前已完成功能

## 2.1 標單異常偵測

功能說明：

使用者上傳 Excel 標單後，系統會解析標單內的工項、規格、單位、數量與單價，並根據系統建議單價或標準單價進行異常判斷。

目前已完成：

1. Excel 標單上傳
2. 標單資料解析
3. 總筆數、正常數、異常數、待審數統計
4. 異常項目置頂
5. 優先處理項目顯示
6. 待審工項區塊
7. 明細表格
8. 單筆明細彈窗
9. 異常原因顯示
10. Z 分數顯示
11. 系統建議說明
12. 人工推翻異常判定

目前功能定位：

> 整張標單上傳後，針對每一筆工項進行單價異常偵測與人工複核。

---

## 2.2 工程估算／用量率概算

功能說明：

使用者輸入工程條件與樓地板面積後，系統根據歷史資料或用量率規則，估算各工項的合理數量。

目前估算邏輯核心為：


預估數量 = 用量率 × 樓地板面積

目前已完成：

建築類型篩選
結構類型篩選
歷史年度篩選
地區篩選
樓地板面積輸入
各工項平均值
各工項中位數
預估數量
建議範圍
最大值
最小值
樣本數
匯出報表按鈕

目前功能定位：

根據工程條件與樓地板面積，產生一份工程概算基準表。

注意：

目前的工程估算屬於概算，不是從圖面直接算量，也不代表正式工程數量。

##2.3 決標回寫

功能說明：

使用者上傳最終決標標單後，系統會解析可對應的工項，並將資料寫入歷史資料來源，供後續異常偵測與估算使用。

目前已完成：

最終決標標單上傳
Excel 解析
已對應工項寫入歷史資料
未對應工項進入待處理
字典學習基礎流程
決標資料回寫流程

目前功能定位：

將決標後的實際資料累積成歷史資料，提供下次估算、價格檢核與行情判斷使用。

##2.4 建案列表與相似建案

功能說明：

系統可以顯示建案列表，並根據建案條件查詢相似工程，提供估算與比對參考。

目前已完成：

建案列表
建案明細
相似建案查詢
依工程條件進行參考案例查詢
##2.5 參數管理

功能說明：

提供估算規則或用量率規則的管理介面，供後續工程估算使用。

目前已完成：

規則列表
新增規則
編輯規則
查詢規則

目前功能定位：

維護工程估算所需的用量率或規則參數。

#3. 整合主系統的核心原則

主系統目前已經有自己的資料模型，包含：

Projects
EstimateLists
QuoteItems
WorkItems
MatchingRecords
Materials
WorkItemMaterials
Keywords
ConstructionCategories
ConstructionMethods
LocationTypes
SurfaceTypes

因此整合時不建議直接把本專案的資料表全部搬進主系統。

建議整合原則：

1. 共用主系統既有核心資料表
2. 不建立第二套 Project 資料
3. 不建立第二套 WorkItem / Material 主檔
4. 不直接覆蓋主系統 Layout
5. 只新增主系統缺少的估算、異常偵測、操作紀錄相關資料表

#4. 主系統資料庫與本專案資料庫對應

##4.1 本專案資料表對應建議
本專案資料表	主系統建議對應	整合方式	備註
project_master	Projects	改用主系統 Projects	不建議保留第二套 project_master
material_master	WorkItems / Materials	需重新對應	本專案 material_master 可能同時包含標準工項與材料概念
historical_transaction	EstimateLists + QuoteItems	改用主系統標單資料流	決標資料建議以 SourceType = FinalBid 儲存
item_dictionary	Keywords	合併	建議 keyword 對應 WorkItemID
unmapped_items	QuoteItems + MatchingRecords	可由未對應狀態表示	不一定需要獨立資料表
quantity_rule_base	WorkItemUsageRates	建議新增表	主系統目前未必有用量率規則
project_estimation	EstimationRuns / EstimationResults	建議新增表	儲存估算批次與估算結果
audit_log	AuditLogs	新增或合併	若主系統已有 Log 表，則改接主系統 Log

##4.2 整合結論

不建議直接搬移本專案所有資料表。

建議保留主系統既有資料表：

Projects
EstimateLists
QuoteItems
WorkItems
MatchingRecords
Materials
WorkItemMaterials
Keywords
ConstructionCategories
ConstructionMethods
LocationTypes
SurfaceTypes

建議新增資料表：

AnomalyDetectionRuns
AnomalyDetectionResults
EstimationRuns
EstimationResults
WorkItemUsageRates
AuditLogs
#5. 建議新增資料表

以下為邏輯設計，實際欄位型別與命名方式可依主系統資料庫規範調整。

##5.1 AnomalyDetectionRuns

用途：

紀錄每一次標單異常偵測批次。

建議欄位：

欄位	說明
RunID	異常偵測批次 ID
EstimateListID	對應主系統 EstimateLists
RunTime	執行時間
RunType	偵測類型，例如 Price / Quantity / Both
Status	狀態，例如 Completed / Failed
CreatedBy	執行者
Note	備註

關聯：

EstimateListID → EstimateLists

##5.2 AnomalyDetectionResults

用途：

紀錄每一次異常偵測後，每一筆標單項目的檢測結果。

建議欄位：

欄位	說明
ResultID	異常偵測結果 ID
RunID	對應 AnomalyDetectionRuns
QuoteItemID	對應主系統 QuoteItems
WorkItemID	對應主系統 WorkItems
OriginalUnitPrice	原始標單單價
SuggestedUnitPrice	系統建議單價
PriceDiffPercent	單價差異百分比
PriceZScore	單價 Z 分數
PriceStatus	價格狀態，例如 Normal / Warning / Abnormal
OriginalQuantity	原始標單數量
SuggestedQuantity	系統建議數量
QuantityDiffPercent	數量差異百分比
QuantityStatus	數量狀態
AmountImpact	金額影響
OverallRisk	整體風險，例如 Low / Medium / High
Reason	異常原因
ManualStatus	人工判定狀態
ManualNote	人工備註
ReviewedBy	複核者
ReviewedAt	複核時間

關聯：

RunID → AnomalyDetectionRuns
QuoteItemID → QuoteItems
WorkItemID → WorkItems

使用功能：

標單異常偵測頁
異常摘要
異常排序
明細彈窗
人工推翻

##5.3 EstimationRuns

用途：

紀錄每一次工程估算批次。

建議欄位：

欄位	說明
EstimationRunID	工程估算批次 ID
ProjectID	對應主系統 Projects
BuildingType	建築類型
StructureType	結構類型
Location	地區
FloorArea	樓地板面積
YearFrom	歷史資料起始年度
YearTo	歷史資料結束年度
CreatedAt	建立時間
CreatedBy	建立者

關聯：

ProjectID → Projects

##5.4 EstimationResults

用途：

紀錄每次工程估算產生的工項估算結果。

建議欄位：

欄位	說明
EstimationResultID	估算結果 ID
EstimationRunID	對應 EstimationRuns
WorkItemID	對應主系統 WorkItems
AverageUsageRate	平均用量率
MedianUsageRate	中位用量率
SuggestedQuantity	預估數量
SuggestedQuantityLow	預估數量下限
SuggestedQuantityHigh	預估數量上限
Unit	單位
SampleCount	樣本數
ConfidenceLevel	信心水準

關聯：

EstimationRunID → EstimationRuns
WorkItemID → WorkItems

使用功能：

工程估算頁
估算結果表
匯出估算報表

##5.5 WorkItemUsageRates

用途：

儲存標準工項的用量率規則，供工程估算使用。

建議欄位：

欄位	說明
UsageRateID	用量率 ID
WorkItemID	對應主系統 WorkItems
ProjectType	工程類型
StructureType	結構類型
Location	地區
UsageRate	用量率
Unit	單位
LowRatio	下限比例
HighRatio	上限比例
SourceType	來源，例如 Historical / ManualRule
SampleCount	樣本數
EffectiveDate	生效日期
IsActive	是否啟用
CreatedAt	建立時間

關聯：

WorkItemID → WorkItems

使用功能：

工程估算
參數管理
用量率規則維護

##5.6 AuditLogs

用途：

紀錄重要操作，方便後續查核。

建議欄位：

欄位	說明
LogID	操作紀錄 ID
UserID	操作者 ID
ActionType	操作類型
TargetTable	目標資料表
TargetID	目標資料 ID
OldValue	修改前資料
NewValue	修改後資料
Description	操作說明
CreatedAt	建立時間

建議紀錄事件：

1. 標單異常偵測執行
2. 工程估算執行
3. 決標回寫
4. 異常人工推翻
5. 參數新增
6. 參數修改
7. 字典新增
8. 未對應工項處理

#6. 建議資料流程

##6.1 標單異常偵測資料流
Projects
  ↓
EstimateLists
  ↓
QuoteItems
  ↓
MatchingRecords
  ↓
WorkItems
  ↓
AnomalyDetectionRuns
  ↓
AnomalyDetectionResults

流程說明：

1. 使用者上傳 Excel 標單
2. 系統建立 EstimateList
3. Excel 每一列寫入 QuoteItems
4. 系統透過 Keywords / MatchingRecords 對應 WorkItems
5. 系統執行異常偵測
6. 建立 AnomalyDetectionRuns
7. 每一筆檢測結果寫入 AnomalyDetectionResults
8. 前端顯示摘要、異常排序、明細與人工推翻

##6.2 工程估算資料流
Projects
  ↓
EstimationRuns
  ↓
WorkItemUsageRates
  ↓
EstimationResults

流程說明：

1. 使用者輸入工程條件
2. 使用者輸入樓地板面積
3. 系統建立 EstimationRun
4. 系統根據 WorkItemUsageRates 或歷史資料計算用量率
5. 系統產生 EstimationResults
6. 前端顯示預估數量、合理範圍、樣本數與信心水準

##6.3 決標回寫資料流
Projects
  ↓
EstimateLists(SourceType = FinalBid)
  ↓
QuoteItems
  ↓
MatchingRecords

流程說明：

1. 使用者上傳最終決標標單
2. 系統建立 EstimateList
3. EstimateList 的 SourceType 建議設為 FinalBid
4. Excel 每一列寫入 QuoteItems
5. 可對應工項建立 MatchingRecords
6. 未對應工項保留待處理狀態
7. 此批 QuoteItems 後續可作為歷史價格與用量來源

#7. 程式碼位置與功能對應

目前專案主要檔案位置如下。

##7.1 標單異常偵測

相關檔案：

Controllers/HomeController.cs
Views/Home/Index.cshtml
Models/TenderAnomalyDto.cs
Services/TenderExcelParser.cs

目前功能：

1. 上傳 Excel 標單
2. 解析工項、規格、單位、數量、單價
3. 計算建議單價
4. 判斷異常
5. 顯示摘要統計
6. 顯示異常排序
7. 查看單筆明細
8. 人工推翻異常判定

整合建議：

建議從 HomeController 拆出 AnomalyController。
資料來源需改接主系統 EstimateLists、QuoteItems、WorkItems、MatchingRecords。
結果建議寫入 AnomalyDetectionRuns、AnomalyDetectionResults。

##7.2 工程估算

相關檔案：

Controllers/EstimationController.cs
Views/Home/EstimationUI.cshtml
Models/EstimationDto.cs
Models/QuantityRuleBase.cs

目前功能：

1. 選擇建築類型
2. 選擇結構類型
3. 選擇地區
4. 選擇歷史年度
5. 輸入樓地板面積
6. 根據用量率產生預估數量
7. 匯出報表

整合建議：

保留 EstimationController。
資料來源需改接主系統 Projects、WorkItems、WorkItemUsageRates。
估算結果建議寫入 EstimationRuns、EstimationResults。

##7.3 決標回寫

相關檔案：

Controllers/HomeController.cs
Views/Home/UploadFinalBid.cshtml
Models/HistoricalTransaction.cs
Models/ItemDictionary.cs
Models/UnmappedItem.cs

目前功能：

1. 上傳最終決標標單
2. 解析標單內容
3. 已對應工項寫入歷史資料
4. 未對應工項進入待處理資料
5. 字典學習

整合建議：

建議拆成 FinalBidController。
historical_transaction 不建議原封不動搬入主系統。
建議改用 EstimateLists + QuoteItems，並用 SourceType = FinalBid 表示決標資料。

##7.4 建案列表與相似建案

相關檔案：

Views/Home/ProjectList.cshtml
Views/Home/ProjectDetail.cshtml
Views/Home/SimilarProjects.cshtml
Models/ProjectMaster.cs

整合建議：

改接主系統 Projects。
不建議保留 project_master。

##7.5 參數管理

相關檔案：

Controllers/QuantityRuleController.cs
Views/QuantityRule/Index.cshtml
Views/QuantityRule/Create.cshtml
Views/QuantityRule/Edit.cshtml
Models/QuantityRuleBase.cs

整合建議：

quantity_rule_base 建議改為 WorkItemUsageRates。
參數管理頁面後續可改為 EstimationRules / UsageRates 管理。

##7.6 DbContext

相關檔案：

Data/ApplicationDbContext.cs

整合建議：

不要直接覆蓋主系統 DbContext。
應該將需要的 DbSet 加入主系統既有 DbContext。

##7.7 Layout

相關檔案：

Views/Shared/_Layout.cshtml
Views/Shared/_Layout.cshtml.css

整合建議：

不要直接覆蓋主系統 Layout。
應該保留主系統 Layout，再將本功能入口加入主系統導覽列。
若本專案有獨立 CSS，建議抽成單獨 css 檔。
