# 研究 Actions 執行查核｜2026-11-23

[Run 45049455364](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364) 已核實，測試提交為 `b3bceb90b2d8775ba7006ac146bed3d0ecffbc27`。本次修改從 `e0109ade56e34f225efd001022ba3745bcbde838` 接續，提交訊息為 `research: enforce isolated audit roles and reconcile acceptance evidence`。

- T04 隔離 EF 換算 PASS。T07 隔離 Draft 讀取、人工映射、Publish、Reopen、重複提交與稽核核對 PASS。兩項保留原本完整驗收門檻。
- T08 隔離建案讀取 30 次，p50 19.04 ms、p95 26.33 ms。正式全流程效能仍待測。
- T09 原 Run FAIL，匿名稽核 HTTP 200。研究 runner 編譯時加入隔離授權，門檻為匿名 401／403、無效憑證 401、一般使用者 403、主管 200 且讀回 T07 稽核紀錄。修正後 Actions 結果待核實。正式角色整合仍待驗收。
- Word 渲染 job PASS，產出 8 頁 PDF 與 PNG。逐頁視覺仍待取得可開啟的頁面。下載產物後需按下列雜湊核對來源。
- 本批範圍是 `research/acceptance-ci/` 與研究交接文件。隔離 runner 使用一次性 PostgreSQL，無 Migration。下一步核實新 Run 角色矩陣並完成八頁視覺判讀。研究算法與 A0 維持既有版本。


| 產物 | GitHub SHA-256 |
|---|---|
| [隔離 API](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364/artifacts/14764044670) | `f377309bc77ed310d2a19ffe4f61db8973f33661462dce790b418d769f82c078` |
| [Word 渲染](https://github.com/YI-CHENLIN1204/BiddingSystem-2A/actions/runs/45049455364/artifacts/14764009657) | `211cd7572a9f37798e4118ce70cb8e4e36a3945cfadb354acd37101ec35d2680` |

GitHub 已核實兩件產物與相同的 head SHA。環境未能下載頁面位元組。第 1–8 頁的字型、圖表、分頁、頁邊與溢出均待視覺覆核。
