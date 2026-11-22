# A0 正式分位計算核對

2026-11-22。重跑第一版固定廠房池，將離線重播的參考值與權重送入正式 C# 數量與大項金額的加權分位函式。159 個案項的 P15、P50、P85 共 477 個對照，皆於絕對誤差 0.01 內通過。金額先換算每坪再回推總額。

驗證範圍為已給定參考值與權重時的分位函式。正式 API 的資料查詢、篩選、排序與權重生成仍待隔離資料庫核對。住宅原始來源和可獨立驗證的實務資料仍缺。異常真值與正式回寫仍屬未測項目。

從 repo 根目錄執行：

```powershell
python research/2a_synthetic/kde_comparison/export_a0_contract.py <source> <b1-output>
dotnet run -c Release --project tools/graduation-synthetic/QuantileContract -- <b1-output>/factory_a0_contract.json
```

## 固定候選池選案核對

固定候選池新增正式 ProjectSimilarityService.ScoreCandidates 核對。159 案項共 1,272 個參考 ID、順位與權重相符（權重絕對誤差 ≤ 1e-12），477 個分位對照再度通過。候選資格由離線快照提供，排序與權重公式由驗證程式重播；SQL、正式 API 與入庫資料範圍仍待隔離環境驗證。
