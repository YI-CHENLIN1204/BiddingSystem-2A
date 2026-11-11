---
name: waizheng-2a-handoff-guardian
description: "崴正營造 BiddingSystem-2A 專屬交接守門 Skill。凡在此 repo 修改程式、模型、研究、測試、設定、DB、API、UI 或文件，必須先核對最新程式及交接基線，並在每次更改後審查、記錄是否需同步更新交接文件。適用跨 ChatGPT 對話接手、研究回溯與版本治理。"
---

# Waizheng 2A Handoff Guardian — 崴正營造 2A 交接同步守門

## 適用範圍與觸發規則

**僅適用** Repository `YI-CHENLIN1204/BiddingSystem-2A`，開發分支 `feature/1d-api-integration`。凡用戶要求繼續開發、修正、改參數、做研究、擴充測試、提交 GitHub、同步交接、轉移對話、記錄決策或部署，都應依本 Skill 執行。**每做一次變更，都重做一次交接影響審查，不能因為「只是小修改」省略。**

> 重要：本 Skill 是 **GitHub repo 的版本化操作規則**，不等於在每個 ChatGPT 對話都自動全域安裝；新對話須先讀此檔與 `AGENTS.md`。只靠文字承諾無法保證任何外部代理一定遵循，配套提供可執行的變更檢查腳本及 PR CI gate。

## 開始工作前（必做）

1. 確認 GitHub 專案、分支、工作樹：`git status`、`git branch --show-current`。不要假設文件一定最新或本機沒有未提交變更。
2. **優先**閱讀：
   - `docs/CURRENT_HANDOFF_2026-11-11.md`（目前主交接）
   - `docs/RESEARCH_TRACEABILITY_2026-11-11.md`（Git SHA、實驗與證據等級）
   - `docs/NEW_CHAT_START_2026-11-11.md`（新對話入口）
   - `docs/DECISIONS.md`、`docs/WORKLOG.md`、`README.md`
   - 模型研究時另讀 `docs/2A_MODEL_AUDIT_2026-11-10.md` 與 `research/2a_synthetic/README.md`
3. 對照 **最新程式碼與實際可讀取資料**，過時文件（特別是 2026-10-15 的歷史交接）不能覆蓋現況。沒有真實證據時標記待驗證。
4. 禁止無證據重寫核心權重、混入模擬資料、改 `main`、覆蓋用戶未提交檔、清除 DB 測試資料；嚴禁提交密碼。

## 每一次變更的硬性循環（CHANGE → REVIEW → RECORD → VERIFY）

**每次更動都必須實際走完以下步驟。** 即使判斷「交接內容不用改」，也要留下審查記錄。

### A. 改動前：建立變更基線
- 記錄基準 SHA `git rev-parse HEAD`；列出預計修改檔與原狀。
- 明確說明這次改動是正式 2A 程式、研究 Sandbox、文件、設定或資料模型；測試環境與正式環境的資料責任不能混淆。

### B. 改動後：逐項問「交接必須更新嗎？」
逐條比對：

| 檢查面向 | 若受影響，需要檢查／更新 |
|---|---|
| 架構、資料流、功能適用範圍、權限、介面操作 | `docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/2A_CURRENT_ARCHITECTURE.md`、`docs/HANDOFF.md`（只在有必要且不覆蓋歷史記載時） |
| 演算法、權重、TopK、門檻、分布、信心度、ESS、實驗設計 | `docs/CURRENT_HANDOFF_2026-11-11.md`、`docs/2A_MODEL_AUDIT_2026-11-10.md`、`research/2a_synthetic/README.md` |
| DB schema、來源、資料品質、模擬／正式隔離 | `docs/DATABASE.md`、`docs/DECISIONS.md`、主交接 |
| API、測試命令、建置／部署環境 | `docs/API.md`、`docs/DEVELOPMENT.md`、`docs/LOCAL_TEST_GUIDE.md`、主交接 |
| 使用者決策、研究方向、採用／停止條件 | `docs/DECISIONS.md`、主交接 |
| Git 提交、研究回合、實驗輸出／失敗、測試證據 | **`docs/RESEARCH_TRACEABILITY_2026-11-11.md`**、**`docs/WORKLOG.md`** |
| 新對話接手時「第一件要做什麼」已變 | `docs/NEW_CHAT_START_2026-11-11.md` 與索引 `docs/README.md` |

