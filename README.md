# KGI_test
# 權證試算與避險系統 (Warrant Calculation & Hedging System)

本專案是一個前後端分離的金融試算工具，旨在提供使用者快速檢索權證清單、輸入標的股價進行理論價值計算（Delta 避險張數），並自動記錄與檢視歷史存檔明細。

---

## 🏗️ 系統架構與設計理由

本專案在架構與技術選型上秉持**「輕量、高效、易維護」**的原則：

### 1. 前端：Vue 3 (Composition API) + Axios + 原生 CSS (Flexbox)
* **Vue 3 Composition API**：邏輯組織更清晰，方便將狀態（如選中的權證、輸入的股價、歷史紀錄）與 API 互動函式模組化。
* **原生 CSS (Flexbox)**：捨棄大型 CSS 框架（如 Tailwind），改用純原生 Flexbox 進行左右雙欄排版。這樣做能**免除額外安裝與編譯套件的負擔**，讓專案結構保持最乾淨、最單純，也更容易自訂微調樣式。
* **Axios**：提供強大且直覺的 HTTP 請求攔截與錯誤處理能力，方便與後端進行資料非同步交換。

### 2. 後端：.NET Core Web API + Dapper + SQL Server
* **.NET Core Web API**：高效能的跨平台 Web 服務框架，內建依賴注入與強大路由機制。
* **Dapper (微型 ORM)**：相較於笨重的 EF Core，Dapper 直接透過手寫 SQL 進行對應，執行速度極快，且能精確掌控資料庫查詢邏輯（特別適合處理金融計算與高頻歷史檢索）。
* **SQL Server**：穩定可靠的關聯式資料庫，儲存權證主檔 (`Warrant_Master`) 與歷史試算紀錄 (`Warrant_Trial_Log`)。

---

## ⚙️ 環境需求 (Prerequisites)

* **後端**：.NET 8.0 SDK (或以上)、Visual Studio 2022 (或 VS Code)
* **前端**：Node.js v18.x (或以上)、npm
* **資料庫**：SQL Server

---

## 🛠️ 資料庫初始化 (Database Setup)

請在您的 SQL Server 中執行以下指令建立所需的資料表：

```sql
-- 1. 權證主檔
CREATE TABLE Warrant_Master (
    Warrant_ID VARCHAR(50) PRIMARY KEY,
    Strike_Price DECIMAL(18,4) NOT NULL,
    Conversion_Ratio DECIMAL(18,4) NOT NULL,
    Warrant_Type VARCHAR(20) NOT NULL,
    Position_Qty INT NOT NULL
);

-- 2. 歷史試算與避險紀錄檔
CREATE TABLE Warrant_Trial_Log (
    Log_ID INT IDENTITY(1,1) PRIMARY KEY,
    Warrant_ID VARCHAR(50) NOT NULL,
    Market_Price DECIMAL(18,4) NOT NULL,
    Theory_Price DECIMAL(18,4) NOT NULL,
    Hedge_Qty DECIMAL(18,4) NOT NULL,
    Created_Time DATETIME DEFAULT GETDATE()
);
