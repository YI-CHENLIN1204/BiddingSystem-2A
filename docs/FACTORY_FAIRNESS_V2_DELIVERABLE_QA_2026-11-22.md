# Factory Fairness V2：研究數字與交付檔案結構核驗

日期：2026-11-22。驗收識別：`factory-fairness-v2-deliverable-qa-20261122`。核對 GitHub 研究分支 commit `d2a71c91cc6420717b645732bd4f37ba9b09135c`，不改任何實驗數據或正式環境。

## 可核對已完成

- 研究分支 `research/kde-comparison-20261122` 最新基準 `d2a71c9`。
- `fairness-v2/Factory-Fairness-Report-ZH.md`、`Factory-Fairness-Report-ZH.docx`、`manifest.json`、`equal_sample_summary.csv`、`equal_sample_bootstrap.csv`、`e3_summary.csv` 實際存在。
- GitHub E2 `replay_factory_equal_n.py` 包含：train 126、只用目標開工日之前案例、K5/K8、同目標及truth、各自K筆歷史案且在paired時 assert兩法 n=K、相同 scope/spec/unit/work_scope、樓地板面積的共同口徑；E2 限合成資料及已檢視 test 的探索性比較。既有摘要 K5 70% 金額 n=18、A0 score0.564122789、B1 score1.521466892；K8 金額 n=15、A0 0.457047317、B1 1.585574600。K5混凝土/鋼筋 n各6，K8各1，無穩健總勝者。
- E1 A0 vs A1 70% score 0.5302→0.5026，改善點估約5.2%；建案bootstrap CI跨0，時點限制結果改善約3.1%。因此正式A0目前保留是**部署風險控管**，不能說實際獨立工程已確認勝出。
- `equal_sample_per_case.csv`、`e3_per_case.csv`、`dictionary_material_per_case.csv` 可在 GitHub `fairness-v2/Factory-Fairness-V2-Raw.zip` 的 ZIP 中央目錄找到；ZIP含**三份CSV且僅此三份**。GitHub這份壓縮檔並**非**使用者口頭提到的 `Factory-Fairness-V2-Evidence.zip`，後者本輪未找到 GitHub 同路徑檔案。
- GitHub `fairness-v2` 目錄可確認 **14 張 SVG**，0張直接列出的PNG；`manifest.json` 雖記錄多張PNG檔的SHA256，**不等於PNG已提交到GitHub**。
- GitHub版本 `Factory-Fairness-Report-ZH.docx` 的 ZIP/OOXML 中央目錄包含僅三個member：`[Content_Types].xml`、`_rels/.rels`、`word/document.xml`。其中**沒有`word/media/`圖片、沒有內嵌圖表**。可以確認這是文字/表格版 DOCX，**不代表DOCX無法開啟或圖文渲染一定失敗**；因本輪未取得可在本機使用的文件副本，**未做Word分頁/字型/列印可讀性目視驗證**。
- 本輪讀取GitHub源程式、CSV彙總、manifest及二進位ZIP中央目錄核對**並非完整重跑E2模型**、無法獨立核准docx版面、也沒有執行CI/handoff gate。本輪未碰DB/API/正式UI。

## 交給 Codex 的必要收尾項目

1. **報告圖文完整性**：把主文確實引用的核心圖（A0/A1、E2 K5與K8、E3品質與可估率、字典安全、工時假設）插入正式 DOCX，加入圖號、中文完整圖說、合成/探索性/樣本數提醒；完成 Word→PDF或逐頁PNG render 檢查中文字型、表格換頁與圖片清晰度。
2. **圖檔可取用**：將 300dpi PNG 與SVG提交到研究版次可取得的圖目錄，或明確封裝於完整Evidence ZIP。不能只有manifest有SHA但下載位置找不到。PNG應含合適字體和n標示。
3. **可下載完整證據包**：如要稱 `Factory-Fairness-V2-Evidence.zip`，需實際建立並提交，至少含 Word/PDF（若有）、PNG/SVG、manifest、摘要CSV、原始逐案CSV、重現指令和README；原 `Raw.zip` 保留，不覆寫。完整證據包與單純原始三CSV壓縮檔名稱不得混淆。
4. **數字一致性和研究結論**：報告圖表從 `equal_sample_summary.csv`、`equal_sample_bootstrap.csv`、`e3_summary.csv` 直接重生；E2 K8 數量兩項各只有1個共同可估案例，不報CI/統計顯著；原版B1與B1-K控制變體分開；只有合成回測，不稱企業工程實績。
5. **研發安全與測試**：原正式 `feature/1d-api-integration`、Supabase及估算API均不得因報告補圖而改動。跑交接gate、Word逐頁外觀檢查、檔案清單和SHA，再集中提交。

## 最終分級

- **研究方法／CSV交叉核對：具可引用的探索性成果。**
- **研究演算法正式採用決策：維持A0、不自動切換，真實獨立盲測NOT RUN。**
- **教授版Word圖文與完整證據包：交付有缺口，需補圖與完整性驗收，尚非「圖文完整最終交付」。**
