# 真實資料與真人研究驗收

1. 保管員在私有工作目錄執行 `python intake.py verify-source ORIGINAL ORIGINAL/manifest.json`。manifest 需有相對路徑對 SHA-256 的 `file_sha256` 或 `files` 字典。若原報表的 manifest 版型不同，先核對原規格，再將檔名與既有雜湊轉列，保留原件。
2. 複製 templates 內 cases.csv、lines.csv 為校核後的工作表。只納入可匿名使用的列。編製材料／單位碼及來源／家族鍵，審核幣別與原始文字，確定可合法使用。split 為 train、validation、blind。整個來源及同家族留在同 split，分批凍結時讓所有舊組最晚資料可用日早於新組最早估算日。未明日期不入包。
3. 以私有環境變數 RESEARCH_ANON_KEY 設定至少 32 字元的秘密，執行 `python intake.py freeze PRIVATE_INPUT NEW_FROZEN`，再執行 `python intake.py verify NEW_FROZEN`。核對源 SHA、模型輸入與私有答案。只將 model 子目錄交模型組，private 子目錄及 manifest 留保管員的受限空間。秘密不進證據。去識別僅涵蓋欄位白名單及 HMAC ID，工項碼及組合日期由保管員審核再分享。
4. 工程師用兩份 engineer-review.csv 獨立填寫，值是協議的無空白代碼，classification 是 normal、anomaly、inconclusive。保管員執行 `python intake.py reconcile left.csv right.csv out`，分歧由指定裁決人填 decisions.csv，再用新目錄及 `--decisions decisions.csv`。分歧未全裁決時不輸出正式答案。解盲前另核對兩人均涵蓋私有 blind_final_bid.csv 的全部案例／列鍵。現行工具不產生標籤、不替代工程判定。
5. T03 由真人於不同匹配工料表操作，填入 crossover.csv。執行 `python analyze_human_crossover.py observations.csv NEW_OUT`。分析器檢查 AB／BA 順序、時間、輸出品質和配對；零筆回 NOT RUN。

本工具是凍結、分割驗證與盲測交付入口。真實表格欄位轉碼、試驗預先登記及實際推論需以取得的資料及核准規範進行。合成 replay 的 SHA 鎖定條件保留。

若要依預先登記日期自動分割，先將 split 填 auto，執行 `python intake.py assign PRIVATE_INPUT NEW_ASSIGNED YYYY-MM-DD YYYY-MM-DD`，最後兩日期依序為 validation、blind 起日。跨來源、同家族跨組及資料晚於估算日會被拒絕。拒絕後不要沿用該次輸出。模型輸入保留面積、樓層及同鍵位置的 HMAC；行資料須補規格碼。