**判定原則：**
- 會使新對話的「目前狀態、資料責任、測試通過與否、下個任務」與現況不同 → **必須更新對應交接文件**。
- 純重排、註解、無行為變化、文件拼字 → 可以判定交接內容不必改，**但仍須記錄審查理由**。
- 涉及行為、模型結論、資料來源、功能接口、參數或研究里程碑 → **通常至少需要更新主交接／研究回溯／WORKLOG 中相關部分**；不要只在 commit message 註記。
- 每輪保持一個簡短的 **`docs/HANDOFF_CHANGE_REVIEWS.jsonl`** 審查紀錄；資料格式見下節。

### C. 審查記錄格式（必填）
每完成一批功能／研究／文件修改，新增一行 JSON 至 `docs/HANDOFF_CHANGE_REVIEWS.jsonl`：

```json
{"date":"2026-11-11","change_id":"2a-handoff-guardian-bootstrap","summary":"新增專案 Skill 與交接檢查門檻","changed_paths":["AGENTS.md",".agents/skills/waizheng-2a-handoff-guardian/SKILL.md"],"handoff_update_required":true,"updated_documents":["docs/CURRENT_HANDOFF_2026-11-11.md","docs/RESEARCH_TRACEABILITY_2026-11-11.md"],"reason":"新對話交接流程與開發操作規範有實質調整","verification":"提交後檢查檔案及腳本；CI 是否執行另行確認"}
```

**記錄要求：**
- `changed_paths` 必須涵蓋這輪所有變更檔（檢查紀錄自身可省略）。
- `handoff_update_required=true` 時 `updated_documents` 不得空，且須列出**本輪真的修改的交接文件**。
- `handoff_update_required=false` 時 `updated_documents=[]`，`reason` 必須具體說明「為何沒有影響交接」。
- `verification` 區分「只提交」「使用者口頭無錯」「完整終端通過」「真實工程驗證」。
- 已提交 commit SHA 若尚未取得，勿先填假的 SHA；交接文件可透過 `git log` 與明確的 `change_id` 回查。
- 不得刪除舊行或把失敗紀錄移除。

### D. 最終確認
- 執行單元測試、編譯或最低相關驗證；只標示真正跑過的測試。
- 執行（本地）：
  ```powershell
  python scripts/check_handoff_review.py --base HEAD~1
  ```
  以上比較範圍是**當前 HEAD 與前一 commit**，適合單一整合提交。若正在改多個 commit，應改用本輪開始前的基線 SHA；**不要照抄 HEAD~1 遮掉其他改動**。
- 用戶若要求轉新對話：先確認新交接讀入點與研究輸出是否被 `.gitignore` 排除。需分析 CSV 時明確告知用戶要上傳哪幾份。
- 回報必須包含變更摘要、最新 Git SHA、實際測試、**交接審查結論（更新／無需更新）**及下次優先工作。

## 研究真實性底線
- 產學版：僅經確認真實與完整的已核准決標標單，才應成為正式模型基準；目前 Demo Approved 是已知例外風險。
- 專題版：可使用假設的模擬資料做 Monte Carlo、演算法敏感度與分布比較，**絕不可聲稱模擬 MAPE 等於真實工程準確率**。
- 外部開源資料：先核實合法取得、授權、材料與建案範圍、結構、時間及計量單位；不得當成崴正自己的決標歷史。
- 任何百分位區間、ESS、相似度、CV 都不等於未校準的「93% 判定正確率」。

## 新對話必用的接手策略
1. 先要求／執行讀取本 Skill、`AGENTS.md`、主交接與研究回溯表。
2. 檢查最新 `git log`、`git status` 與 `docs/HANDOFF_CHANGE_REVIEWS.jsonl` 尾端審查結果。
3. 以當前程式與使用者最新測試覆蓋過時的文件段落；遇衝突先寫明。
4. 繼續下一個**尚未完成**的研究／開發任務，禁止重新發明已完成的功能。
5. 每輪遵循 A→B→C→D；**永遠在做完任何更改後重新問一次「交接文件要更新嗎？」**
