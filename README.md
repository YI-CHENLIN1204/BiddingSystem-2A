# BiddingSystem-2A

專案目前採前後端分離架構，並暫時保留舊 Razor UI 作為遷移期回退路徑。

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

前端預設位於 `http://localhost:5173`，開發伺服器會將 `/api` 代理至
`http://localhost:5237`。

## 環境設定

正式環境請設定前端的 `VITE_API_BASE_URL`，並在後端
`Cors:AllowedOrigins` 中列出前端來源。資料庫連線字串可用環境變數
`ConnectionStrings__DefaultConnection` 覆寫。

## 遷移狀態

新前端已涵蓋投標異常分析、區間估價、決標匯入、專案管理、模型參數、
材料與單位主檔，以及操作紀錄。確認新前端正式環境功能無誤後，才移除
後端的 `Views/` 與 `wwwroot/` 相容層。
