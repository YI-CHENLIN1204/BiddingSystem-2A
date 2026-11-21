# 畢業專題 2026-11-25 測試資料與精準度交付清單

> 狀態：2026-11-21 盤點；以 GitHub main / feature 分支程式為準。本文件是執行清單，不是已通過的準確度驗證報告。

## 交付分層（不能混淆）
| 資料等級 | 來源 | 能證明 | 不能證明 |
|---|---|---|---|
| A：合法取得且可核驗的真實建案 | 取得授權的完成價量明細、建案條件與確定階段 | 工程現場可用性、真實精度（在獨立測試建案上） | 未有來源／階段證據的數值不能代表最終決標或實作 |
| B：可核驗的公開招標／預算標單 | 政府機關公告與可下載的原始文檔 | 工項名稱、分類、欄位及格式真實性；部分預算數量／價格 | 不一定是得標契約單價，不一定是最終實作數量 |
| C：根據文獻與工程條件生成的合成測試 | tools/graduation-synthetic，標明 SYNTHETIC | 上傳、解析、多表選取、對應、程式回歸及受控模型比較 | 真實工程估算精準度，不得冠以企業實測 |

## 已找到、待逐案授權／下載及解析的公開來源
1. [工程會：工程價格資料庫作業辦法](https://www.pcc.gov.tw/content/index?eid=1372&lang=1&type=C)：工程資料類型規範，分契約單價／預算單價／詳細價目／單價分析／資源統計。
2. [水利署：工程採購投標須知](https://www.wra.gov.tw/wralaw/Law_Content.aspx?n=9329&s=12930&sms=9191)：電子詳細價目可為 PCCES 或 Excel，資料版本可能因公告而更新。
3. [雲林縣學校工程預算審查規定](https://law.yunlin.gov.tw/LawContent.aspx?id=GL000282)：預算總表、詳細價目、單價分析、工程數量計算表四種不同來源，不應混用。
4. [臺南市政府工務局大宗資材參考價表公開搜尋](https://www.tainan.gov.tw/)：可作價格級距參考；具體期數、版次、檔案與授權仍須逐份取得並記錄。不可直接當成交價。
5. [國立中興大學總務處營繕組：工程預算書範本（XLS/ODS）](https://oga.nchu.edu.tw/download2class/id/62/mid/73)：可下載官方空白預算表，供欄位／版型測試；**範本沒有真實工程逐案成交數量，不是精準度真值**。
6. [臺中市住宅發展工程處：總標單公開範本](https://thd.taichung.gov.tw/981499/981563/981580/1374224/1374229/2065448)：總標單不同計價方法的公開範本，可用於文件分類／版型對照，非工程明細量價真值。

## 畢專資料結構與檢測範圍
- 類別：假設／安全、整地／開挖／回填、基礎、結構 RC/SC、模板、泥作、防水、屋頂、外牆、門窗、金屬、室內裝修、油漆、景觀、電氣、給排水、消防、空調、弱電、電梯與附屬設備。
- 每筆保留資料來源及狀態（SYNTHETIC、PUBLIC_BUDGET、PUBLIC_CONTRACT、AUTHORIZED_REAL），也保留 project_id、sheet、row、item_code、name、spec、unit、qty、unit_price、amount、工程類別、資料缺漏與警示。
- 建案特徵至少保留 project_type、structure_type、city、floor_area、floor_count、basement_count、date；新增更多工程特徵需確認來源，不可推定已有基地面積或地質。
- 全部不規則或缺欄位資料，必須以標準答案 JSON 逐列檢驗，**HTTP 200 和 360 筆生成完成不等於解析正確**。

## 2026-11-25 可重現指令（不寫資料庫）
使用者若仍在 `feature/1d-api-integration`，可僅同步 tools 目錄，不覆蓋尚未提交的前端修改。

```powershell
git fetch origin main
git restore --source=origin/main --worktree -- tools/graduation-synthetic
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
python tools/graduation-synthetic/generate_irregular_excel.py --source .\graduation-data --output .\graduation-irregular-v05.xlsx --truth .\graduation-irregular-v05-truth.json --items 360
python tools/graduation-synthetic/evaluate_synthetic_holdout.py --source .\graduation-data --output .\graduation-synthetic-holdout-v01.json --top-k 8 --min-sample 5
```

若 `graduation-data` 尚不存在，先使用 `python tools/graduation-synthetic/generate.py --count 300 --seed 20261119 --output .\graduation-data`。輸出檔若存在會拒絕覆寫；換新的檔名，不刪除未知檔案。

## 精準度實驗設計
- **訓練與驗證按建案切分**；既有生成資料使用 `train` 與 `test`。同一建案不同工作表、不同工項絕不可跨訓練與測試。
- 模型 A：相似案件＋時間權重＋P15/P50/P85 加權分位數。
- 模型 B：**相同來源與篩選條件**，用加權平均／變異數擬合常態分布，計算相同分位數作基準。
- 指標：每個材料/偵錯細項的 P50 絕對百分比誤差（MAPE；數量為零需獨立處理）、P15-P85 覆蓋率、平均區間寬、70% 區間評分（interval score）、有效樣本數，以及無法產生估算的比例。
- 嚴格區分：`evaluate_synthetic_holdout.py` **只是離線代理估算程式**，其相似度函數不等同完整 C# `ProjectSimilarityService`，只可用於合成數據的流程健檢，**不得在教授簡報稱為系統的真實精準度**。
- 真實精準度至少需多個有來源與時間證據的獨立實際建案，加上對應歷史材料數量；不足時報告「無足夠樣本，無法估計外部準確度」，不能自行創造數字。
- 真實資料不可加入 GitHub；僅離線保留來源 manifest 與去識別化統計。

## 已完成與待驗收
- v0.4 既有 360 工項、7 工作表、Windows 8/8 PASS；但此結果早於 v0.5 的工項與格式修正，**不可直接當新版本 PASS**。
- 2026-11-21 修改 Excel 欄寬、列高及自動換行；前端 `feature/1d-api-integration` 的 `WorkbookSheetPicker.vue` 也修正工作表名稱、工項／規格、警示欄的換行與水平捲動。
- v0.5 新增依有無地下室限制工程工項與樓層代碼；加入合成 holdout 分位數與常態對照，**實際 Windows 重跑、實際建案精度、C# API E2E 尚待驗證**。
- 下個步驟：取得合法的原始公共工程與至少少量具建案特徵的匿名化企業檔案，建立 source manifest，校準分類與比例，再做真實保留樣本估算。
