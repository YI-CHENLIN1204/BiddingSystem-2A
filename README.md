# BiddingSystem-2A

> **Repository Policy:** 此專案涉及產學合作內容與合作資料，因此維持 Private。對外作品集只描述可公開的架構、方法與本人實際參與內容，不公開合作方原始資料、資料庫內容或未經授權的程式碼。

專案採完全前後端分離架構：Vue 單頁應用程式只透過 HTTP API 與
ASP.NET Core 後端溝通，後端不包含 Razor Views 或前端靜態資產。

## 目錄

- `frontend/`：Vue 3、TypeScript、Vite 單頁應用程式
- `崴正/`：ASP.NET Core Web API、EF Core 與 PostgreSQL

## 本機開發

啟動後端：

```powershell
$env:ConnectionStrings__DefaultConnection = "<PostgreSQL 連線字串>"
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
需要暫時改用其他後端連接埠時，可在啟動前設定
`VITE_API_PROXY_TARGET`，例如 `http://127.0.0.1:5262`。

## 環境設定

正式環境請設定前端的 `VITE_API_BASE_URL`，並在後端
`Cors:AllowedOrigins` 中列出前端來源。資料庫連線字串必須使用環境變數
`ConnectionStrings__DefaultConnection` 提供，不應寫入 `appsettings.json` 或提交至 Git。

可複製 `frontend/.env.example` 為 `frontend/.env.local`。本機透過 Vite
proxy 開發時維持 `/api` 即可；前後端部署在不同網域時，改成完整的
後端網址，例如 `https://api.example.com/api`。

## 架構邊界

- `frontend/` 可獨立安裝、建置與部署，不需要後端輸出任何頁面。
- `崴正/` 只提供 `/api/*` 與 Swagger，不處理 SPA 路由或前端檔案。
- 本機由 Vite proxy 轉送 `/api`；正式環境可使用反向代理或設定完整 API URL。
- 資料庫存取只存在後端，前端不持有資料庫連線資訊。

舊 Razor UI 已從目前版本移除。如需查閱遷移前版本，可從 Git 提交
`b8361f8` 復原；這不影響目前資料庫內容。

## 建置

```powershell
dotnet publish .\崴正\崴正.csproj -c Release

cd .\frontend
npm ci
npm run build
```

## Repository Policy

- 不提交正式資料庫密碼、API key 或 deployment secret。
- 不公開合作方原始工程 / 成交 / 投標資料。
- `.env.example` 僅保留 placeholder。
- 若未來需要展示，只使用合成 / 去識別化資料與可公開截圖。
