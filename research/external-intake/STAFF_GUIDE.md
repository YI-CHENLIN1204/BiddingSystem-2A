# 崴正營造 2A 外部研究作業包

## 資料保管員

取得書面授權、正式住宅快照原始 CSV、原 manifest 與最終決標檔。保留原件唯讀。最少交付 build 式樣見 templates/cases.csv、templates/lines.csv 與 final-award-map.json。每案例填原檔相對路徑、核准版本、原 SHA-256、授權碼、首次可用日、幣別、明細工作表清單與 header_row。CSV 用 ["csv"]，Excel 用實際欄名及指定分頁。原始清單及欄位碼表只留私有空間。確切日期與罕見案例組合也需授權審閱。

決標單價、數量、複價分欄。單位只接受核定映射，不換算；其核定規格必須指定可接受單位碼。未定價、公式、複價不符、零負或未知規格直接拒絕整批，保管員另建核准的 literal 副本並保留原檔與來源對應。絕不以複價倒算缺少的決標單價。版本號與日期須由保管員核證。工具拒絕同檔 SHA、相同正規化內容、同案多版本、跨來源／系列的原有切分衝突。近重複修訂仍需以同一 case_id／family_id 註冊，人工比對專案、標案版本、別名與日期。禁止把外部檔案提交 repo、上傳 GitHub CI 或匯入正式 Supabase。

~~~text
python -m pip install -r requirements.txt
python intake.py verify-source ORIGINAL ORIGINAL/manifest.json
python import_awards.py PRIVATE_SOURCE PRIVATE_AWARD_MAP.json NEW_MAPPED
python intake.py assign NEW_MAPPED NEW_ASSIGNED VALIDATION_START BLIND_START
python intake.py freeze NEW_ASSIGNED NEW_FROZEN
python intake.py verify NEW_FROZEN
~~~

若 split 事先已登記，略過 assign，freeze 直接指向 NEW_MAPPED。assign 的輸入 split 要填 auto。使用私有環境變數 RESEARCH_ANON_KEY，由保管員管理。PRIVATE_line_provenance.csv 含授權碼、分頁、列號與原 SHA，只留受限空間。import-receipt.json 可供內部查核計數與 SHA，對外發送前仍需資料管理人核准。模型組只取 NEW_FROZEN/model。現行 targets 只有可分享的建案欄位。逐工項模型若需要估算日前的 BOQ，另提供經授權、獨立雜湊封存的估算時點快照，再以保管員對照表接上匿名案例／行鍵。禁止把最終決標資料複製成模型輸入。

## 工程師獨立盲標

保管員把相同的匿名案例／行鍵、工項內容、規格和核准的工程參考交給兩名工程師，各填一份 engineer-blind.csv。先核定 estimable、材料、規格、單位、量與價狀態、是否應棄答及理由碼。eligibility 用 yes/no/unknown，量價分別使用 normal/anomaly/inconclusive，無可核定材料或規格填 NA，保留 reason_code，例如 UNIT_UNKNOWN、SCOPE_EXCLUDED、REFERENCE_INSUFFICIENT。兩人不得看模型輸出或彼此答案。需要時由第三人裁決；保留原件，分歧未結案前勿輸出正式真值。

在預測組看不到真值時，先封存模型／參數、資料 SHA 與 predictions-sealed.csv 的 SHA，預測應保留所有行含棄答。候選以 | 分隔，最多五個；無候選或不自動對應用 NA。解盲後由保管員計算材料碼比對、Top-K、量價混淆計數、Precision、Recall、FPR、棄答與人工覆核時間／更正數。空分母為 null。候選命中表示真值材料碼在指定順位，規格／單位是否亦相符仍需角色核對。覆核秒數及更正數使用實際的覆核紀錄，不以零填未測。

~~~text
python engineer_review.py reconcile NEW_FROZEN/private/blind_final_bid.csv REVIEWER_1.csv REVIEWER_2.csv NEW_CONFLICTS
python engineer_review.py reconcile NEW_FROZEN/private/blind_final_bid.csv REVIEWER_1.csv REVIEWER_2.csv NEW_GOLD --decisions DECISIONS.csv
python engineer_review.py score NEW_GOLD/adjudicated.csv SEALED_PREDICTIONS.csv NEW_METRICS --prediction-sha256 PRESEALED_SHA256
~~~

## T03 真人 AB／BA

A 為人工查碼與人工標註，B 為系統建議後逐項覆核與修正。兩張 167 列既有合成練習表只能演練，見證據 ZIP 的 t03-practice 對應檔。正式測量使用核准的兩份不重複、規模與難度相近的標單，由保管員凍結 SHA 和品質基準。以匿名操作員分配 AB 或 BA，每一半用相反序列，各人做兩個不同標單，交錯標單與流程，避免工作表固定綁定某一流程。每段使用相同設備與規則，第二段前清空狀態，受測者不得共享。受測同意及真實識別碼留私有空間。

助理每一階段填 t03-events.csv。phase 是 setup、mapping、review、error_detection、error_correction、maintenance；event 是 start、pause、resume、end。時間使用帶時區 ISO 8601，notes 用不含個資的簡碼，無備註填 NA。暫停時間不計，填 crossover.csv 的分段秒數與加總。記錄總工項、正確接受、錯誤自動接受、未解決、修正耗時及獨立覆核雜湊。品質不合格仍保留原始紀錄。分析輸出 paired.csv、summary.json，依品質門檻計算配對時間、序列及準確率，樣本不足時保留缺值。真人仍為 NOT RUN。

~~~text
python check_crossover_events.py OBSERVED_EVENTS.csv HUMAN_OBSERVATIONS.csv
python analyze_human_crossover.py HUMAN_OBSERVATIONS.csv NEW_T03_RESULT
~~~

正式結案使用專案驗收矩陣。若要獨立重現 B1，須取得原作者完整實作與未公開參數；目前研究保持獨立重建。已凍結 T05 門檻與採用未過結論維持。
