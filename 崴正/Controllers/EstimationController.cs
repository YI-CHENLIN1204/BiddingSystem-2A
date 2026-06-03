using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;
using Microsoft.AspNetCore.Authorization;

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
                .Where(h => materialIds.Contains(h.Material_ID))
                .Where(h => h.IsDeleted == false)
                .Where(h => h.Is_Outlier == false)
                .Where(h => h.Transaction_Date >= longTermStartDate)
                .ToList();

            // 4. 依照材料 ID 分組，後面直接從記憶體拿
            var historyDict = allHistories
                .GroupBy(h => h.Material_ID)
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
                    .Where(h => h.Transaction_Date >= shortTermStartDate)
                    .ToList();

                var shortPriceValues = shortHistories.Select(h => h.Unit_Price).ToList();
                var longPriceValues = longHistories.Select(h => h.Unit_Price).ToList();

                int sampleCount = longHistories
                    .Where(h => !string.IsNullOrWhiteSpace(h.Project_Id))
                    .Select(h => h.Project_Id)
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

        
        // 🚀 UC-05 & UC-06：匯入最終決標標單 & 回寫歷史資料
        // ==========================================
        [HttpPost("upload-final-bid")]
        public IActionResult UploadFinalBid(IFormFile excelFile, [FromForm] string projectName)
        {
            if (excelFile == null || excelFile.Length == 0)
                return BadRequest(new { message = "請先選擇 Excel 檔案" });

            if (string.IsNullOrWhiteSpace(projectName))
                return BadRequest(new { message = "請輸入此決標案的工程專案名稱" });

            try
            {
                var parser = new TenderExcelParser();
                using var stream = excelFile.OpenReadStream();
                var rows = parser.Parse(stream, excelFile.FileName);

                var dictionaryList = _context.ItemDictionaries.ToList();
                var materials = _context.MaterialMasters.ToList();
                var materialDict = materials.ToDictionary(m => m.MaterialId, m => m);

                int successInsertCount = 0;
                int unmappedCount = 0;
                int invalidDataCount = 0;
                int missingMaterialCount = 0;
                int unitMismatchCount = 0;

                // 避免同一次匯入大量重複寫入 unmapped_items
                var existingUnmappedKeys = _context.UnmappedItems
                    .Where(x => x.ProjectName == projectName && x.Status == "待處理")
                    .Select(x => (x.ItemName ?? "") + "|" + (x.Spec ?? ""))
                    .ToHashSet();

                foreach (var row in rows)
                {
                    if (row.Qty == null || row.Qty <= 0 || row.UnitPrice == null || row.UnitPrice < 0)
                    {
                        invalidDataCount++;
                        continue;
                    }

                    var matchedDict = dictionaryList.FirstOrDefault(d =>
                        (row.ItemName ?? "").Contains(d.Keyword, StringComparison.OrdinalIgnoreCase) ||
                        (row.Spec ?? "").Contains(d.Keyword, StringComparison.OrdinalIgnoreCase));

                    if (matchedDict == null)
                    {
                        unmappedCount++;

                        var itemName = row.ItemName ?? "";
                        var spec = row.Spec ?? "";
                        var key = itemName + "|" + spec;

                        if (!existingUnmappedKeys.Contains(key))
                        {
                            _context.UnmappedItems.Add(new UnmappedItem
                            {
                                SourceFileName = excelFile.FileName,
                                ProjectName = projectName,
                                ItemName = itemName,
                                Spec = row.Spec,
                                Unit = row.Unit,
                                Qty = row.Qty,
                                UnitPrice = row.UnitPrice,
                                Status = "待處理"
                            });

                            existingUnmappedKeys.Add(key);
                        }

                        continue;
                    }

                    if (!materialDict.TryGetValue(matchedDict.Material_Id, out var material))
                    {
                        missingMaterialCount++;
                        continue;
                    }

                    if (!IsSameUnit(row.Unit, material.BaseUnit))
                    {
                        unitMismatchCount++;
                        continue;
                    }

                    var newHistory = new HistoricalTransaction
                    {
                        Material_ID = material.MaterialId,
                        Actual_Quantity = row.Qty.Value,
                        Unit_Price = row.UnitPrice.Value,
                        Transaction_Date = DateTime.UtcNow.Date,
                        IsDeleted = false,
                        Is_Outlier = false,
                        Project_Id = projectName
                    };

                    _context.HistoricalTransactions.Add(newHistory);
                    successInsertCount++;
                }

                _context.SaveChanges();

                return Ok(new
                {
                    message = "回寫成功",
                    insertCount = successInsertCount,
                    unmappedCount = unmappedCount,
                    invalidDataCount = invalidDataCount,
                    missingMaterialCount = missingMaterialCount,
                    unitMismatchCount = unitMismatchCount,
                    totalCount = rows.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "回寫失敗：" + ex.Message });
            }
        }

        [HttpGet("unmapped-items")]
        public IActionResult GetUnmappedItems()
        {
            var items = _context.UnmappedItems
                .Where(x => x.Status == "待處理")
                .OrderByDescending(x => x.UnmappedId)
                .Take(20)
                .Select(x => new
                {
                    unmappedId = x.UnmappedId,
                    projectName = x.ProjectName,
                    itemName = x.ItemName,
                    spec = x.Spec,
                    unit = x.Unit,
                    qty = x.Qty,
                    unitPrice = x.UnitPrice
                })
                .ToList();

            var materials = _context.MaterialMasters
                .OrderBy(x => x.MaterialName)
                .Select(x => new
                {
                    materialId = x.MaterialId,
                    materialName = x.MaterialName
                })
                .ToList();

            return Ok(new
            {
                items,
                materials
            });
        }

        [HttpPost("add-dictionary")]
        public IActionResult AddDictionary([FromForm] int unmappedId, [FromForm] int materialId)
        {
            var unmapped = _context.UnmappedItems.FirstOrDefault(x => x.UnmappedId == unmappedId);

            if (unmapped == null)
            {
                return NotFound(new { message = "找不到未對應工項" });
            }

            var material = _context.MaterialMasters.FirstOrDefault(x => x.MaterialId == materialId);

            if (material == null)
            {
                return BadRequest(new { message = "找不到標準工項" });
            }

            var keyword = unmapped.ItemName.Trim();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "工項名稱不可為空" });
            }

            bool exists = _context.ItemDictionaries.Any(x =>
                x.Keyword == keyword &&
                x.Material_Id == materialId);

            if (!exists)
            {
                _context.ItemDictionaries.Add(new ItemDictionary
                {
                    Keyword = keyword,
                    Material_Id = materialId
                });
            }

            unmapped.Status = "已加入字典";
            unmapped.SuggestedMaterialId = materialId;

            _context.SaveChanges();

            return Ok(new
            {
                message = $"已將「{keyword}」加入字典，對應至「{material.MaterialName}」"
            });
        }

    }

}


