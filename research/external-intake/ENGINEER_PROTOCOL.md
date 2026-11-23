# 工程師盲標作業說明

保管員向兩名工程師各發一份相同的匿名化案件及工項，附核准參考資料與空白 [engineer-blind.csv](templates/engineer-blind.csv)。兩人分開填寫。保管員在兩份原稿封存前維持模型候選與另一位工程師答案不可見。

| 欄位 | 填寫方式 |
|---|---|
| case_id／line_id | 照發放的匿名代碼填寫，每列一次。 |
| estimable | yes、no、unknown。按工程範圍、規格、單位與可比資料核定。 |
| material_code／spec_code／unit_code | 填核定代碼。無法核定填 NA，附原因。 |
| quantity_status／price_status | 分別填 normal、anomaly、inconclusive。按核准工程參考，保留判斷依據。 |
| should_abstain／reason_code | 填 yes、no、unknown，並以代碼註記排除、規格不明、單位衝突或基準不足。 |

先由少量先導項確認用語和每列操作時間，再約定正式場次長度。每人交付一個 UTF-8 CSV 與私有判斷依據。保管員核對欄名、行數、重複、遺漏及 SHA-256。指定裁決人對每個分歧填同格式裁決檔，保留兩份原始答案與理由。完成裁決並確認預測已封存後，才揭露字典與候選結果及做第二階段人工覆核，該階段實測秒數和更正數另存。第一階段的答案保持封存。

技術人員代為核對與計分，工程師只需交付判斷。見 [輸出及解盲順序](STAFF_GUIDE.md)。
