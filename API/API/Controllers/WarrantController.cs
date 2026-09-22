using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarrantController : Controller
    {
        private readonly string _connectionString;
        //public IActionResult Index()
        //{
        //    return View();
        //}

        public WarrantController(IConfiguration configuration)
        {
            // 讀取 appsettings.json 裡的資料庫連線字串
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // 1. 取得權證清單 API (供左側列表與搜尋使用)
        [HttpGet("list")]
        public async Task<IActionResult> GetWarrants([FromQuery] string? keyword)
        {
            //using var conn = new SqlConnection(_connectionString);
            //string sql = "SELECT Warrant_ID, Strike_Price, Conversion_Ratio, Warrant_Type, Position_Qty FROM Warrant_Master";

            //if (!string.IsNullOrEmpty(keyword))
            //{
            //    sql += " WHERE Warrant_ID LIKE @Keyword";
            //}
            //sql += " ORDER BY Warrant_ID";

            //var list = await conn.QueryAsync(sql, new { Keyword = $"%{keyword}%" });
            //return Ok(list);
            try
            {
                using var conn = new SqlConnection(_connectionString);

                // 如果沒有輸入關鍵字，直接抓全部
                if (string.IsNullOrEmpty(keyword))
                {
                    string sqlAll = "SELECT Warrant_ID, Strike_Price, Conversion_Ratio, Warrant_Type, Position_Qty FROM Warrant_Master ORDER BY Warrant_ID";
                    var allList = await conn.QueryAsync<WarrantMasterModel>(sqlAll);
                    return Ok(allList);
                }
                else
                {
                    // 如果有輸入關鍵字，才加上 WHERE 條件
                    string sqlKeyword = "SELECT Warrant_ID, Strike_Price, Conversion_Ratio, Warrant_Type, Position_Qty FROM Warrant_Master WHERE Warrant_ID LIKE @Keyword ORDER BY Warrant_ID";
                    var list = await conn.QueryAsync<WarrantMasterModel>(sqlKeyword, new { Keyword = $"%{keyword}%" });
                    return Ok(list);
                }
            }
            catch (Exception ex)
            {
                // 這樣當 500 發生時，會把真實的例外原因（例如資料庫連線失敗、欄位打錯）回傳給前端
                return StatusCode(500, new { message = "伺服器內部錯誤: " + ex.Message });
            }
        }

        // 2. 核心試算與儲存 API (取代原本 WinForms 的計算按鈕)
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateAndSave([FromBody] TrialRequestDto request)
        {
            // 【防禦性檢查】：標的價 <= 0 時 API 回傳錯誤，禁止存檔
            if (request.Market_Price <= 0)
            {
                return BadRequest(new { message = "防禦性檢查錯誤：標的股價必須大於 0，禁止試算與存檔！" });
            }

            using var conn = new SqlConnection(_connectionString);

            // 取得該權證的基本資料
            var warrant = await conn.QueryFirstOrDefaultAsync<WarrantMasterModel>(
                "SELECT * FROM Warrant_Master WHERE Warrant_ID = @Warrant_ID",
                new { request.Warrant_ID });

            if (warrant == null)
            {
                return NotFound(new { message = "找不到指定的權證代碼" });
            }

            // 【核心計算 1：理論價值計算】
            decimal theoryPrice = 0;
            if (warrant.Warrant_Type.ToUpper() == "CALL")
            {
                theoryPrice = Math.Max(0, (request.Market_Price - warrant.Strike_Price) * warrant.Conversion_Ratio);
            }
            else if (warrant.Warrant_Type.ToUpper() == "PUT")
            {
                theoryPrice = Math.Max(0, (warrant.Strike_Price - request.Market_Price) * warrant.Conversion_Ratio);
            }

            // 【核心計算 2：Delta 判斷與避險張數試算】
            decimal delta = 0.2M; // 預設 OTM
            if (request.Market_Price == warrant.Strike_Price)
            {
                delta = 0.5M; // ATM
            }
            else if ((warrant.Warrant_Type.ToUpper() == "CALL" && request.Market_Price > warrant.Strike_Price) ||
                     (warrant.Warrant_Type.ToUpper() == "PUT" && request.Market_Price < warrant.Strike_Price))
            {
                delta = 0.8M; // ITM
            }

            decimal hedgeQty = warrant.Position_Qty * warrant.Conversion_Ratio * delta;

            // 【如果前端勾選或點擊儲存，寫入 Warrant_Trial_Log】
            if (request.Save_Flag)
            {
                string insertSql = @"
                INSERT INTO Warrant_Trial_Log (Warrant_ID, Market_Price, Theory_Price, Hedge_Qty, Created_Time)
                VALUES (@Warrant_ID, @Market_Price, @Theory_Price, @Hedge_Qty, GETDATE())";

                await conn.ExecuteAsync(insertSql, new
                {
                    request.Warrant_ID,
                    request.Market_Price,
                    Theory_Price = theoryPrice,
                    Hedge_Qty = hedgeQty
                });
            }

            // 將計算結果回傳給 Vue 前端
            return Ok(new
            {
                Theory_Price = theoryPrice,
                Hedge_Qty = hedgeQty,
                Delta = delta
            });
        }

        // 3. 取得歷史紀錄 API (最近 10 筆)
        [HttpGet("history/{warrantId}")]
        public async Task<IActionResult> GetHistory(string warrantId)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = @"
            SELECT TOP 10 Log_ID, Warrant_ID, Market_Price, Theory_Price, Hedge_Qty, Created_Time 
            FROM Warrant_Trial_Log 
            WHERE Warrant_ID = @Warrant_ID 
            ORDER BY Created_Time DESC";

            var logs = await conn.QueryAsync(sql, new { Warrant_ID = warrantId });
            return Ok(logs);
        }

    }

    // 輔助 DTO 類別
    public class TrialRequestDto
    {
        public string Warrant_ID { get; set; }
        public decimal Market_Price { get; set; }
        public bool Save_Flag { get; set; }
    }

    public class WarrantMasterModel
    {
        public string Warrant_ID { get; set; }
        public decimal Strike_Price { get; set; }
        public decimal Conversion_Ratio { get; set; }
        public string Warrant_Type { get; set; }
        public int Position_Qty { get; set; }
    }

}
