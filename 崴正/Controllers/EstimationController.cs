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

        // 新版：直接上傳 Excel，不讀歷史資料庫，不儲存本次標單，只針對本次 Excel 內部資料做異常偵測。
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

                var results = AnalyzeRowsWithoutHistory(rows);

                return Ok(new TenderAnomalyResponseDto
                {
                    TotalCount = results.Count,
                    AbnormalCount = results.Count(x => x.IsAbnormal),
                    Results = results
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Excel 解析失敗：" + ex.Message });
            }
        }

        private static List<TenderAnomalyResultDto> AnalyzeRowsWithoutHistory(List<TenderImportRowDto> rows)
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
                StatusMessage = "正常"
            }).ToList();

            foreach (var item in results)
            {
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

                if (reasons.Any())
                {
                    item.IsAbnormal = true;
                    item.StatusMessage = string.Join("、", reasons);
                }
            }

            ApplyZScoreByUnit(results, x => x.Qty, (x, z) => x.QtyZScore = z, "數量偏離本次標單同單位資料");
            ApplyZScoreByUnit(results, x => x.UnitPrice, (x, z) => x.UnitPriceZScore = z, "單價偏離本次標單同單位資料");

            return results;
        }

        private static void ApplyZScoreByUnit(
            List<TenderAnomalyResultDto> results,
            Func<TenderAnomalyResultDto, decimal?> valueSelector,
            Action<TenderAnomalyResultDto, decimal> zSetter,
            string reason)
        {
            var groups = results
                .Where(x => !string.IsNullOrWhiteSpace(x.Unit) && valueSelector(x) != null)
                .GroupBy(x => x.Unit.Trim());

            foreach (var group in groups)
            {
                var values = group.Select(x => valueSelector(x)!.Value).ToList();
                if (values.Count < 3) continue;

                decimal mean = values.Average();
                double variance = values.Average(v => Math.Pow((double)(v - mean), 2));
                decimal sd = (decimal)Math.Sqrt(variance);
                if (sd == 0) continue;

                foreach (var item in group)
                {
                    decimal value = valueSelector(item)!.Value;
                    decimal z = Math.Round(Math.Abs((value - mean) / sd), 2);
                    zSetter(item, z);

                    if (z >= 2.0m)
                    {
                        item.IsAbnormal = true;
                        item.StatusMessage = item.StatusMessage == "正常" || string.IsNullOrWhiteSpace(item.StatusMessage)
                            ? $"{reason}，Z-Score={z}"
                            : item.StatusMessage + $"、{reason}，Z-Score={z}";
                    }
                }
            }
        }
    }
}
