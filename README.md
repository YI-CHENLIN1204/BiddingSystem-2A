# BiddingSystem-2A

專案目前採前後端分離架構。後端預設只建置 Web API；舊 Razor UI 原始碼
暫時保留，作為遷移期的明確回退路徑。

## 目錄

- `frontend/`：Vue 3、TypeScript、Vite 單頁應用程式
- `崴正/`：ASP.NET Core Web API、EF Core 與 PostgreSQL

## 本機開發

啟動後端：

```powershell
dotnet run --project .\崴正\崴正.csproj
```

啟動前端：

```powershell
cd .\frontend
npm install
npm run dev
```

前端固定使用 `http://127.0.0.1:5174`，後端固定使用
`http://127.0.0.1:5261`。Vite 會把 `/api` 代理至後端；`strictPort`
已啟用，因此連接埠被占用時會直接報錯，不會靜默切換到另一個連接埠。

## 環境設定

正式環境請設定前端的 `VITE_API_BASE_URL`，並在後端
`Cors:AllowedOrigins` 中列出前端來源。資料庫連線字串可用環境變數
`ConnectionStrings__DefaultConnection` 覆寫。

可複製 `frontend/.env.example` 為 `frontend/.env.local`。本機透過 Vite
proxy 開發時維持 `/api` 即可；前後端部署在不同網域時，改成完整的
後端網址，例如 `https://api.example.com/api`。

## 遷移狀態

新前端已涵蓋投標異常分析、區間估價、決標匯入、專案管理、模型參數、
材料與單位主檔，以及操作紀錄。確認新前端正式環境功能無誤後，才移除
後端的 `Views/` 與 `wwwroot/` 相容層。

後端預設不編譯舊 MVC 頁面 Controller、`Views/` 或 `wwwroot/`，執行時也只
映射屬性路由 API。若需要回退至舊 Razor UI，必須同時在建置與執行時明確
啟用：

```powershell
dotnet run --project .\崴正\崴正.csproj -p:EnableLegacyUi=true -- --LegacyUi:Enabled=true
```

只設定 `LegacyUi:Enabled=true` 而未使用建置參數時，後端會立即停止並說明
設定錯誤，避免產生半啟用狀態。舊頁面與 MVC Controller 在遷移驗收完成前
仍保留於儲存庫。
