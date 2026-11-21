# SRC／城市／基地面積相似度：嚴格模式驗收計畫（2026-11-21）

## 1. 適用範圍與既有成果
- 正式 2A 量價 API 僅允許「結構體工程」數量的鋼筋、混凝土及大項價格；SC 案若無鋼筋則只輸出混凝土。不得讓鋼骨、模板、土方等候選工項直接取得正式紅綠燈。
- 原有隔離畢專 DB 的 train-only 210 案、99,959 筆歷史交易及 RC SYN-00029、SC SYN-00002 實測結果必須保留；**不要重跑 GraduationSeed 或 down -v**。
- `main` 的後端部份舊於 `feature/1d-api-integration`：**目前實際使用的 feature 分支已經有 SiteAreaScore、SiteAreaWeight、SiteAreaDifferenceCutoffRatio**。不得把 main 尚未同步的特性誤當成 feature 未完成。
- 既有時間權重與相似度權重同時存在，不能稱為沒有時間加權；無 Z-score、LSTM、Razor 的現行宣稱。

## 2. 實作清單
- `main: tools/graduation-synthetic/generate_full_project_dataset.py` 加入 **opt-in** `--include-src`，只有明確啟用時才在新資料來源產生 SRC，不能影響原本固定 seed 的 RC/SC v2 資料。SRC 使用鋼骨、鋼筋、混凝土三個獨立合成材料族；數值僅為研究示例，非規範工程用量。
- `main: tools/graduation-synthetic/test_full_project_dataset.py` 加入 SRC opt-in、材料族、重現性及原資料向後相容測試。
- `feature/1d-api-integration: 崴正/Services/ProjectSimilarityService.cs` SRC 目標僅取 **SRC 歷史建案**，RC/SC 保持原選樣規則。已有 SRC 但少於最低樣本數時，必須顯示資料不足而非借 RC/SC 做 SRC 的紅綠燈。
- `feature/1d-api-integration: tools/graduation-synthetic/SimilarityContract/` 新增不需資料庫的 .NET 10 檢查工具，實際呼叫 C# 的結構、城市及基地面積相似分數函式。城市「相同才加分」，不等於城市價格倍率；基地面積的缺失欄位不能得到加分。
- 尚未增加任何候選材料至 `TwoAScopePolicy` 正式允許的數量偵錯類別，也不修改資料庫 Migration。

## 3. 驗證門檻（未通過前，不增加正式紅綠燈）
### A. SRC 基本安全與功能
1. Python 全部既有測試仍 PASS，`--include-src` 有 RC、SC、SRC，SRC 核心材料各自存在；原 v2 資料 SHA 與已建 DB 維持不變。
2. 對 SRC 目標，正式相似建案不得有 RC／SC；**若現有 DB 沒有 SRC 歷史案例，回應應表示無樣本/不足，而不是輸出借用 RC 或 SC 的 High/Low/Red**。
3. SRC 可以被上傳為 Target Pipeline，但不可以把測試案寫進訓練歷史。
4. 新 SRC 研究資料只能使用新資料夾與新隔離 DB；需單獨授權才能移除任何既有 volume。

### B. 城市、基地面積敏感度
1. 相同建案條件，只變更城市：同城市 `CityScore=CityWeight`、不同城市 `CityScore=0`，且其他分數完全不變。
2. 只變更基地面積：相同基地面積 `SiteAreaScore=SiteAreaWeight`；差 50% 且 cutoff=1.0 時得分 50%；差距達 cutoff 為 0；缺少候選基地面積為 0。
3. 實際 API 同樣需要用單因子對照案例，檢查 Top8 參考樣本與 P15/P50/P85 的變化，記錄城市/基地面積因素是否改變排序、樣本數及估計值；不得用單元測試取代真實 API 實驗。
4. 城市與基地面積若造成樣本數不足，應維持不足狀態，不得自動把不相關工程當成可信歷史。

### C. 新候選工項升格條件
1. 鋼骨（SC/SRC）、模板（需分工法）、土方（需基地與地下室/開挖條件）只做研究；建築立面、裝修選材、門窗等高度依賴設計之項目先排除。
2. 先確認單位、工項識別規則、分母（m²/m³）、設計與工法關鍵條件完整；不得跨不同單位或不同施工方法混合。
3. 每個結構/工項群組需有足夠**獨立歷史建案**並通過有效樣本數、權重集中度、期間與來源品質門檻。
4. 在 held-out 建案上批次回測 P15/P85 涵蓋率、誤差及穩定性，並有可靠的正常/異常真值標記後，才能計算假陽性/假陰性；沒有真值時不能稱為「準確率通過」。
5. 即使合成回測合格，也只能標「合成情境研究通過」，進入正式紅綠燈前需另經企業資料/專家檢核與授權；不能直接啟用。

## 4. 本機待執行測試
使用者在有 `main` synthetic 工具和 feature C# 主程式的同一工作目錄：
```powershell
python -m unittest discover -s tools/graduation-synthetic -p "test_*.py" -v
dotnet run -c Release --project .\tools\graduation-synthetic\SimilarityContract\SimilarityContract.csproj

# 全新輸出；既有 graduation-data-v2 不得覆蓋
python .\tools\graduation-synthetic\generate_full_project_dataset.py --count 60 --seed 20261119 --include-src --output .\graduation-src-research-v1
```
注意：`SimilarityContract` 只測真正的 C# 計分函式，**不是正式 API / DB 端到端驗證**；新 SRC 檔也只生成研究檔案，**不會寫入 DB**。

## 5. 現況與退回方式
- 此次 GitHub 修改已提交，但執行狀態仍以使用者實際 Windows 日誌為準；沒有日誌不能標 PASS。
- 若回歸失敗，原 RC/SC DB 與 Target Pipeline 不需刪除；程式碼由 Git commit 還原即可。沒有 DB schema 變更或 Migration。
