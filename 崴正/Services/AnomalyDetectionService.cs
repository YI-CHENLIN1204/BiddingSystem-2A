using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class AnomalyDetectionService
    {
        private readonly ApplicationDbContext _context;

        public AnomalyDetectionService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 原 JSON 異常分析
        // ==========================================
        public EstimationResponseDto AnalyzeBid(
            BidRequestDto request)
        {
            var finalReport =
                new EstimationResponseDto
                {
                    Project_Id =
                        request.Project_Id
                };

            var groupedItems =
                request.Items
                    .GroupBy(x => new
                    {
                        x.Material_Id,
                        x.Floor_Zone
                    })
                    .Select(g => new
                    {
                        Material_Id =
                            g.Key.Material_Id,

                        Floor_Zone =
                            g.Key.Floor_Zone,

                        Total_Quantity =
                            g.Sum(x =>
                                x.Input_Quantity)
                    })
                    .ToList();

            foreach (var item in groupedItems)
            {
                var rule =
                    _context.QuantityRuleBases
                        .FirstOrDefault(r =>
                            r.Material_ID ==
                            item.Material_Id);

                decimal ruleThreshold =
                    rule != null
                        ? rule.Z_Score_Threshold
                        : 2.0m;

                var historyData =
                    _context.HistoricalTransactions
                        .Where(h =>
                            h.MaterialId ==
                            item.Material_Id &&
                            h.IsOutlier == false)
                        .Select(h => new
                        {
                            h.ActualQuantity,
                            h.UnitPrice,
                            h.TransactionDate
                        })
                        .ToList();

                decimal suggestedQty = 0;
                decimal calculatedZScore = 0;
                bool isAbnormal = false;
                string statusMsg = "";
                decimal predictedPrice = 0;

                if (!historyData.Any())
                {
                    statusMsg =
                        rule != null
                            ? "需人工首筆核定"
                            : "無基準資料";

                    suggestedQty =
                        item.Total_Quantity;

                    isAbnormal = true;
                }
                else
                {
                    double k = 0.005;
                    DateTime today =
                        DateTime.Today;

                    double totalWeight = 0;
                    double weightedSum = 0;

                    var weightedData =
                        historyData.Select(h =>
                        {
                            int daysOld =
                                (today -
                                 h.TransactionDate)
                                .Days;

                            double weight =
                                Math.Exp(
                                    -k * daysOld
                                );

                            totalWeight +=
                                weight;

                            weightedSum +=
                                (double)h.ActualQuantity *
                                weight;

                            return new
                            {
                                h.ActualQuantity,
                                weight
                            };
                        })
                        .ToList();

                    double weightedMean =
                        weightedSum /
                        totalWeight;

                    suggestedQty =
                        (decimal)weightedMean;

                    double varianceSum =
                        weightedData.Sum(d =>
                            d.weight *
                            Math.Pow(
                                (double)d.ActualQuantity -
                                weightedMean,
                                2
                            )
                        );

                    decimal historySD =
                        (decimal)Math.Sqrt(
                            varianceSum /
                            totalWeight
                        );

                    if (historySD == 0)
                    {
                        calculatedZScore = 0;
                        isAbnormal = false;

                        statusMsg =
                            "資料一致 (無波動)";
                    }
                    else
                    {
                        calculatedZScore =
                            Math.Abs(
                                (
                                    item.Total_Quantity -
                                    (decimal)weightedMean
                                ) /
                                historySD
                            );

                        isAbnormal =
                            calculatedZScore >
                            ruleThreshold;

                        statusMsg =
                            isAbnormal
                                ? "異常高估"
                                : "合理";
                    }

                    predictedPrice =
                        historyData
                            .Average(h =>
                                h.UnitPrice) *
                        1.02m;
                }

                decimal refTotalPrice =
                    item.Total_Quantity *
                    predictedPrice;

                finalReport.Results.Add(
                    new EstimationResultDto
                    {
                        Material_Id =
                            item.Material_Id,

                        Floor_Zone =
                            item.Floor_Zone,

                        Calculated_Z_Score =
                            Math.Round(
                                calculatedZScore,
                                2
                            ),

                        Is_Abnormal =
                            isAbnormal,

                        Predicted_Unit_Price =
                            Math.Round(
                                predictedPrice,
                                0
                            ),

                        Reference_Total_Price =
                            Math.Round(
                                refTotalPrice,
                                0
                            ),

                        Suggested_Quantity =
                            Math.Round(
                                suggestedQty,
                                2
                            ),

                        Status_Message =
                            statusMsg
                    }
                );
            }

            return finalReport;
        }

        // ==========================================
        // Excel 異常分析
        // ==========================================
        public ServiceResult AnalyzeExcel(
            Stream excelStream,
            string fileName)
        {
            try
            {
                var start =
                    DateTime.Now;

                var parser =
                    new TenderExcelParser();

                var rows =
                    parser.Parse(
                        excelStream,
                        fileName
                    );

                var afterParse =
                    DateTime.Now;

                var results =
                    AnalyzeRowsWithHistory(
                        rows
                    );

                var afterAnalyze =
                    DateTime.Now;

                Console.WriteLine(
                    $"Excel解析耗時：{(afterParse - start).TotalSeconds} 秒"
                );

                Console.WriteLine(
                    $"異常偵測耗時：{(afterAnalyze - afterParse).TotalSeconds} 秒"
                );

                Console.WriteLine(
                    $"總耗時：{(afterAnalyze - start).TotalSeconds} 秒"
                );

                Console.WriteLine(
                    $"解析筆數：{rows.Count}，回傳筆數：{results.Count}"
                );

                return ServiceResult.Ok(
                    new TenderAnomalyResponseDto
                    {
                        TotalCount =
                            results.Count,

                        AbnormalCount =
                            results.Count(x =>
                                x.IsAbnormal),

                        Results =
                            results
                    }
                );
            }
            catch (Exception ex)
            {
                return ServiceResult.Validation(
                    "Excel 解析或異常偵測失敗：" +
                    ex.Message
                );
            }
        }

        // ==========================================
        // 人工推翻異常判定
        // ==========================================
        public ServiceResult OverrideAnomaly(
            OverrideRequestDto request,
            string actionUser)
        {
            if (string.IsNullOrWhiteSpace(
                    request.OverrideReason))
            {
                return ServiceResult.Validation(
                    "⚠️ 系統阻擋：必須強制填寫推翻理由！"
                );
            }

            var log =
                new AuditLog
                {
                    SourceFileName =
                        request.SourceFileName,

                    SheetName =
                        request.SheetName,

                    RowNumber =
                        request.RowNumber,

                    ItemName =
                        request.ItemName,

                    OriginalStatusMessage =
                        request.OriginalStatusMessage,

                    OverrideReason =
                        request.OverrideReason,

                    ActionUser =
                        string.IsNullOrWhiteSpace(
                            actionUser)
                            ? "Estimator_Staff"
                            : actionUser,

                    CreatedAt =
                        DateTime.UtcNow
                };

            try
            {
                _context.AuditLogs.Add(
                    log
                );

                _context.SaveChanges();

                return ServiceResult.Ok(
                    new
                    {
                        message =
                            "✅ 成功寫入 Audit Log 稽核軌跡！",

                        status =
                            "人工認可"
                    }
                );
            }
            catch (Exception ex)
            {
                return ServiceResult.Validation(
                    "Audit Log 寫入失敗：" +
                    ex.Message
                );
            }
        }

        // ==========================================
        // 使用歷史資料進行異常分析
        // ==========================================
        private List<TenderAnomalyResultDto>
            AnalyzeRowsWithHistory(
                List<TenderImportRowDto> rows)
        {
            var results =
                new List<TenderAnomalyResultDto>();

            var today =
                DateTime.UtcNow.Date;

            var shortTermStartDate =
                today.AddDays(-30);

            var longTermStartDate =
                today.AddDays(-180);

            const decimal shortTermWeight =
                0.7m;

            const decimal longTermWeight =
                0.3m;

            const int minimumSampleCount =
                10;

            var dictionaryList =
                _context.ItemDictionaries
                    .ToList();

            var materials =
                _context.MaterialMasters
                    .ToList();

            var materialDict =
                materials.ToDictionary(
                    m => m.MaterialId,
                    m => m
                );

            var materialIds =
                materials
                    .Select(m =>
                        m.MaterialId)
                    .ToList();

            var ruleDict =
                _context.QuantityRuleBases
                    .Where(r =>
                        materialIds.Contains(
                            r.Material_ID))
                    .ToDictionary(
                        r => r.Material_ID,
                        r => r.Z_Score_Threshold
                    );

            var allHistories =
                _context.HistoricalTransactions
                    .Where(h =>
                        materialIds.Contains(
                            h.MaterialId))
                    .Where(h =>
                        h.IsDeleted == false)
                    .Where(h =>
                        h.IsOutlier == false)
                    .Where(h =>
                        h.TransactionDate >=
                        longTermStartDate)
                    .ToList();

            var historyDict =
                allHistories
                    .GroupBy(h =>
                        h.MaterialId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

            foreach (var row in rows)
            {
                var result =
                    new TenderAnomalyResultDto
                    {
                        SourceFileName =
                            row.SourceFileName,

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
                            row.ItemName,

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

                        StatusMessage =
                            "正常"
                    };

                var reasons =
                    new List<string>();

                if (result.Qty == null ||
                    result.Qty <= 0)
                {
                    reasons.Add(
                        "數量空白或小於等於 0"
                    );
                }

                if (result.UnitPrice == null ||
                    result.UnitPrice < 0)
                {
                    reasons.Add(
                        "單價空白或小於 0"
                    );
                }

                if (result.Amount != null &&
                    result.Amount < 0)
                {
                    reasons.Add(
                        "複價小於 0"
                    );
                }

                bool hasCriticalDataProblem =
                    string.IsNullOrWhiteSpace(
                        result.Unit) ||
                    result.Qty == null ||
                    result.Qty <= 0 ||
                    result.UnitPrice == null ||
                    result.UnitPrice < 0;

                if (hasCriticalDataProblem)
                {
                    ClearZScores(result);

                    result.StatusMessage =
                        "資料不完整：" +
                        string.Join(
                            "、",
                            reasons
                        );

                    results.Add(result);

                    continue;
                }

                if (result.Qty != null &&
                    result.UnitPrice != null)
                {
                    result.ExpectedAmount =
                        Math.Round(
                            result.Qty.Value *
                            result.UnitPrice.Value,
                            2
                        );

                    if (result.Amount != null)
                    {
                        result.AmountDifference =
                            Math.Round(
                                result.Amount.Value -
                                result.ExpectedAmount.Value,
                                2
                            );

                        var tolerance =
                            Math.Max(
                                1m,
                                Math.Abs(
                                    result.ExpectedAmount.Value
                                ) * 0.05m
                            );

                        if (Math.Abs(
                                result.AmountDifference.Value
                            ) > tolerance)
                        {
                            reasons.Add(
                                "複價與 數量×單價 不一致"
                            );
                        }
                    }
                }

                var matchedDict =
                    dictionaryList
                        .FirstOrDefault(d =>
                            (
                                result.ItemName ??
                                ""
                            )
                            .Contains(
                                d.Keyword,
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            (
                                result.Spec ??
                                ""
                            )
                            .Contains(
                                d.Keyword,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );

                if (matchedDict == null)
                {
                    ClearZScores(result);

                    result.StatusMessage =
                        "待對應 (系統字典無法辨識此工項)";

                    results.Add(result);

                    continue;
                }

                if (!materialDict.TryGetValue(
                        matchedDict.Material_Id,
                        out var material))
                {
                    result.IsAbnormal =
                        true;

                    result.StatusMessage =
                        "系統錯誤：字典對應到不存在的材料ID";

                    results.Add(result);

                    continue;
                }

                result.MatchedMaterialName =
                    material.MaterialName;

                result.Category =
                    material.Category;

                if (!IsSameUnit(
                        result.Unit,
                        material.BaseUnit))
                {
                    ClearZScores(result);

                    if (reasons.Any())
                    {
                        result.StatusMessage =
                            string.Join(
                                "、",
                                reasons
                            ) +
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

                decimal ruleThreshold =
                    2.0m;

                if (ruleDict.TryGetValue(
                        material.MaterialId,
                        out var threshold) &&
                    threshold > 0)
                {
                    ruleThreshold =
                        threshold;
                }

                var longHistories =
                    historyDict.TryGetValue(
                        material.MaterialId,
                        out var histories)
                        ? histories
                        : new List<HistoricalTransaction>();

                var shortHistories =
                    longHistories
                        .Where(h =>
                            h.TransactionDate >=
                            shortTermStartDate)
                        .ToList();

                var shortPriceValues =
                    shortHistories
                        .Select(h =>
                            h.UnitPrice)
                        .ToList();

                var longPriceValues =
                    longHistories
                        .Select(h =>
                            h.UnitPrice)
                        .ToList();

                int sampleCount =
                    longHistories
                        .Where(h =>
                            h.ProjectMasterId.HasValue)
                        .Select(h =>
                            h.ProjectMasterId)
                        .Distinct()
                        .Count();

                if (sampleCount <
                    minimumSampleCount)
                {
                    ClearZScores(result);

                    result.StatusMessage =
                        $"資料不足：歷史工程案例數不足（{sampleCount}/{minimumSampleCount}），不執行 Z-score 統計判斷，列入待審。";

                    results.Add(result);

                    continue;
                }

                if (longPriceValues.Count >= 2)
                {
                    result.HistoricalAvgUnitPrice180Days =
                        Math.Round(
                            longPriceValues.Average(),
                            2
                        );

                    result.HistoricalStdUnitPrice180Days =
                        Math.Round(
                            CalculateStdDev(
                                longPriceValues
                            ),
                            2
                        );
                }

                if (
                    result.HistoricalAvgUnitPrice180Days
                        .HasValue &&
                    result.Qty.HasValue)
                {
                    result.SuggestedUnitPrice =
                        result
                            .HistoricalAvgUnitPrice180Days
                            .Value;

                    result.SuggestedAmount =
                        Math.Round(
                            result
                                .SuggestedUnitPrice
                                .Value *
                            result.Qty.Value,
                            0
                        );
                }

                result.ShortTermUnitPriceZScore =
                    CalculateZScore(
                        result.UnitPrice,
                        shortPriceValues
                    );

                result.LongTermUnitPriceZScore =
                    CalculateZScore(
                        result.UnitPrice,
                        longPriceValues
                    );

                result.WeightedUnitPriceZScore =
                    CalculateWeightedZScore(
                        result.LongTermUnitPriceZScore,
                        result.ShortTermUnitPriceZScore,
                        longTermWeight,
                        shortTermWeight
                    );

                result.QtyZScore =
                    null;

                result.UnitPriceZScore =
                    result.WeightedUnitPriceZScore;

                if (
                    result.WeightedUnitPriceZScore
                        .HasValue &&
                    result
                        .WeightedUnitPriceZScore
                        .Value >=
                    ruleThreshold)
                {
                    reasons.Add(
                        $"單價偏離歷史行情（Z = {result.WeightedUnitPriceZScore.Value:0.00}，門檻 = {ruleThreshold:0.##}）"
                    );
                }

                if (reasons.Any())
                {
                    result.IsAbnormal =
                        true;

                    result.StatusMessage =
                        string.Join(
                            "、",
                            reasons
                        );
                }
                else
                {
                    result.IsAbnormal =
                        false;

                    result.StatusMessage =
                        "正常";
                }

                results.Add(result);
            }

            return results;
        }

        private static void ClearZScores(
            TenderAnomalyResultDto result)
        {
            result.IsAbnormal =
                false;

            result.QtyZScore =
                null;

            result.UnitPriceZScore =
                null;

            result.ShortTermQtyZScore =
                null;

            result.LongTermQtyZScore =
                null;

            result.WeightedQtyZScore =
                null;

            result.ShortTermUnitPriceZScore =
                null;

            result.LongTermUnitPriceZScore =
                null;

            result.WeightedUnitPriceZScore =
                null;
        }

        private static bool IsSameUnit(
            string? excelUnit,
            string databaseUnit)
        {
            return NormalizeUnit(
                       excelUnit ?? ""
                   ) ==
                   NormalizeUnit(
                       databaseUnit
                   );
        }

        private static string NormalizeUnit(
            string unit)
        {
            if (string.IsNullOrWhiteSpace(
                    unit))
            {
                return string.Empty;
            }

            var normalized =
                unit.Trim()
                    .ToLowerInvariant();

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

        private static decimal? CalculateZScore(
            decimal? currentValue,
            List<decimal> historyValues)
        {
            if (!currentValue.HasValue)
            {
                return null;
            }

            if (historyValues == null ||
                historyValues.Count < 2)
            {
                return null;
            }

            var average =
                historyValues.Average();

            var stdDev =
                CalculateStdDev(
                    historyValues
                );

            if (stdDev == 0)
            {
                return null;
            }

            return Math.Round(
                Math.Abs(
                    (
                        currentValue.Value -
                        average
                    ) /
                    stdDev
                ),
                2
            );
        }

        private static decimal?
            CalculateWeightedZScore(
                decimal? longTermZ,
                decimal? shortTermZ,
                decimal longTermWeight,
                decimal shortTermWeight)
        {
            if (
                longTermZ.HasValue &&
                shortTermZ.HasValue)
            {
                return Math.Round(
                    longTermZ.Value *
                    longTermWeight +
                    shortTermZ.Value *
                    shortTermWeight,
                    2
                );
            }

            if (longTermZ.HasValue)
            {
                return longTermZ.Value;
            }

            if (shortTermZ.HasValue)
            {
                return shortTermZ.Value;
            }

            return null;
        }

        private static decimal CalculateStdDev(
            List<decimal> values)
        {
            if (values == null ||
                values.Count < 2)
            {
                return 0;
            }

            var average =
                values.Average();

            var variance =
                values.Average(v =>
                    Math.Pow(
                        (double)(
                            v - average
                        ),
                        2
                    )
                );

            return (decimal)Math.Sqrt(
                variance
            );
        }
    }
}