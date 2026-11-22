# 2A 廠房測試跨裝置接續

## 目前進度

- 開發分支 `feature/1d-api-integration`，本次測試基線 `d7f7eb3e868e9422dc852623789243e91ac63fdf`。
- Supabase 2A 測試專案 ref 為 `sbykuwypriudrgurmssb`。換裝置時從有權限的 Supabase Dashboard 取得同一專案的 Session Pooler 連線字串，密碼只在本機輸入。
- 批次 `FACTORY_20261122_V1_TRAIN` 已匯入。126 案、16,786 筆已對應明細可直接從雲端讀取。
- 案件 #256（SC 合成廠房-0004）在資料庫為 Draft、164 筆。使用者畫面確認鋼骨 3 筆、kg、數量與單價可見，標準材料已對應。
- 本輪實測舊工作階段 5184、5264、5263 的 health 都回 200，但新增估算 options 都回 404。先啟動新版服務再驗估算。執行工具所在程序未取得 DB 連線變數，尚無新版實際 DB 驗收。

## 新 Windows 裝置操作

安裝 Git、.NET 10 SDK、Node.js（本次工具版本為 24）。有既有工作目錄可另開一個乾淨目錄執行以下命令。

```powershell
git clone --branch feature/1d-api-integration https://github.com/YI-CHENLIN1204/BiddingSystem-2A.git BiddingSystem-2A-test
cd BiddingSystem-2A-test
git switch --detach d7f7eb3e868e9422dc852623789243e91ac63fdf
git rev-parse --short HEAD
```

終端 A 在專案根目錄啟動後端。提示時貼上連線字串，替換其中的密碼佔位符。連線字串只會放在此終端的程序環境中。

```powershell
$secret = Read-Host "Supabase PostgreSQL connection string" -AsSecureString
$env:ConnectionStrings__DefaultConnection = [System.Net.NetworkCredential]::new('', $secret).Password
Remove-Variable secret
dotnet run --project .\崴正\崴正.csproj --no-launch-profile -- --urls http://127.0.0.1:5265
```

終端 B 也在同一專案根目錄，啟動新版前端。保留兩個終端執行。

```powershell
cd frontend
npm.cmd ci
$env:VITE_API_PROXY_TARGET = 'http://127.0.0.1:5265'
npm.cmd run dev -- --port 5185
```

終端 C 檢查新版 API 與代理。

```powershell
(Invoke-WebRequest 'http://127.0.0.1:5265/api/bulk-material-estimates/options').StatusCode
(Invoke-WebRequest 'http://127.0.0.1:5185/api/bulk-material-estimates/options').StatusCode
```

兩者都應為 200。後端 404 時先核對 Git SHA 及啟動目錄。只有前端 404 時檢查代理目標。若後端回傳 500，查看終端 A 的錯誤及 Supabase 連線設定，回報時遮住連線字串。

## 從案件 #256 繼續

1. 打開 http://127.0.0.1:5185/projects/256，確認「SC 合成廠房-0004」、164 筆明細、待歷史基準審核。
2. 選「決標工項」，篩選「鋼骨鋼構工程」，預期 3 筆已對應「結構鋼骨（kg）」。
3. 到「結構體工程估算」頁，確認有大宗工項估算區。以 #256 的基本資料設定用途廠房、結構 SC，確認可選鋼骨與標準材料。Draft 尚未進入估算參考池，沒有足夠同類 Approved 案時應回報資料不足。
4. 確認新版能正常查詢後，在指定測試環境以 #256 驗證「確認並納入 2A 歷史基準」流程。核對狀態、模型歷史資料及操作紀錄，再執行同用途、同結構的鋼骨估算，確認樣本不足時正常退避。有足夠獨立來源後再核對區間與參考案。模板與土方接著測，土方要比對地下層數。

回報保留 Git SHA、案件編號、API 狀態及畫面。驗證集與測試集供後續保留測試使用。完整流程還包括上傳、解析、人工確認、估算、異常檢查、回寫及稽核。

[廠房測試 Excel 與真值下載](https://chatgpt.com/library?file_id=libfile_2d196bcdc69c81918497a64c71d535b0&account_id=personal)
