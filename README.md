# BiddingSystem-2A

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


## 1D → 2A 介接（開發中）

2A 目前新增一層唯讀的 1D 介接骨架，用來依 Project ID 取得 1D 已完成並儲存的整個建案工程大項分類結果，
再由 2A 在各工程大項內建立異常偵測所需的材料細項分類。1D 已負責工程大項，2A 不重複分類大項。

目前新增 API：

- `GET /api/one-d/status`：檢查 1D API 是否已設定。
- `GET /api/one-d/projects/{projectId}/result`：唯讀取得 1D 已儲存的分類結果。
- `GET /api/one-d/projects/{projectId}/detection-preview`：取得 1D 結果後，預覽 2A 偵錯細項分類。
- `POST /api/one-d/detection-preview`：不連線 1D，直接貼入測試資料進行細項分類預覽。

目前以完整建案作為 POC 輸入。所有 1D 工程大項都會保留；2A 只對已建立規則的大項繼續往下分類：

- 結構體工程 → 鋼筋
- 結構體工程 → 混凝土
- 結構體工程 → 模板
- 鋼骨鋼構工程 → 鋼骨

例如防水工程、門窗工程、景觀工程等大項若尚未建立 2A 細項規則，仍會保留在建案分析結果中，
並明確標示「細項規則待建立」，不會被忽略或硬分類。

此階段不修改既有異常偵測主流程、不寫入新的分類資料表，
等待需求確認細項分析粒度後再正式串接。

Docker 使用時可在根目錄 `.env` 設定：

```env
ONE_D_API_BASE_URL="http://<1D-host>:<1D-port>/"
```

若 1D 尚未部署或連線位址尚未確認，可先留空；
2A 既有功能仍可正常啟動。


### POC 展示方式

切到 `feature/1d-detection-integration` 分支後，以 Docker 啟動：

```powershell
docker compose up -d --build
```

開啟：

```text
http://localhost:8081/project-analysis-preview
```

展示建議順序：

1. 按「載入示範建案」，確認 2A 一次承接同一建案內多個 1D 工程大項，而不是只處理結構體工程。
2. 查看「建案工程大項總覽」，可看到假設工程、土方及基礎工程、結構體工程、鋼骨鋼構工程、防水工程、門窗工程、景觀工程等大項全部被保留。
3. 查看「已建立的 2A 偵錯細項」，展示結構體工程已可往下形成鋼筋、混凝土、模板；鋼骨鋼構工程可形成鋼骨。
4. 說明「SD420W 鋼筋續接器」會維持待確認，不會誤併入鋼筋重量。
5. 說明防水、門窗、景觀等尚未建立細項規則的大項仍保留在建案中，後續可逐步補上，不影響 1D 大項結果。

