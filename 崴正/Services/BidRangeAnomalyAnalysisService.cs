using 崴正.Models;

namespace 崴正.Services
{
    public class BidRangeAnomalyAnalysisService
    {
        private readonly MaterialMappingService
            _materialMappingService;

        private readonly UnitConversionService
            _unitConversionService;

        private readonly BatchEstimationService
            _batchEstimationService;


        public BidRangeAnomalyAnalysisService(
            MaterialMappingService materialMappingService,
            UnitConversionService unitConversionService,
            BatchEstimationService batchEstimationService)
        {
            _materialMappingService =
                materialMappingService;

            _unitConversionService =
                unitConversionService;

            _batchEstimationService =
                batchEstimationService;
        }


        // ==========================================
        // 暫存在記憶體中的材料實際用量
        //
        // 同一 MaterialId 的多個 Excel 工項
        // 最後會加總成一筆。
        // ==========================================
        private class MaterialActualAccumulator
        {
            public int MaterialId { get; set; }

            public string MaterialName { get; set; }
                = string.Empty;

            public string BaseUnit { get; set; }
                = string.Empty;

            public decimal ActualQuantity { get; set; }

            public int SourceRowCount { get; set; }
        }


        public async Task<ServiceResult>
            AnalyzeAsync(
                BidRangeAnomalyAnalysisRequest request)
        {
            // ==========================================
            // 1. 基本驗證
            // ==========================================

            if (request == null)
            {
                return ServiceResult.Validation(
                    "異常分析資料不可為空"
                );
            }

            if (
                request.ExcelBytes == null ||
                request.ExcelBytes.Length == 0
            )
            {
                return ServiceResult.Validation(
                    "請上傳待分析的 Excel 標單"
                );
            }

            if (request.Target == null)
            {
                return ServiceResult.Validation(
                    "請輸入目標建案條件"
                );
            }


            // ==========================================
            // 2. 建立回傳物件
            // ==========================================

            var response =
                new BidRangeAnomalyAnalysisResponseDto
                {
                    FileName =
                        request.FileName
                        ?? string.Empty
                };


            // ==========================================
            // 3. 解析 Excel
            //
            // 注意：
            // 只解析，不寫 FinalBidItem。
            // ==========================================


            try
            {
                var parser =
                    new TenderExcelParser();

                using var stream =
                    new MemoryStream(
                        request.ExcelBytes
                    );


                var parsedRows =
                    parser.Parse(
                        stream,
                        request.FileName ?? string.Empty
                    );



                if (
                    parsedRows == null ||
                    parsedRows.Count == 0
                )
                {
                    return ServiceResult.Validation(
                        "標單中沒有解析到可用的工程明細"
                    );
                }


                // --------------------------------------
                // 這裡不真的使用 dynamic 邏輯；
                // 只是讓下面的 foreach 保持 parser
                // 原始 Row 型別。
                // --------------------------------------

                response.ParsedRowCount =
                    parsedRows.Count;


                // ==========================================
                // 4. 一次載入唯讀 Mapping Lookup
                // ==========================================


                var lookup =
                    await _materialMappingService
                        .GetReadOnlyLookupAsync();




                // ==========================================
                // 5. MaterialId → Actual Quantity
                // ==========================================

                var actualByMaterial =
                    new Dictionary<
                        int,
                        MaterialActualAccumulator
                    >();


                // ==========================================
                // 6. 處理每一列 Excel
                // ==========================================

                foreach (var row in parsedRows)
                {
                    // --------------------------------------
                    // 6-1. 基本數量資料檢查
                    // --------------------------------------

                    if (
                        !row.Qty.HasValue ||
                        row.Qty.Value <= 0
                    )
                    {
                        response.InvalidRowCount++;

                        response.UnmappedItems.Add(
                            new BidRangeAnomalyUnmappedItemDto
                            {
                                SheetName =
                                    row.SheetName,

                                RowNumber =
                                    row.RowNumber,

                                ItemName =
                                    row.ItemName
                                    ?? string.Empty,

                                Spec =
                                    row.Spec,

                                Unit =
                                    row.Unit,

                                Quantity =
                                    row.Qty,

                                Reason =
                                    "數量空白或小於等於 0"
                            }
                        );

                        continue;
                    }


                    if (
                        string.IsNullOrWhiteSpace(
                            row.Unit
                        )
                    )
                    {
                        response.InvalidRowCount++;

                        response.UnmappedItems.Add(
                            new BidRangeAnomalyUnmappedItemDto
                            {
                                SheetName =
                                    row.SheetName,

                                RowNumber =
                                    row.RowNumber,

                                ItemName =
                                    row.ItemName
                                    ?? string.Empty,

                                Spec =
                                    row.Spec,

                                Unit =
                                    row.Unit,

                                Quantity =
                                    row.Qty,

                                Reason =
                                    "標單單位為空白"
                            }
                        );

                        continue;
                    }


                    // --------------------------------------
                    // 6-2. Material Mapping
                    // --------------------------------------

                    var matched =
                        _materialMappingService
                            .FindBestMatch(
                                lookup,
                                row.ItemName,
                                row.Spec
                            );


                    if (matched == null)
                    {
                        response.UnmappedRowCount++;

                        response.UnmappedItems.Add(
                            new BidRangeAnomalyUnmappedItemDto
                            {
                                SheetName =
                                    row.SheetName,

                                RowNumber =
                                    row.RowNumber,

                                ItemName =
                                    row.ItemName
                                    ?? string.Empty,

                                Spec =
                                    row.Spec,

                                Unit =
                                    row.Unit,

                                Quantity =
                                    row.Qty,

                                Reason =
                                    "找不到對應的標準材料"
                            }
                        );

                        continue;
                    }


                    // --------------------------------------
                    // 6-3. 單位轉換
                    //
                    // Excel Unit
                    //      ↓
                    // MaterialMaster.BaseUnit
                    //
                    // UnitPrice 在新版數量異常判定中
                    // 暫時不使用，但 Convert() 需要參數，
                    // 因此沒有單價時傳 0。
                    // --------------------------------------

                    var conversion =
                        _unitConversionService
                            .Convert(
                                row.Qty.Value,

                                row.UnitPrice
                                    ?? 0m,

                                row.Unit,

                                matched.BaseUnit
                            );


                    if (!conversion.Success)
                    {
                        response.InvalidRowCount++;

                        response.UnmappedItems.Add(
                            new BidRangeAnomalyUnmappedItemDto
                            {
                                SheetName =
                                    row.SheetName,

                                RowNumber =
                                    row.RowNumber,

                                ItemName =
                                    row.ItemName
                                    ?? string.Empty,

                                Spec =
                                    row.Spec,

                                Unit =
                                    row.Unit,

                                Quantity =
                                    row.Qty,

                                Reason =
                                    conversion.ErrorMessage
                                    ?? "單位無法轉換"
                            }
                        );

                        continue;
                    }


                    // --------------------------------------
                    // 到這裡才代表：
                    //
                    // Mapping 成功
                    // +
                    // Quantity 有效
                    // +
                    // Unit 成功標準化
                    // --------------------------------------

                    response.MappedRowCount++;


                    // --------------------------------------
                    // 6-4. 同 MaterialId 加總
                    //
                    // 例如：
                    //
                    // 柱 SD420  30噸
                    // 梁 SD420  50噸
                    // 板 SD420  20噸
                    //
                    // Actual = 100噸
                    // --------------------------------------

                    if (
                        !actualByMaterial.TryGetValue(
                            matched.MaterialId,
                            out var accumulator
                        )
                    )
                    {
                        accumulator =
                            new MaterialActualAccumulator
                            {
                                MaterialId =
                                    matched.MaterialId,

                                MaterialName =
                                    matched.MaterialName,

                                BaseUnit =
                                    matched.BaseUnit
                            };

                        actualByMaterial.Add(
                            matched.MaterialId,
                            accumulator
                        );
                    }


                    accumulator.ActualQuantity +=
                        conversion.Quantity;

                    accumulator.SourceRowCount++;
                }




                // ==========================================
                // 7. 標單中成功解析出的標準材料數
                // ==========================================

                response.MaterialCount =
                    actualByMaterial.Count;


                // ==========================================
                // 8. 如果完全沒有材料可分析
                //
                // 不把 Excel 寫進 DB，
                // 直接把 Mapping / Invalid 結果回傳。
                // ==========================================

                if (!actualByMaterial.Any())
                {
                    return ServiceResult.Ok(
                        response
                    );
                }


                // ==========================================
                // 9. 呼叫既有 Batch Estimation
                //
                // Target 只解析一次
                // Similarity 只算一次
                //
                // 然後針對 Excel 裡實際出現的材料，
                // 取得各自 P15 / P50 / P85。
                // ==========================================


                var estimationResult =
                    await _batchEstimationService
                        .EstimateMaterialsAsync(
                            request.Target,
                            actualByMaterial.Keys
                        );



                if (!estimationResult.Success)
                {
                    return estimationResult;
                }


                if (
                    estimationResult.Data
                    is not BatchEstimationDto batch
                )
                {
                    return ServiceResult.Validation(
                        "合理用量估算結果格式錯誤"
                    );
                }


                response.CandidateProjectCount =
                    batch.CandidateProjectCount;


                // ==========================================
                // 10. Actual vs P15 / P50 / P85
                // ==========================================

                foreach (var range in batch.Results)
                {
                    if (
                        !actualByMaterial.TryGetValue(
                            range.MaterialId,
                            out var actual
                        )
                    )
                    {

                        continue;
                    }


                    var item =
                        new BidRangeAnomalyMaterialResultDto
                        {
                            MaterialId =
                                range.MaterialId,

                            MaterialName =
                                range.MaterialName,

                            BaseUnit =
                                range.BaseUnit,

                            ActualQuantity =
                                actual.ActualQuantity,

                            SourceRowCount =
                                actual.SourceRowCount,

                            ReferenceProjectCount =
                                range.ReferenceProjectCount,

                            MinimumSampleCount =
                                range.MinimumSampleCount,

                            HasEnoughSamples =
                                range.HasEnoughSamples,

                            LowerQuantity =
                                range.LowerQuantity,

                            MedianQuantity =
                                range.MedianQuantity,

                            UpperQuantity =
                                range.UpperQuantity
                        };


                    // ======================================
                    // 10-1. 樣本不足
                    // ======================================

                    if (
                        !range.HasEnoughSamples ||
                        !range.LowerQuantity.HasValue ||
                        !range.MedianQuantity.HasValue ||
                        !range.UpperQuantity.HasValue
                    )
                    {
                        item.AnomalyStatus =
                            "InsufficientSamples";

                        item.StatusMessage =
                            string.IsNullOrWhiteSpace(
                                range.StatusMessage
                            )
                                ? "歷史樣本不足，暫時無法判定異常"
                                : range.StatusMessage;


                        response
                            .InsufficientSampleCount++;
                    }


                    // ======================================
                    // 10-2. Actual < P15
                    // ======================================

                    else if (
                        actual.ActualQuantity <
                        range.LowerQuantity.Value
                    )
                    {
                        item.AnomalyStatus =
                            "Low";

                        item.StatusMessage =
                            $"實際用量低於 P15 合理下界 " +
                            $"({range.LowerQuantity.Value:N2} {range.BaseUnit})";


                        response.LowCount++;
                    }


                    // ======================================
                    // 10-3. Actual > P85
                    // ======================================

                    else if (
                        actual.ActualQuantity >
                        range.UpperQuantity.Value
                    )
                    {
                        item.AnomalyStatus =
                            "High";

                        item.StatusMessage =
                            $"實際用量高於 P85 合理上界 " +
                            $"({range.UpperQuantity.Value:N2} {range.BaseUnit})";


                        response.HighCount++;
                    }


                    // ======================================
                    // 10-4. P15 <= Actual <= P85
                    // ======================================

                    else
                    {
                        item.AnomalyStatus =
                            "Normal";

                        item.StatusMessage =
                            "實際用量位於 P15～P85 合理區間內";


                        response.NormalCount++;
                    }


                    response.Results.Add(
                        item
                    );
                }


                // ==========================================
                // 11. 排序
                //
                // 高異常
                // ↓
                // 低異常
                // ↓
                // 正常
                // ↓
                // 樣本不足
                //
                // 之後 UI 第一眼先看到需要注意的材料。
                // ==========================================

                response.Results =
                    response.Results
                        .OrderBy(x =>
                        GetStatusOrder(
                                x.AnomalyStatus
                            )
                        )
                        .ThenBy(x =>
                            x.MaterialName
                        )
                        .ToList();






                return ServiceResult.Ok(
                    response
                );
            }
            catch (Exception ex)
            {
                return ServiceResult.Validation(
                    $"標單分析失敗：{ex.Message}"
                );
            }
        }


        private static int GetStatusOrder(
            string status)
        {
            return status switch
            {
                "High" => 1,
                "Low" => 2,
                "Normal" => 3,
                "InsufficientSamples" => 4,
                _ => 9
            };
        }
    }
}