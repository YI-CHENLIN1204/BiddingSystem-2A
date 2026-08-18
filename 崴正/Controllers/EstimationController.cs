using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace 崴正.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstimationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EstimationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 保留原本的 JSON 測試 API，避免其他地方若有用到會壞掉。
        [HttpPost("analyze")]
        public IActionResult AnalyzeBid([FromBody] BidRequestDto request)
        {
            var finalReport = new EstimationResponseDto { Project_Id = request.Project_Id };

            var groupedItems = request.Items
                .GroupBy(x => new { x.Material_Id, x.Floor_Zone })
                .Select(g => new
                {
                    Material_Id = g.Key.Material_Id,
                    Floor_Zone = g.Key.Floor_Zone,
                    Total_Quantity = g.Sum(x => x.Input_Quantity)
                }).ToList();

            foreach (var item in groupedItems)
            {
                var rule = _context.QuantityRuleBases.FirstOrDefault(r => r.Material_ID == item.Material_Id);
                decimal ruleThreshold = rule != null ? rule.Z_Score_Threshold : 2.0m;

                var historyData = _context.HistoricalTransactions
                    .Where(h => h.MaterialId == item.Material_Id && h.IsOutlier == false)
                    .Select(h => new { h.ActualQuantity, h.UnitPrice, h.TransactionDate })
                    .ToList();

                decimal suggestedQty = 0;
                decimal calculatedZScore = 0;
                bool isAbnormal = false;
                string statusMsg = "";
                decimal predictedPrice = 0;

                if (!historyData.Any())
                {
                    statusMsg = rule != null ? "需人工首筆核定" : "無基準資料";
                    suggestedQty = item.Total_Quantity;
                    isAbnormal = true;
                }
                else
                {
                    double k = 0.005;
                    DateTime today = DateTime.Today;
                    double totalWeight = 0;
                    double weightedSum = 0;

                    var weightedData = historyData.Select(h =>
                    {
                        int daysOld = (today - h.TransactionDate).Days;
                        double weight = Math.Exp(-k * daysOld);
                        totalWeight += weight;
                        weightedSum += (double)h.ActualQuantity * weight;
                        return new { h.ActualQuantity, weight };
                    }).ToList();

                    double weightedMean = weightedSum / totalWeight;
                    suggestedQty = (decimal)weightedMean;

                    double varianceSum = weightedData.Sum(d => d.weight * Math.Pow((double)d.ActualQuantity - weightedMean, 2));
                    decimal historySD = (decimal)Math.Sqrt(varianceSum / totalWeight);

                    if (historySD == 0)
                    {
                        calculatedZScore = 0;
                        isAbnormal = false;
                        statusMsg = "資料一致 (無波動)";
                    }
                    else
                    {
                        calculatedZScore = Math.Abs((item.Total_Quantity - (decimal)weightedMean) / historySD);
                        isAbnormal = calculatedZScore > ruleThreshold;
                        statusMsg = isAbnormal ? "異常高估" : "合理";
                    }

                    predictedPrice = historyData.Average(h => h.UnitPrice) * 1.02m;
                }

                decimal refTotalPrice = item.Total_Quantity * predictedPrice;

                finalReport.Results.Add(new EstimationResultDto
                {
                    Material_Id = item.Material_Id,
                    Floor_Zone = item.Floor_Zone,
                    Calculated_Z_Score = Math.Round(calculatedZScore, 2),
                    Is_Abnormal = isAbnormal,
                    Predicted_Unit_Price = Math.Round(predictedPrice, 0),
                    Reference_Total_Price = Math.Round(refTotalPrice, 0),
                    Suggested_Quantity = Math.Round(suggestedQty, 2),
                    Status_Message = statusMsg
                });
            }

            return Ok(finalReport);
        }

        // 新版：直接上傳 Excel，不儲存本次標單；使用資料庫歷史資料計算時間權重 Z 分數。
        [HttpPost("analyze-excel")]
        public IActionResult AnalyzeExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest(new { message = "請先選擇 Excel 檔案" });

            string extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                return BadRequest(new { message = "目前請先上傳 .xlsx 檔案；.xls 舊格式請先另存新檔為 .xlsx" });

            try
            {
                var start = DateTime.Now;

                var parser = new TenderExcelParser();
                List<TenderImportRowDto> rows;

                using (var stream = excelFile.OpenReadStream())
                {
                    rows = parser.Parse(stream, excelFile.FileName);
                }

                var afterParse = DateTime.Now;

                var results = AnalyzeRowsWithHistory(rows);

                var afterAnalyze = DateTime.Now;

                Console.WriteLine($"Excel解析耗時：{(afterParse - start).TotalSeconds} 秒");
                Console.WriteLine($"異常偵測耗時：{(afterAnalyze - afterParse).TotalSeconds} 秒");
                Console.WriteLine($"總耗時：{(afterAnalyze - start).TotalSeconds} 秒");
                Console.WriteLine($"解析筆數：{rows.Count}，回傳筆數：{results.Count}");

                return Ok(new TenderAnomalyResponseDto
                {
                    TotalCount = results.Count,
                    AbnormalCount = results.Count(x => x.IsAbnormal),
                    Results = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Excel 解析或異常偵測失敗：" + ex.Message });
            }
        }

        /// <summary>
        /// 使用資料庫歷史資料計算異常。
        /// 短期：最近 30 天，權重 70%。
        /// 長期：最近 180 天，權重 30%。
        /// 加權Z = 長期Z × 0.3 + 短期Z × 0.7。
        ///
        /// 優化重點：
        /// 1. 材料主檔只查一次。
        /// 2. 規則表只查一次。
        /// 3. 180 天歷史資料只查一次。
        /// 4. foreach 裡面不再重複查資料庫。
        /// </summary>
        private List<TenderAnomalyResultDto> AnalyzeRowsWithHistory(List<TenderImportRowDto> rows)
        {
            var results = new List<TenderAnomalyResultDto>();

            var today = DateTime.UtcNow.Date;
            var shortTermStartDate = today.AddDays(-30);
            var longTermStartDate = today.AddDays(-180);

            const decimal shortTermWeight = 0.7m;
            const decimal longTermWeight = 0.3m;
            const int minimumSampleCount = 10;

        // 1. 把字典表全部抓出來放進記憶體 (UC-01 智慧對應)
            var dictionaryList = _context.ItemDictionaries.ToList();

                // 2. 抓出系統所有標準建材
            var materials = _context.MaterialMasters.ToList();

            var materialDict = materials
                .ToDictionary(m => m.MaterialId, m => m);

            var materialIds = materials
                .Select(m => m.MaterialId)
                .ToList();

            // 2. 規則表先一次查出來
            var ruleDict = _context.QuantityRuleBases
                .Where(r => materialIds.Contains(r.Material_ID))
                .ToDictionary(r => r.Material_ID, r => r.Z_Score_Threshold);

            // 3. 180 天內的歷史資料先一次查出來
            var allHistories = _context.HistoricalTransactions
                .Where(h => materialIds.Contains(h.MaterialId))
                .Where(h => h.IsDeleted == false)
                .Where(h => h.IsOutlier == false)
                .Where(h => h.TransactionDate >= longTermStartDate)
                .ToList();

            // 4. 依照材料 ID 分組，後面直接從記憶體拿
            var historyDict = allHistories
                .GroupBy(h => h.MaterialId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var row in rows)
            {
                var result = new TenderAnomalyResultDto
                {
                    SourceFileName = row.SourceFileName,
                    SheetName = row.SheetName,
                    RowNumber = row.RowNumber,
                    ItemCode = row.ItemCode,
                    Category1 = row.Category1,
                    Category2 = row.Category2,
                    Category3 = row.Category3,
                    ItemName = row.ItemName,
                    Spec = row.Spec,
                    Unit = row.Unit,
                    Qty = row.Qty,
                    UnitPrice = row.UnitPrice,
                    Amount = row.Amount,
                    Note = row.Note,
                    StatusMessage = "正常"
                };

                var reasons = new List<string>();

                // 基本資料合理性檢查
                if (result.Qty == null || result.Qty <= 0)
                    reasons.Add("數量空白或小於等於 0");

                if (result.UnitPrice == null || result.UnitPrice < 0)
                    reasons.Add("單價空白或小於 0");

                if (result.Amount != null && result.Amount < 0)
                    reasons.Add("複價小於 0");

                bool hasCriticalDataProblem =
                    string.IsNullOrWhiteSpace(result.Unit) ||
                    result.Qty == null ||
                    result.Qty <= 0 ||
                    result.UnitPrice == null ||
                    result.UnitPrice < 0;

                if (hasCriticalDataProblem)
                {
                    result.IsAbnormal = false;

                    result.QtyZScore = null;
                    result.UnitPriceZScore = null;

                    result.ShortTermQtyZScore = null;
                    result.LongTermQtyZScore = null;
                    result.WeightedQtyZScore = null;

                    result.ShortTermUnitPriceZScore = null;
                    result.LongTermUnitPriceZScore = null;
                    result.WeightedUnitPriceZScore = null;

                    result.StatusMessage = "資料不完整：" + string.Join("、", reasons);

                    results.Add(result);
                    continue;
                }

                if (result.Qty != null && result.UnitPrice != null)
                {
                    result.ExpectedAmount = Math.Round(result.Qty.Value * result.UnitPrice.Value, 2);

                    if (result.Amount != null)
                    {
                        result.AmountDifference = Math.Round(result.Amount.Value - result.ExpectedAmount.Value, 2);
                        var tolerance = Math.Max(1m, Math.Abs(result.ExpectedAmount.Value) * 0.05m);

                        if (Math.Abs(result.AmountDifference.Value) > tolerance)
                            reasons.Add("複價與 數量×單價 不一致");
                    }
                }

                // ==========================================
                // 🚀 實現 UC-01：系統自動對應標準工項
                // ==========================================
                // 系統會去比對 Excel 的「項目名稱」或「規格」有沒有包含字典裡的關鍵字
                var matchedDict = dictionaryList.FirstOrDefault(d => 
                    (result.ItemName ?? "").Contains(d.Keyword, StringComparison.OrdinalIgnoreCase) || 
                    (result.Spec ?? "").Contains(d.Keyword, StringComparison.OrdinalIgnoreCase));

                if (matchedDict == null)
                {
                    // 找不到對應，標記為「待對應」(符合 UC-01 替代流程 3a)
                    result.IsAbnormal = false;
                    result.StatusMessage = "待對應 (系統字典無法辨識此工項)";
                    results.Add(result);
                    continue;
                }

                // 成功對應！找出真正的標準建材
                if (!materialDict.TryGetValue(matchedDict.Material_Id, out var material))
                {
                    result.IsAbnormal = true;
                    result.StatusMessage = "系統錯誤：字典對應到不存在的材料ID";
                    results.Add(result);
                    continue;
                }

                result.MatchedMaterialName = material.MaterialName;
                result.Category = material.Category;

                if (!IsSameUnit(result.Unit, material.BaseUnit))
                {
                    // 單位不符合時，不列入 Z 分數異常偵測
                    // 不算異常，也不計算任何 Z 分數
                    result.IsAbnormal = false;

                    result.QtyZScore = null;
                    result.UnitPriceZScore = null;

                    result.ShortTermQtyZScore = null;
                    result.LongTermQtyZScore = null;
                    result.WeightedQtyZScore = null;

                    result.ShortTermUnitPriceZScore = null;
                    result.LongTermUnitPriceZScore = null;
                    result.WeightedUnitPriceZScore = null;

                    if (reasons.Any())
                    {
                        result.StatusMessage = string.Join("、", reasons) +
                            $"、單位不符，未列入異常偵測（資料庫單位為 {material.BaseUnit}）";
                    }
                    else
                    {
                        result.StatusMessage =
                            $"單位不符，未列入異常偵測（資料庫單位為 {material.BaseUnit}）";
                    }

                    results.Add(result);
                    continue;
                }

                // 從參數表取得 Z-score 異常門檻，若沒有設定則預設為 2.0
                decimal ruleThreshold = 2.0m;

                if (ruleDict.TryGetValue(material.MaterialId, out var threshold) && threshold > 0)
                    ruleThreshold = threshold;

                // 從 Dictionary 找長期歷史資料，不再查資料庫
                var longHistories = historyDict.TryGetValue(material.MaterialId, out var histories)
                    ? histories
                    : new List<HistoricalTransaction>();

                // 短期資料直接從長期資料中篩選，不再查資料庫
                var shortHistories = longHistories
                    .Where(h => h.TransactionDate >= shortTermStartDate)
                    .ToList();

                var shortPriceValues = shortHistories.Select(h => h.UnitPrice).ToList();
                var longPriceValues = longHistories.Select(h => h.UnitPrice).ToList();

                int sampleCount = longHistories
                    .Where(h => h.ProjectMasterId.HasValue)
                    .Select(h => h.ProjectMasterId)
                    .Distinct()
                    .Count();

                if (sampleCount < minimumSampleCount)
                {
                    result.IsAbnormal = false;

                    result.QtyZScore = null;
                    result.UnitPriceZScore = null;

                    result.ShortTermQtyZScore = null;
                    result.LongTermQtyZScore = null;
                    result.WeightedQtyZScore = null;

                    result.ShortTermUnitPriceZScore = null;
                    result.LongTermUnitPriceZScore = null;
                    result.WeightedUnitPriceZScore = null;

                    result.StatusMessage =
                        $"資料不足：歷史工程案例數不足（{sampleCount}/{minimumSampleCount}），不執行 Z-score 統計判斷，列入待審。";

                    results.Add(result);
                    continue;
                }       
                // 畫面上的歷史平均與標準差以長期 180 天為主要基準

                if (longPriceValues.Count >= 2)
                {
                    result.HistoricalAvgUnitPrice180Days = Math.Round(longPriceValues.Average(), 2);
                    result.HistoricalStdUnitPrice180Days = Math.Round(CalculateStdDev(longPriceValues), 2);
                }

                if (result.HistoricalAvgUnitPrice180Days.HasValue && result.Qty.HasValue)
                {
                    result.SuggestedUnitPrice = result.HistoricalAvgUnitPrice180Days.Value;
                    result.SuggestedAmount = Math.Round(result.SuggestedUnitPrice.Value * result.Qty.Value, 0);
                }

                result.ShortTermUnitPriceZScore = CalculateZScore(result.UnitPrice, shortPriceValues);
                result.LongTermUnitPriceZScore = CalculateZScore(result.UnitPrice, longPriceValues);
                result.WeightedUnitPriceZScore = CalculateWeightedZScore(
                    result.LongTermUnitPriceZScore,
                    result.ShortTermUnitPriceZScore,
                    longTermWeight,
                    shortTermWeight
                );

                // 課程版僅使用單價 Z-score 進行價格異常偵測
                result.QtyZScore = null;
                result.UnitPriceZScore = result.WeightedUnitPriceZScore;



                if (result.WeightedUnitPriceZScore.HasValue && result.WeightedUnitPriceZScore.Value >= ruleThreshold)
                {
                    reasons.Add($"單價偏離歷史行情（Z = {result.WeightedUnitPriceZScore.Value:0.00}，門檻 = {ruleThreshold:0.##}）");
                }

                if (reasons.Any())
                {
                    result.IsAbnormal = true;
                    result.StatusMessage = string.Join("、", reasons);
                }
                else
                {
                    result.IsAbnormal = false;
                    result.StatusMessage = "正常";
                }

                results.Add(result);
            }

            return results;
        }


        private bool IsSameUnit(string excelUnit, string databaseUnit)
        {
            return NormalizeUnit(excelUnit) == NormalizeUnit(databaseUnit);
        }

        private string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return string.Empty;

            var normalized = unit.Trim().ToLowerInvariant();

            return normalized switch
            {
                "㎡" => "m2",
                "m²" => "m2",
                "m2" => "m2",
                "m 2" => "m2",
                "立方米" => "m3",
                "m³" => "m3",
                "m3" => "m3",
                "m 3" => "m3",
                "公斤" => "kg",
                "kg" => "kg",
                "kgs" => "kg",
                _ => normalized
            };
        }

        private decimal GetZScoreThreshold(int materialId)
        {
            var rule = _context.QuantityRuleBases.FirstOrDefault(r => r.Material_ID == materialId);
            return rule?.Z_Score_Threshold > 0 ? rule.Z_Score_Threshold : 2.0m;
        }

        private decimal? CalculateZScore(decimal? currentValue, List<decimal> historyValues)
        {
            if (!currentValue.HasValue)
                return null;

            if (historyValues == null || historyValues.Count < 2)
                return null;

            var average = historyValues.Average();
            var stdDev = CalculateStdDev(historyValues);

            if (stdDev == 0)
                return null;

            return Math.Round(Math.Abs((currentValue.Value - average) / stdDev), 2);
        }

        private decimal? CalculateWeightedZScore(decimal? longTermZ, decimal? shortTermZ, decimal longTermWeight, decimal shortTermWeight)
        {
            if (longTermZ.HasValue && shortTermZ.HasValue)
                return Math.Round(longTermZ.Value * longTermWeight + shortTermZ.Value * shortTermWeight, 2);

            if (longTermZ.HasValue)
                return longTermZ.Value;

            if (shortTermZ.HasValue)
                return shortTermZ.Value;

            return null;
        }

        private decimal CalculateStdDev(List<decimal> values)
        {
            if (values == null || values.Count < 2)
                return 0;

            var average = values.Average();
            var variance = values.Average(v => Math.Pow((double)(v - average), 2));
            return (decimal)Math.Sqrt(variance);
        }

        // ==========================================
        // UC-04：人工推翻異常判定並寫入 Audit Log
        // ==========================================
        [HttpPost("override")]
        public IActionResult OverrideAnomaly([FromBody] OverrideRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.OverrideReason))
            {
                return BadRequest(new { message = "⚠️ 系統阻擋：必須強制填寫推翻理由！" });
            }

            // 組裝稽核資料
            var log = new AuditLog
            {
                SourceFileName = request.SourceFileName,
                SheetName = request.SheetName,
                RowNumber = request.RowNumber,
                ItemName = request.ItemName,
                OriginalStatusMessage = request.OriginalStatusMessage,
                OverrideReason = request.OverrideReason,
                ActionUser = "Estimator_Staff", // 實務上可串接登入者資訊
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.AuditLogs.Add(log);
                _context.SaveChanges();

                return Ok(new { message = "✅ 成功寫入 Audit Log 稽核軌跡！", status = "人工認可" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Audit Log 寫入失敗：" + ex.Message });
            }
        }

        
        [HttpPost("upload-final-bid")]
        public IActionResult UploadFinalBid(
            IFormFile excelFile,
            [FromForm] string projectName,
            [FromForm] string projectType,
            [FromForm] string structureType,
            [FromForm] string city,
            [FromForm] decimal? totalFloorArea,
            [FromForm] int? floorCount,
            [FromForm] int? basementFloorCount,
            [FromForm] string projectMonth)
        {
            // ==============================
            // 1. 基本資料檢查
            // ==============================

            if (excelFile == null || excelFile.Length == 0)
                return BadRequest(new { message = "請先選擇 Excel 檔案" });

            string extension =
                Path.GetExtension(excelFile.FileName)
                    .ToLowerInvariant();

            if (extension != ".xlsx")
            {
                return BadRequest(new
                {
                    message = "目前僅接受 .xlsx 檔案"
                });
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                return BadRequest(new
                {
                    message = "請輸入工程專案名稱"
                });
            }
            if (string.IsNullOrWhiteSpace(projectMonth) ||
            !DateTime.TryParseExact(
                projectMonth,
                "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedProjectMonth))
        {
            return BadRequest(new
            {
                message = "請選擇正確的建案年月"
            });
        }
            projectName = projectName.Trim();

            projectType =
                NormalizeImportText(projectType);

            structureType =
                NormalizeImportText(structureType);

            city =
                NormalizeImportText(city);

            if (!IsAllowedValue(
                    projectType,
                    "住宅",
                    "廠房"))
            {
                return BadRequest(new
                {
                    message = "請選擇建案類型：住宅或廠房"
                });
            }

            if (!IsAllowedValue(
                    structureType,
                    "SC",
                    "RC",
                    "SRC"))
            {
                return BadRequest(new
                {
                    message = "請選擇結構類型：SC、RC 或 SRC"
                });
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest(new
                {
                    message = "請輸入縣市"
                });
            }
            if (!totalFloorArea.HasValue ||
                totalFloorArea.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "請輸入正確的總樓地板面積"
                });
            }
            if (!floorCount.HasValue ||
                floorCount.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "請輸入正確的地上樓層數"
                });
            }

            if (!basementFloorCount.HasValue ||
                basementFloorCount.Value < 0)
            {
                return BadRequest(new
                {
                    message = "請輸入正確的地下樓層數"
                });
            }

            try
            {
                // ==============================
                // 2. 將 Excel 讀進記憶體
                // ==============================

                using var uploadBuffer =
                    new MemoryStream();

                excelFile.CopyTo(uploadBuffer);

                byte[] excelBytes =
                    uploadBuffer.ToArray();

                // ==============================
                // 3. SHA-256 防止同檔重複上傳
                // ==============================

                string fileHash =
                    Convert.ToHexString(
                        SHA256.HashData(excelBytes))
                    .ToLowerInvariant();

                bool sameFileExists =
                    _context.ProjectMasters.Any(p =>
                        !p.IsDeleted &&
                        p.SourceFileHash == fileHash);

                if (sameFileExists)
                {
                    return Conflict(new
                    {
                        message =
                            "此最終標單已經回寫過，不能重複上傳。"
                    });
                }

                // ==============================
                // 4. 防止同一建案重複建立
                // ==============================

                bool sameProjectExists =
                    _context.ProjectMasters.Any(p =>
                        !p.IsDeleted &&
                        p.ProjectName == projectName);

                if (sameProjectExists)
                {
                    return Conflict(new
                    {
                        message =
                            "此建案已完成決標回寫。如需重新上傳，請先至建案管理刪除原建案。"
                    });
                }

                // ==============================
                // 5. Excel 解析
                // ==============================

                var parser =
                    new TenderExcelParser();

                var rows =
                    parser.Parse(
                        new MemoryStream(excelBytes),
                        excelFile.FileName);

                if (rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "標單中沒有解析到可用的工程明細"
                    });
                }

                // ==============================
                // 6. DB Transaction
                // ==============================

                using var dbTransaction =
                    _context.Database.BeginTransaction();

                DateTime now =
                    DateTime.UtcNow;

                // 樓層顯示原始格式
                string floorRange =
                    basementFloorCount.Value > 0
                        ? $"B{basementFloorCount.Value}~{floorCount.Value}F"
                        : $"1~{floorCount.Value}F";

                // ==============================
                // 7. 建立唯一 ProjectMaster
                // ==============================

                var project =
                    new ProjectMaster
                    {
                        ProjectName = projectName,

                        ProjectType = projectType,

                        StructureType = structureType,

                        City = city,

                        TotalFloorArea =
                            totalFloorArea,

                        FloorCount =
                            floorCount.Value,

                        BasementFloorCount =
                            basementFloorCount.Value,

                        FloorRange =
                            floorRange,

                        StartDate = new DateTime(parsedProjectMonth.Year,parsedProjectMonth.Month,1),

                        SourceFileName =
                            excelFile.FileName,

                        SourceFileHash =
                            fileHash,

                        ImportedAt =
                            DateTime.UtcNow,

                        IsDeleted =
                            false
                    };

                _context.ProjectMasters.Add(project);

                // 先存一次，
                // 取得 project.ProjectId
                _context.SaveChanges();

                // ==============================
                // 8. 載入字典與材料主檔
                // ==============================

                var dictionaryList =
                    _context.ItemDictionaries
                        .ToList();

                var materials =
                    _context.MaterialMasters
                        .ToList();

                var materialDict =
                    materials.ToDictionary(
                        m => m.MaterialId,
                        m => m);

                // 等 FinalBidItem 取得 ID 後
                // 再建立 HistoricalTransaction
                var historyCandidates =
                    new List<FinalBidItem>();

                int autoMappedCount = 0;
                int unmappedCount = 0;
                int invalidDataCount = 0;
                int missingMaterialCount = 0;
                int unitMismatchCount = 0;

                // ==============================
                // 9. 每一列都保存成 FinalBidItem
                // ==============================
                foreach (var row in rows)
                {
                    // ==========================================
                    // 1. 先嘗試自動材料匹配
                    // ==========================================

                    var matchedDict =
                        dictionaryList
                            .Where(d =>
                                !string.IsNullOrWhiteSpace(d.Keyword) &&
                                (
                                    (row.ItemName ?? "")
                                        .Contains(
                                            d.Keyword,
                                            StringComparison.OrdinalIgnoreCase)
                                    ||
                                    (row.Spec ?? "")
                                        .Contains(
                                            d.Keyword,
                                            StringComparison.OrdinalIgnoreCase)
                                ))
                            .OrderByDescending(d => d.Keyword.Length)
                            .FirstOrDefault();

                    MaterialMaster? material = null;

                    int? materialId = null;

                    string? matchedKeyword = null;

                    // 預設就是待處理
                    string mappingStatus = "Pending";

                    string? issueCode = null;

                    var issueMessages = new List<string>();


                    // ==========================================
                    // 2. 判斷材料對應問題
                    // ==========================================

                    if (matchedDict == null)
                    {
                        issueCode = "NO_DICTIONARY_MATCH";

                        issueMessages.Add(
                            "找不到可對應的字典關鍵字"
                        );
                    }
                    else
                    {
                        matchedKeyword =
                            matchedDict.Keyword;

                        if (!materialDict.TryGetValue(
                                matchedDict.Material_Id,
                                out var matchedMaterial))
                        {
                            issueCode =
                                "MATERIAL_NOT_FOUND";

                            issueMessages.Add(
                                $"字典指向不存在的材料 ID：{matchedDict.Material_Id}"
                            );

                            missingMaterialCount++;
                        }
                        else
                        {
                            material =
                                matchedMaterial;

                            materialId =
                                matchedMaterial.MaterialId;
                        }
                    }


                    // ==========================================
                    // 3. 判斷數量問題
                    // ==========================================

                    if (!row.Qty.HasValue ||
                        row.Qty.Value <= 0)
                    {
                        // 如果前面還沒有更主要的問題，
                        // 就把數量問題當主要篩選原因
                        if (issueCode == null)
                        {
                            issueCode =
                                "INVALID_QUANTITY";
                        }

                        issueMessages.Add(
                            "數量空白或小於等於 0"
                        );

                        invalidDataCount++;
                    }


                    // ==========================================
                    // 4. 判斷單價問題
                    // ==========================================

                    if (!row.UnitPrice.HasValue ||
                        row.UnitPrice.Value < 0)
                    {
                        if (issueCode == null)
                        {
                            issueCode =
                                "INVALID_UNIT_PRICE";
                        }

                        issueMessages.Add(
                            "單價空白或小於 0"
                        );

                        invalidDataCount++;
                    }


                    // ==========================================
                    // 5. 判斷單位問題
                    // ==========================================

                    if (material != null &&
                        !IsSameUnit(
                            row.Unit ?? "",
                            material.BaseUnit))
                    {
                        if (issueCode == null)
                        {
                            issueCode =
                                "UNIT_MISMATCH";
                        }

                        issueMessages.Add(
                            $"標單單位「{row.Unit ?? "空白"}」" +
                            $"與標準材料單位「{material.BaseUnit}」不一致"
                        );

                        unitMismatchCount++;
                    }


                    // ==========================================
                    // 6. 完全沒有問題才算 Auto
                    // ==========================================

                    if (issueCode == null)
                    {
                        mappingStatus =
                            "Auto";

                        autoMappedCount++;
                    }
                    else
                    {
                        mappingStatus =
                            "Pending";

                        unmappedCount++;
                    }


                    // ==========================================
                    // 7. 不管成功或失敗，
                    //    每一列都寫進 FinalBidItem
                    // ==========================================

                    var bidItem =
                        new FinalBidItem
                        {
                            ProjectMasterId =
                                project.ProjectId,

                            SourceFileName =
                                excelFile.FileName,

                            SheetName =
                                row.SheetName,

                            RowNumber =
                                row.RowNumber,

                            ItemCode =
                                row.ItemCode,

                            Category1 =
                                row.Category1,

                            Category2 =
                                row.Category2,

                            Category3 =
                                row.Category3,

                            ItemName =
                                row.ItemName ?? string.Empty,

                            Spec =
                                row.Spec,

                            Unit =
                                row.Unit,

                            Qty =
                                row.Qty,

                            UnitPrice =
                                row.UnitPrice,

                            Amount =
                                row.Amount,

                            Note =
                                row.Note,

                            MaterialId =
                                materialId,

                            MappingStatus =
                                mappingStatus,

                            MappingIssueCode =
                                issueCode,

                            MappingIssueMessage =
                                issueMessages.Count > 0
                                    ? string.Join("；", issueMessages)
                                    : null,

                            MatchedKeyword =
                                matchedKeyword,

                            CreatedAt =
                                now,

                            UpdatedAt =
                                now
                        };

                    _context.FinalBidItems.Add(
                        bidItem);


                    // ==========================================
                    // 8. 只有 Auto 才能進 HistoricalTransaction
                    // ==========================================

                    if (mappingStatus == "Auto")
                    {
                        historyCandidates.Add(
                            bidItem);
                    }
                }


                // ==============================
                // 11. 儲存所有 FinalBidItem
                //     取得 FinalBidItemId
                // ==============================

                _context.SaveChanges();

                // ==============================
                // 12. 建立 HistoricalTransaction
                // ==============================

                foreach (
                    var bidItem in historyCandidates)
                {
                    if (!bidItem.MaterialId.HasValue ||
                        !bidItem.Qty.HasValue ||
                        !bidItem.UnitPrice.HasValue)
                    {
                        continue;
                    }

                    var history =
                        new HistoricalTransaction
                        {
                            MaterialId =
                                bidItem.MaterialId.Value,

                            ActualQuantity =
                                bidItem.Qty.Value,

                            UnitPrice =
                                bidItem.UnitPrice.Value,

                            // 用建案日期，
                            // 不是上傳當天
                            TransactionDate =
                                project.StartDate!.Value,

                            IsDeleted =
                                false,

                            IsOutlier =
                                false,

                            ProjectMasterId =
                                project.ProjectId,

                            FinalBidItemId =
                                bidItem.FinalBidItemId
                        };

                    _context.HistoricalTransactions.Add(
                        history);
                }

                _context.SaveChanges();

                dbTransaction.Commit();

                return Ok(new
                {
                    message = "決標標單回寫成功",

                    projectId =
                        project.ProjectId,

                    projectName =
                        project.ProjectName,

                    projectType =
                        project.ProjectType,

                    structureType =
                        project.StructureType,

                    city =
                        project.City,

                    totalFloorArea =
                        project.TotalFloorArea,

                    floorCount =
                        project.FloorCount,

                    basementFloorCount =
                        project.BasementFloorCount,

                    projectDate =
                        project.StartDate,

                    sourceFileName =
                        project.SourceFileName,

                    totalCount =
                        rows.Count,

                    autoMappedCount =
                        autoMappedCount,

                    unmappedCount =
                        unmappedCount,

                    historyInsertCount =
                        historyCandidates.Count,

                    invalidDataCount =
                        invalidDataCount,

                    missingMaterialCount =
                        missingMaterialCount,

                    unitMismatchCount =
                        unitMismatchCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return BadRequest(new
                {
                    message = "決標標單回寫失敗：" +
                            (ex.InnerException?.Message ?? ex.Message)
                });
            }
        }

        private static string NormalizeImportText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsAllowedValue(string value, params string[] allowedValues)
        {
            return allowedValues.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        }

        // ==========================================
        // 決標標單工項查詢
        // 預設顯示尚未對應，可搜尋全部狀態
        // ==========================================
        [HttpGet("mapping-items")]
        public IActionResult GetMappingItems(
            [FromQuery] int? projectId = null,
            [FromQuery] string status = "Pending",
            [FromQuery] string? search = null,
            [FromQuery] string? issueCode = null,
            [FromQuery] string? category = null,
            [FromQuery] string? unit = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 50;

            if (pageSize > 200)
                pageSize = 200;

            var query =
                _context.FinalBidItems
                    .AsNoTracking()
                    .AsQueryable();

            // ==============================
            // 建案
            // ==============================

            if (projectId.HasValue)
            {
                query = query.Where(x =>
                    x.ProjectMasterId == projectId.Value);
            }

            // ==============================
            // 對應狀態
            //
            // Pending
            // Auto
            // Manual
            // All
            // ==============================

            status =
                string.IsNullOrWhiteSpace(status)
                    ? "Pending"
                    : status.Trim();

            if (!string.Equals(
                    status,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!IsAllowedValue(
                        status,
                        "Pending",
                        "Auto",
                        "Manual"))
                {
                    return BadRequest(new
                    {
                        message =
                            "狀態只允許 Pending、Auto、Manual、All"
                    });
                }

                query = query.Where(x =>
                    x.MappingStatus == status);
            }

            // ==============================
            // 待處理原因
            // ==============================

            if (!string.IsNullOrWhiteSpace(issueCode) &&
                !string.Equals(
                    issueCode,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedIssue =
                    issueCode.Trim();

                query = query.Where(x =>
                    x.MappingIssueCode == selectedIssue);
            }

            // ==============================
            // 材料分類
            // ==============================

            if (!string.IsNullOrWhiteSpace(category) &&
                !string.Equals(
                    category,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedCategory =
                    category.Trim();

                query = query.Where(x =>
                    x.Material != null &&
                    x.Material.Category != null &&
                    x.Material.Category ==
                        selectedCategory);
            }

            // ==============================
            // 單位
            // ==============================

            if (!string.IsNullOrWhiteSpace(unit) &&
                !string.Equals(
                    unit,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedUnit =
                    unit.Trim();

                query = query.Where(x =>
                    x.Unit != null &&
                    x.Unit == selectedUnit);
            }

            // ==============================
            // 關鍵字搜尋
            //
            // 工項名稱
            // 規格
            // 標準材料
            // 建案名稱
            // ==============================

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern =
                    $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.ItemName,
                        pattern)

                    ||

                    (
                        x.Spec != null &&
                        EF.Functions.ILike(
                            x.Spec,
                            pattern)
                    )

                    ||

                    (
                        x.Material != null &&
                        EF.Functions.ILike(
                            x.Material.MaterialName,
                            pattern)
                    )

                    ||

                    EF.Functions.ILike(
                        x.ProjectMaster.ProjectName,
                        pattern)
                );
            }

            int totalCount =
                query.Count();

            // ==============================
            // 排序
            //
            // 待處理優先
            // 手動其次
            // 自動最後
            // ==============================

            var items =
                query
                    .OrderBy(x =>
                        x.MappingStatus == "Pending"
                            ? 0
                            : x.MappingStatus == "Manual"
                                ? 1
                                : 2)

                    .ThenByDescending(
                        x => x.FinalBidItemId)

                    .Skip(
                        (page - 1) * pageSize)

                    .Take(pageSize)

                    .Select(x => new
                    {
                        finalBidItemId =
                            x.FinalBidItemId,

                        projectId =
                            x.ProjectMasterId,

                        projectName =
                            x.ProjectMaster.ProjectName,

                        sourceFileName =
                            x.SourceFileName,

                        sheetName =
                            x.SheetName,

                        rowNumber =
                            x.RowNumber,

                        itemCode =
                            x.ItemCode,

                        itemName =
                            x.ItemName,

                        spec =
                            x.Spec,

                        unit =
                            x.Unit,

                        qty =
                            x.Qty,

                        unitPrice =
                            x.UnitPrice,

                        materialId =
                            x.MaterialId,

                        materialName =
                            x.Material != null
                                ? x.Material.MaterialName
                                : null,

                        materialCategory =
                            x.Material != null
                                ? x.Material.Category
                                : null,

                        materialUnit =
                            x.Material != null
                                ? x.Material.BaseUnit
                                : null,

                        mappingStatus =
                            x.MappingStatus,

                        mappingIssueCode =
                            x.MappingIssueCode,

                        mappingIssueMessage =
                            x.MappingIssueMessage,

                        matchedKeyword =
                            x.MatchedKeyword,

                        updatedAt =
                            x.UpdatedAt
                    })

                    .ToList();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,

                filters = new
                {
                    status,
                    search,
                    issueCode,
                    category,
                    unit
                },

                items
            });
        }


        [HttpGet("mapping-filter-options")]
        public IActionResult GetMappingFilterOptions(
            [FromQuery] int? projectId = null)
        {
            var itemQuery =
                _context.FinalBidItems
                    .AsNoTracking()
                    .AsQueryable();

            if (projectId.HasValue)
            {
                itemQuery = itemQuery.Where(x =>
                    x.ProjectMasterId ==
                    projectId.Value);
            }

            var units =
                itemQuery
                    .Where(x =>
                        x.Unit != null &&
                        x.Unit != "")
                    .Select(x => x.Unit!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            var categories =
                _context.MaterialMasters
                    .AsNoTracking()
                    .Where(x =>
                        x.Category != null &&
                        x.Category != "")
                    .Select(x => x.Category!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            var issueCodes =
                itemQuery
                    .Where(x =>
                        x.MappingIssueCode != null)
                    .Select(x =>
                        x.MappingIssueCode!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            return Ok(new
            {
                units,
                categories,
                issueCodes
            });
        }
        
        // ==========================================
        // 取得標準材料清單
        // 供人工對應下拉選單使用
        // ==========================================
        [HttpGet("material-options")]
        public IActionResult GetMaterialOptions(
            [FromQuery] string? search = null)
        {
            var query =
                _context.MaterialMasters
                    .AsNoTracking()
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern =
                    $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.MaterialName,
                        pattern)
                    ||
                    (
                        x.Category != null &&
                        EF.Functions.ILike(
                            x.Category,
                            pattern)
                    )
                );
            }

            var materials =
                query
                    .OrderBy(x => x.MaterialName)
                    .Select(x => new
                    {
                        materialId =
                            x.MaterialId,

                        materialName =
                            x.MaterialName,

                        category =
                            x.Category,

                        baseUnit =
                            x.BaseUnit
                    })
                    .Take(200)
                    .ToList();

            return Ok(materials);
        }

        // ==========================================
        // 人工指定 / 修改材料對應
        //
        // 支援：
        // Unmapped → Manual
        // Auto     → Manual
        // Manual   → Manual
        // ==========================================
        [HttpPost("update-material-mapping")]
        public IActionResult UpdateMaterialMapping(
            [FromForm] long finalBidItemId,
            [FromForm] int materialId)
        {
            using var dbTransaction =
                _context.Database.BeginTransaction();

            try
            {
                // ==============================
                // 1. 找標單工項
                // ==============================

                var bidItem =
                    _context.FinalBidItems
                        .Include(x => x.ProjectMaster)
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                            finalBidItemId);

                if (bidItem == null)
                {
                    return NotFound(new
                    {
                        message =
                            "找不到此決標標單工項"
                    });
                }

                // ==============================
                // 2. 找標準材料
                // ==============================

                var material =
                    _context.MaterialMasters
                        .FirstOrDefault(x =>
                            x.MaterialId ==
                            materialId);

                if (material == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "找不到指定的標準材料"
                    });
                }

                // ==============================
                // 3. 必須有有效數量與單價
                // ==============================

                if (!bidItem.Qty.HasValue ||
                    bidItem.Qty.Value <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "此工項數量無效，不能建立歷史交易"
                    });
                }

                if (!bidItem.UnitPrice.HasValue ||
                    bidItem.UnitPrice.Value < 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "此工項單價無效，不能建立歷史交易"
                    });
                }

                // ==============================
                // 4. 單位必須一致
                // ==============================

                if (!IsSameUnit(
                        bidItem.Unit ?? "",
                        material.BaseUnit))
                {
                    return BadRequest(new
                    {
                        message =
                            $"單位不符：標單單位為「{bidItem.Unit}」，" +
                            $"標準材料單位為「{material.BaseUnit}」"
                    });
                }

                int? oldMaterialId =
                    bidItem.MaterialId;

                string oldStatus =
                    bidItem.MappingStatus;

                // ==============================
                // 5. 建立 / 修正字典
                // ==============================

                string keyword =
                    !string.IsNullOrWhiteSpace(
                        bidItem.ItemName)
                        ? bidItem.ItemName.Trim()
                        : bidItem.Spec?.Trim()
                            ?? string.Empty;

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(new
                    {
                        message =
                            "工項名稱與規格皆為空，無法建立字典"
                    });
                }

                /*
                * 如果以前手動配錯：
                *
                * keyword → material 15
                *
                * 現在改成 material 18，
                * 要同步修正既有字典。
                */
                var existingDictionaryRows =
                    _context.ItemDictionaries
                        .Where(x =>
                            x.Keyword == keyword)
                        .ToList();

                if (existingDictionaryRows.Count == 0)
                {
                    _context.ItemDictionaries.Add(
                        new ItemDictionary
                        {
                            Keyword =
                                keyword,

                            Material_Id =
                                material.MaterialId
                        });
                }
                else
                {
                    foreach (
                        var dictionaryRow
                        in existingDictionaryRows)
                    {
                        dictionaryRow.Material_Id =
                            material.MaterialId;
                    }
                }

                // ==============================
                // 6. 更新 FinalBidItem
                // ==============================

                bidItem.MaterialId =
                    material.MaterialId;

                bidItem.MappingStatus =
                    "Manual";

                bidItem.MappingIssueCode =
                    null;

                bidItem.MappingIssueMessage =
                    null;

                bidItem.MatchedKeyword =
                    keyword;

                bidItem.UpdatedAt =
                    DateTime.UtcNow;

                bidItem.UpdatedAt =
                    DateTime.UtcNow;

                // ==============================
                // 7. 同步 HistoricalTransaction
                // ==============================

                var history =
                    _context.HistoricalTransactions
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                            bidItem.FinalBidItemId);

                string historyAction;

                if (history == null)
                {
                    // 原本是 Unmapped，
                    // 現在第一次人工對應成功
                    history =
                        new HistoricalTransaction
                        {
                            MaterialId =
                                material.MaterialId,

                            ActualQuantity =
                                bidItem.Qty.Value,

                            UnitPrice =
                                bidItem.UnitPrice.Value,

                            TransactionDate =
                                bidItem.ProjectMaster
                                    .StartDate
                                    ?? DateTime.UtcNow.Date,

                            IsDeleted =
                                false,

                            IsOutlier =
                                false,

                            ProjectMasterId =
                                bidItem.ProjectMasterId,

                            FinalBidItemId =
                                bidItem.FinalBidItemId
                        };

                    _context.HistoricalTransactions
                        .Add(history);

                    historyAction =
                        "新增歷史交易";
                }
                else
                {
                    // 原本 Auto 或 Manual，
                    // 現在人工改成另一個材料
                    history.MaterialId =
                        material.MaterialId;

                    history.ActualQuantity =
                        bidItem.Qty.Value;

                    history.UnitPrice =
                        bidItem.UnitPrice.Value;

                    history.ProjectMasterId =
                        bidItem.ProjectMasterId;

                    history.TransactionDate =
                        bidItem.ProjectMaster
                            .StartDate
                            ?? history.TransactionDate;

                    history.IsDeleted =
                        false;

                    historyAction =
                        "更新既有歷史交易";
                }

                // ==============================
                // 8. Audit Log
                // ==============================

                var log =
                    new AuditLog
                    {
                        SourceFileName =
                            bidItem.SourceFileName,

                        SheetName =
                            bidItem.SheetName ?? string.Empty,

                        RowNumber =
                            bidItem.RowNumber ?? 0,

                        ItemName =
                            bidItem.ItemName,

                        OriginalStatusMessage =
                            $"原狀態：{oldStatus}；" +
                            $"原 MaterialId：" +
                            $"{oldMaterialId?.ToString() ?? "NULL"}",

                        OverrideReason =
                            $"人工修改材料對應為：" +
                            $"{material.MaterialName} " +
                            $"(MaterialId={material.MaterialId})",

                        ActionUser =
                            User?.Identity?.Name
                            ?? "Estimator_Staff",

                        CreatedAt =
                            DateTime.UtcNow
                    };

                _context.AuditLogs.Add(log);

                // ==============================
                // 9. 全部一起存
                // ==============================

                _context.SaveChanges();

                dbTransaction.Commit();

                return Ok(new
                {
                    message =
                        $"「{bidItem.ItemName}」已人工對應至「{material.MaterialName}」",

                    finalBidItemId =
                        bidItem.FinalBidItemId,

                    oldMaterialId,

                    newMaterialId =
                        material.MaterialId,

                    materialName =
                        material.MaterialName,

                    mappingStatus =
                        bidItem.MappingStatus,

                    historyAction
                });
            }
            catch (Exception ex)
            {
                dbTransaction.Rollback();

                return BadRequest(new
                {
                    message =
                        "材料對應更新失敗：" +
                        ex.Message
                });
            }
        }
    }

}
