using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;

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
                    .Where(h => h.Material_ID == item.Material_Id && h.Is_Outlier == false)
                    .Select(h => new { h.Actual_Quantity, h.Unit_Price, h.Transaction_Date })
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
                        int daysOld = (today - h.Transaction_Date).Days;
                        double weight = Math.Exp(-k * daysOld);
                        totalWeight += weight;
                        weightedSum += (double)h.Actual_Quantity * weight;
                        return new { h.Actual_Quantity, weight };
                    }).ToList();

                    double weightedMean = weightedSum / totalWeight;
                    suggestedQty = (decimal)weightedMean;

                    double varianceSum = weightedData.Sum(d => d.weight * Math.Pow((double)d.Actual_Quantity - weightedMean, 2));
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

                    predictedPrice = historyData.Average(h => h.Unit_Price) * 1.02m;
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

        // 新版：上傳 Excel 後，不儲存本次標單；只針對「磁磚、混凝土、鋼筋」查資料庫歷史資料計算 Z 分數。
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
                var parser = new TenderExcelParser();
                List<TenderImportRowDto> rows;

                using (var stream = excelFile.OpenReadStream())
                {
                    rows = parser.Parse(stream, excelFile.FileName);
                }

                var results = AnalyzeRowsWithHistory(rows);

                return Ok(new TenderAnomalyResponseDto
                {
                    TotalCount = results.Count,
                    TargetCount = results.Count(x => x.IsTargetMaterial),
                    AbnormalCount = results.Count(x => x.IsAbnormal),
                    Results = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Excel 解析或歷史資料比對失敗：" + ex.Message });
            }
        }

        private List<TenderAnomalyResultDto> AnalyzeRowsWithHistory(List<TenderImportRowDto> rows)
        {
            var results = rows.Select(r => new TenderAnomalyResultDto
            {
                SourceFileName = r.SourceFileName,
                SheetName = r.SheetName,
                RowNumber = r.RowNumber,
                ItemCode = r.ItemCode,
                Category1 = r.Category1,
                Category2 = r.Category2,
                Category3 = r.Category3,
                ItemName = r.ItemName,
                Spec = r.Spec,
                Unit = r.Unit,
                Qty = r.Qty,
                UnitPrice = r.UnitPrice,
                Amount = r.Amount,
                Note = r.Note,
                IsAbnormal = false,
                IsTargetMaterial = false,
                StatusMessage = "非目前檢測材料"
            }).ToList();

            var targetMaterialNames = new[] { "磁磚", "混凝土", "鋼筋" };

            var materials = _context.MaterialMasters
                .Where(m => targetMaterialNames.Contains(m.MaterialName))
                .ToList();

            foreach (var item in results)
            {
                var matchedName = MatchTargetMaterialName(item.ItemName);

                if (matchedName == null)
                {
                    item.StatusMessage = "非目前檢測材料（僅檢測磁磚、混凝土、鋼筋）";
                    continue;
                }

                item.IsTargetMaterial = true;
                item.MatchedMaterialName = matchedName;

                var material = materials.FirstOrDefault(m => m.MaterialName == matchedName);
                if (material == null)
                {
                    item.IsAbnormal = true;
                    item.StatusMessage = $"資料庫找不到材料主檔：{matchedName}";
                    continue;
                }

                if (!IsSameUnit(item.Unit, material.BaseUnit))
                {
                    item.IsAbnormal = true;
                    item.StatusMessage = $"單位不符，資料庫標準單位為 {material.BaseUnit}";
                    continue;
                }

                var reasons = new List<string>();

                if (item.Qty == null || item.Qty <= 0)
                    reasons.Add("數量空白或小於等於 0");

                if (item.UnitPrice == null || item.UnitPrice < 0)
                    reasons.Add("單價空白或小於 0");

                if (item.Amount != null && item.Amount < 0)
                    reasons.Add("複價小於 0");

                if (item.Qty != null && item.UnitPrice != null)
                {
                    item.ExpectedAmount = Math.Round(item.Qty.Value * item.UnitPrice.Value, 2);

                    if (item.Amount != null)
                    {
                        item.AmountDifference = Math.Round(item.Amount.Value - item.ExpectedAmount.Value, 2);
                        var tolerance = Math.Max(1m, Math.Abs(item.ExpectedAmount.Value) * 0.05m);

                        if (Math.Abs(item.AmountDifference.Value) > tolerance)
                            reasons.Add("複價與 數量×單價 不一致");
                    }
                }

                var histories = _context.HistoricalTransactions
                    .Where(h => h.Material_ID == material.MaterialId)
                    .Where(h => h.IsDeleted == false)
                    .Where(h => h.Is_Outlier == false)
                    .Select(h => new
                    {
                        h.Actual_Quantity,
                        h.Unit_Price
                    })
                    .ToList();

                if (histories.Count < 2)
                {
                    reasons.Add("歷史資料不足，無法計算 Z 分數");
                    item.IsAbnormal = reasons.Any(r => r != "歷史資料不足，無法計算 Z 分數");
                    item.StatusMessage = reasons.Any() ? string.Join("、", reasons) : "歷史資料不足，無法計算 Z 分數";
                    continue;
                }

                var qtyValues = histories.Select(h => h.Actual_Quantity).ToList();
                var priceValues = histories.Select(h => h.Unit_Price).ToList();

                item.HistoricalAvgQty = Math.Round(qtyValues.Average(), 2);
                item.HistoricalStdQty = Math.Round(CalculateStdDev(qtyValues), 2);
                item.HistoricalAvgUnitPrice = Math.Round(priceValues.Average(), 2);
                item.HistoricalStdUnitPrice = Math.Round(CalculateStdDev(priceValues), 2);

                decimal threshold = GetZScoreThreshold(material.MaterialId);

                if (item.Qty != null && item.HistoricalStdQty > 0)
                {
                    item.QtyZScore = Math.Round(Math.Abs((item.Qty.Value - item.HistoricalAvgQty.Value) / item.HistoricalStdQty.Value), 2);
                    if (item.QtyZScore >= threshold)
                        reasons.Add($"數量與歷史資料差異過大，Z={item.QtyZScore}");
                }
                else if (item.HistoricalStdQty == 0)
                {
                    reasons.Add("歷史數量標準差為 0，無法計算數量 Z 分數");
                }

                if (item.UnitPrice != null && item.HistoricalStdUnitPrice > 0)
                {
                    item.UnitPriceZScore = Math.Round(Math.Abs((item.UnitPrice.Value - item.HistoricalAvgUnitPrice.Value) / item.HistoricalStdUnitPrice.Value), 2);
                    if (item.UnitPriceZScore >= threshold)
                        reasons.Add($"單價與歷史資料差異過大，Z={item.UnitPriceZScore}");
                }
                else if (item.HistoricalStdUnitPrice == 0)
                {
                    reasons.Add("歷史單價標準差為 0，無法計算單價 Z 分數");
                }

                item.IsAbnormal = reasons.Any(r =>
                    !r.Contains("標準差為 0") &&
                    !r.Contains("歷史資料不足"));

                item.StatusMessage = reasons.Any() ? string.Join("、", reasons) : "正常";
            }

            return results;
        }

        private static string? MatchTargetMaterialName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return null;

            var text = itemName.Trim();

            if (text.Contains("磁磚"))
                return "磁磚";

            if (text.Contains("混凝土") || text.Contains("CLSM", StringComparison.OrdinalIgnoreCase))
                return "混凝土";

            if (text.Contains("鋼筋"))
                return "鋼筋";

            return null;
        }

        private static bool IsSameUnit(string excelUnit, string dbUnit)
        {
            return string.Equals(
                NormalizeUnit(excelUnit),
                NormalizeUnit(dbUnit),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return "";

            return unit.Trim()
                .Replace("㎡", "m2")
                .Replace("平方公尺", "m2")
                .Replace("立方公尺", "m3")
                .ToLowerInvariant();
        }

        private decimal GetZScoreThreshold(int materialId)
        {
            var threshold = _context.QuantityRuleBases
                .Where(r => r.Material_ID == materialId)
                .Select(r => r.Z_Score_Threshold)
                .FirstOrDefault();

            return threshold > 0 ? threshold : 2.0m;
        }

        private static decimal CalculateStdDev(List<decimal> values)
        {
            if (values == null || values.Count < 2)
                return 0;

            decimal avg = values.Average();
            double variance = values.Average(v => Math.Pow((double)(v - avg), 2));
            return (decimal)Math.Sqrt(variance);
        }
    }
}
