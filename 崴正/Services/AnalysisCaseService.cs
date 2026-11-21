using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class AnalysisCaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly OneDIntegrationService _oneDIntegrationService;
        private readonly TwoAPreprocessService _twoAPreprocessService;
        private readonly TwoAQuantityAnalysisService _quantityAnalysisService;
        private readonly MajorCategoryPriceRangeService _priceRangeService;
        private readonly AuditLogService _auditLogService;


        public AnalysisCaseService(
            ApplicationDbContext context,
            OneDIntegrationService oneDIntegrationService,
            TwoAPreprocessService twoAPreprocessService,
            TwoAQuantityAnalysisService quantityAnalysisService,
            MajorCategoryPriceRangeService priceRangeService,
            AuditLogService auditLogService)
        {
            _context = context;
            _oneDIntegrationService = oneDIntegrationService;
            _twoAPreprocessService = twoAPreprocessService;
            _quantityAnalysisService = quantityAnalysisService;
            _priceRangeService = priceRangeService;
            _auditLogService = auditLogService;
        }


        public async Task<ServiceResult> CreateFromOneDAsync(
            int oneDProjectId,
            AnalysisCaseMetadataRequest metadata,
            string? note,
            string actionUser,
            CancellationToken cancellationToken)
        {
            var validation = ValidateMetadata(metadata);
            if (validation != null)
            {
                return validation;
            }

            var source =
                await _oneDIntegrationService.GetProjectResultAsync(
                    oneDProjectId,
                    cancellationToken);

            if (!source.Success || source.Data == null)
            {
                return source.StatusCode == StatusCodes.Status404NotFound
                    ? ServiceResult.NotFound(source.Message)
                    : ServiceResult.Validation(source.Message);
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var analysisCase =
                    CreateCaseEntity(metadata);

                _context.AnalysisCases.Add(analysisCase);
                await _context.SaveChangesAsync(cancellationToken);

                var version =
                    CreateOneDVersionEntity(
                        analysisCase.AnalysisCaseId,
                        1,
                        oneDProjectId,
                        note,
                        source.Data.Items);

                _context.AnalysisBidVersions.Add(version);
                await _context.SaveChangesAsync(cancellationToken);

                AddCaseAudit(
                    analysisCase,
                    version,
                    "CreateAnalysisCaseFromOneD",
                    $"由 1D ProjectId={oneDProjectId} 建立待偵錯案件",
                    actionUser);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Ok(new
                {
                    message = "已建立 2A 待偵錯案件，1D 結果已儲存為版本快照。",
                    analysisCaseId = analysisCase.AnalysisCaseId,
                    analysisBidVersionId = version.AnalysisBidVersionId,
                    versionNo = version.VersionNo,
                    sourceType = version.SourceType,
                    sourceOneDProjectId = oneDProjectId,
                    itemCount = version.Items.Count
                });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<ServiceResult> CreateFromUploadAsync(
            AnalysisCaseMetadataRequest metadata,
            byte[] excelBytes,
            string fileName,
            string? note,
            string actionUser,
            CancellationToken cancellationToken,
            IReadOnlyCollection<string>? includedSheetNames)
        {
            var validation = ValidateMetadata(metadata);
            if (validation != null)
            {
                return validation;
            }

            var parsed =
                ParseUpload(excelBytes, fileName, includedSheetNames);

            if (!parsed.Success)
            {
                return parsed.Result!;
            }

            var structureValidation =
                ValidateUploadStructure(
                    parsed.Rows!
                );

            if (structureValidation != null)
            {
                return structureValidation;
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var analysisCase =
                    CreateCaseEntity(metadata);

                _context.AnalysisCases.Add(analysisCase);
                await _context.SaveChangesAsync(cancellationToken);

                var version =
                    CreateUploadVersionEntity(
                        analysisCase.AnalysisCaseId,
                        1,
                        excelBytes,
                        fileName,
                        note,
                        parsed.Rows!);

                _context.AnalysisBidVersions.Add(version);
                await _context.SaveChangesAsync(cancellationToken);

                AddCaseAudit(
                    analysisCase,
                    version,
                    "CreateAnalysisCaseFromUpload",
                    $"由上傳標單「{fileName}」建立待偵錯案件",
                    actionUser);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Ok(new
                {
                    message = "已建立 2A 待偵錯案件，上傳標單只儲存於 Target Pipeline，不會寫入歷史資料。",
                    analysisCaseId = analysisCase.AnalysisCaseId,
                    analysisBidVersionId = version.AnalysisBidVersionId,
                    versionNo = version.VersionNo,
                    sourceType = version.SourceType,
                    sourceFileName = fileName,
                    itemCount = version.Items.Count
                });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<ServiceResult> AddVersionFromOneDAsync(
            int analysisCaseId,
            int oneDProjectId,
            string? note,
            string actionUser,
            CancellationToken cancellationToken)
        {
            var analysisCase =
                await _context.AnalysisCases
                    .FirstOrDefaultAsync(
                        x => x.AnalysisCaseId == analysisCaseId,
                        cancellationToken);

            if (analysisCase == null)
            {
                return ServiceResult.NotFound("找不到待偵錯案件");
            }

            var source =
                await _oneDIntegrationService.GetProjectResultAsync(
                    oneDProjectId,
                    cancellationToken);

            if (!source.Success || source.Data == null)
            {
                return source.StatusCode == StatusCodes.Status404NotFound
                    ? ServiceResult.NotFound(source.Message)
                    : ServiceResult.Validation(source.Message);
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                int versionNo =
                    await GetNextVersionNoAsync(
                        analysisCaseId,
                        cancellationToken);

                await ClearCurrentVersionAsync(
                    analysisCaseId,
                    cancellationToken);

                var version =
                    CreateOneDVersionEntity(
                        analysisCaseId,
                        versionNo,
                        oneDProjectId,
                        note,
                        source.Data.Items);

                _context.AnalysisBidVersions.Add(version);
                analysisCase.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                AddCaseAudit(
                    analysisCase,
                    version,
                    "AddAnalysisVersionFromOneD",
                    $"從 1D ProjectId={oneDProjectId} 建立第 {versionNo} 版",
                    actionUser);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Ok(new
                {
                    message = "已建立新的 1D 快照版本，既有版本仍保留。",
                    analysisCaseId,
                    analysisBidVersionId = version.AnalysisBidVersionId,
                    versionNo,
                    itemCount = version.Items.Count
                });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<ServiceResult> AddVersionFromUploadAsync(
            int analysisCaseId,
            byte[] excelBytes,
            string fileName,
            string? note,
            string actionUser,
            CancellationToken cancellationToken,
            IReadOnlyCollection<string>? includedSheetNames)
        {
            var analysisCase =
                await _context.AnalysisCases
                    .FirstOrDefaultAsync(
                        x => x.AnalysisCaseId == analysisCaseId,
                        cancellationToken);

            if (analysisCase == null)
            {
                return ServiceResult.NotFound("找不到待偵錯案件");
            }

            var parsed = ParseUpload(excelBytes, fileName, includedSheetNames);
            if (!parsed.Success)
            {
                return parsed.Result!;
            }

            var structureValidation =
                ValidateUploadStructure(
                    parsed.Rows!
                );

            if (structureValidation != null)
            {
                return structureValidation;
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                int versionNo =
                    await GetNextVersionNoAsync(
                        analysisCaseId,
                        cancellationToken);

                await ClearCurrentVersionAsync(
                    analysisCaseId,
                    cancellationToken);

                var version =
                    CreateUploadVersionEntity(
                        analysisCaseId,
                        versionNo,
                        excelBytes,
                        fileName,
                        note,
                        parsed.Rows!);

                _context.AnalysisBidVersions.Add(version);
                analysisCase.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                AddCaseAudit(
                    analysisCase,
                    version,
                    "AddAnalysisVersionFromUpload",
                    $"上傳「{fileName}」建立第 {versionNo} 版",
                    actionUser);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Ok(new
                {
                    message = "已建立新的待偵錯標單版本，既有版本仍保留。",
                    analysisCaseId,
                    analysisBidVersionId = version.AnalysisBidVersionId,
                    versionNo,
                    itemCount = version.Items.Count
                });
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<ServiceResult> GetCasesAsync()
        {
            var cases =
                await _context.AnalysisCases
                    .AsNoTracking()
                    .Where(x => x.Status != "Archived")
                    .OrderByDescending(x => x.UpdatedAt)
                    .Select(x => new AnalysisCaseSummaryDto
                    {
                        AnalysisCaseId = x.AnalysisCaseId,
                        CaseName = x.CaseName,
                        ProjectType = x.ProjectType,
                        StructureType = x.StructureType,
                        City = x.City,
                        TotalFloorArea = x.TotalFloorArea,
                        SiteArea = x.SiteArea,
                        FloorCount = x.FloorCount,
                        BasementFloorCount = x.BasementFloorCount,
                        ProjectDate = x.ProjectDate,
                        Status = x.Status,
                        VersionCount = x.Versions.Count,
                        CurrentVersionId = x.Versions
                            .Where(v => v.IsCurrent)
                            .Select(v => (long?)v.AnalysisBidVersionId)
                            .FirstOrDefault(),
                        CurrentVersionNo = x.Versions
                            .Where(v => v.IsCurrent)
                            .Select(v => (int?)v.VersionNo)
                            .FirstOrDefault(),
                        CurrentSourceType = x.Versions
                            .Where(v => v.IsCurrent)
                            .Select(v => v.SourceType)
                            .FirstOrDefault(),
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToListAsync();

            return ServiceResult.Ok(cases);
        }


        public async Task<ServiceResult> GetCaseAsync(
            int analysisCaseId)
        {
            var analysisCase =
                await _context.AnalysisCases
                    .AsNoTracking()
                    .Include(x => x.Versions)
                        .ThenInclude(v => v.Items)
                    .FirstOrDefaultAsync(
                        x => x.AnalysisCaseId == analysisCaseId);

            if (analysisCase == null)
            {
                return ServiceResult.NotFound("找不到待偵錯案件");
            }

            var current =
                analysisCase.Versions
                    .FirstOrDefault(v => v.IsCurrent);

            var dto =
                new AnalysisCaseDetailDto
                {
                    Case = new AnalysisCaseSummaryDto
                    {
                        AnalysisCaseId = analysisCase.AnalysisCaseId,
                        CaseName = analysisCase.CaseName,
                        ProjectType = analysisCase.ProjectType,
                        StructureType = analysisCase.StructureType,
                        City = analysisCase.City,
                        TotalFloorArea = analysisCase.TotalFloorArea,
                        SiteArea = analysisCase.SiteArea,
                        FloorCount = analysisCase.FloorCount,
                        BasementFloorCount = analysisCase.BasementFloorCount,
                        ProjectDate = analysisCase.ProjectDate,
                        Status = analysisCase.Status,
                        VersionCount = analysisCase.Versions.Count,
                        CurrentVersionId = current?.AnalysisBidVersionId,
                        CurrentVersionNo = current?.VersionNo,
                        CurrentSourceType = current?.SourceType,
                        UpdatedAt = analysisCase.UpdatedAt
                    },
                    Versions = analysisCase.Versions
                        .OrderByDescending(v => v.VersionNo)
                        .Select(v => ToVersionDto(v))
                        .ToList()
                };

            return ServiceResult.Ok(dto);
        }


        public async Task<ServiceResult> GetVersionAsync(
            int analysisCaseId,
            long versionId)
        {
            var version =
                await _context.AnalysisBidVersions
                    .AsNoTracking()
                    .Include(x => x.AnalysisCase)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.AnalysisBidVersionId == versionId &&
                        x.AnalysisCaseId == analysisCaseId);

            if (version == null)
            {
                return ServiceResult.NotFound("找不到指定的待偵錯版本");
            }

            var caseEntity = version.AnalysisCase;

            var dto =
                new AnalysisVersionDetailDto
                {
                    Case = new AnalysisCaseSummaryDto
                    {
                        AnalysisCaseId = caseEntity.AnalysisCaseId,
                        CaseName = caseEntity.CaseName,
                        ProjectType = caseEntity.ProjectType,
                        StructureType = caseEntity.StructureType,
                        City = caseEntity.City,
                        TotalFloorArea = caseEntity.TotalFloorArea,
                        SiteArea = caseEntity.SiteArea,
                        FloorCount = caseEntity.FloorCount,
                        BasementFloorCount = caseEntity.BasementFloorCount,
                        ProjectDate = caseEntity.ProjectDate,
                        Status = caseEntity.Status,
                        VersionCount = await _context.AnalysisBidVersions
                            .AsNoTracking()
                            .CountAsync(v => v.AnalysisCaseId == analysisCaseId),
                        CurrentVersionId = version.IsCurrent
                            ? version.AnalysisBidVersionId
                            : await _context.AnalysisBidVersions
                                .AsNoTracking()
                                .Where(v => v.AnalysisCaseId == analysisCaseId && v.IsCurrent)
                                .Select(v => (long?)v.AnalysisBidVersionId)
                                .FirstOrDefaultAsync(),
                        CurrentVersionNo = version.IsCurrent
                            ? version.VersionNo
                            : await _context.AnalysisBidVersions
                                .AsNoTracking()
                                .Where(v => v.AnalysisCaseId == analysisCaseId && v.IsCurrent)
                                .Select(v => (int?)v.VersionNo)
                                .FirstOrDefaultAsync(),
                        CurrentSourceType = await _context.AnalysisBidVersions
                            .AsNoTracking()
                            .Where(v => v.AnalysisCaseId == analysisCaseId && v.IsCurrent)
                            .Select(v => v.SourceType)
                            .FirstOrDefaultAsync(),
                        UpdatedAt = caseEntity.UpdatedAt
                    },
                    Version = ToVersionDto(version),
                    Items = version.Items
                        .OrderBy(x => x.AnalysisBidItemId)
                        .Select(ToItemDto)
                        .ToList()
                };

            return ServiceResult.Ok(dto);
        }


        // Read-only disclosure of the exact material-scope preprocessing used
        // by RunQuantityAnalysisAsync. No new analysis run or audit write.
        public async Task<ServiceResult> GetVersionQuantityScopeAsync(
            int analysisCaseId,
            long versionId)
        {
            var version = await _context.AnalysisBidVersions
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.AnalysisBidVersionId == versionId &&
                    x.AnalysisCaseId == analysisCaseId);

            if (version == null)
                return ServiceResult.NotFound("找不到指定的待偵錯版本");

            var orderedItems = version.Items
                .OrderBy(x => x.AnalysisBidItemId)
                .ToList();

            var preprocess = _twoAPreprocessService.Build(
                analysisCaseId, ToPreprocessSourceItems(orderedItems));

            var scopeRows = orderedItems.Select((sourceItem, index) =>
            {
                var item = preprocess.Items[index];
                return new
                {
                    sourceItem.AnalysisBidItemId,
                    sourceItem.SheetName,
                    sourceItem.RowNumber,
                    sourceItem.ItemName,
                    sourceItem.Spec,
                    sourceItem.Unit,
                    sourceItem.Quantity,
                    sourceItem.MajorCategory,
                    item.DetectionCategory,
                    item.Status,
                    item.Reason,
                    item.UnitNormalizationStatus,
                    item.NormalizedQuantity,
                    item.NormalizedUnit,
                    CandidateType = TenderImportInspectionService
                        .ClassifyQuantityCandidate(
                            BuildAnalysisText(sourceItem.ItemName, sourceItem.Spec),
                            null)
                };
            }).ToArray();

            return ServiceResult.Ok(new
            {
                analysisCaseId,
                analysisBidVersionId = versionId,
                itemCount = orderedItems.Count,
                preprocess.IncludedItemCount,
                preprocess.ExcludedItemCount,
                preprocess.PendingItemCount,
                excludedQuantityCandidateCount = scopeRows.Count(x =>
                    x.Status == "Excluded" && x.CandidateType == "Excluded"),
                items = scopeRows,
                note = "此區是依目前數量偵錯前處理規則，對版本快照重新計算的唯讀分類；如規則日後調整，歷次 Run 仍保留當時結果。排除不代表工項沒有材料，複合工項須人工確認；分類排除不影響大項價格的計價納入。"
            });
        }


        private static List<OneDSavedResultItem> ToPreprocessSourceItems(
            IReadOnlyList<AnalysisBidItem> orderedItems)
        {
            return orderedItems
                .Select(x => new OneDSavedResultItem
                {
                    Category = x.SourceCategory1,
                    Chapter = x.MajorCategory ?? string.Empty,
                    SeqInChapter = x.SourceSequence,
                    Code = x.ItemCode,
                    Name = BuildAnalysisText(x.ItemName, x.Spec),
                    Unit = x.Unit,
                    Quantity = x.Quantity,
                    Confidence = x.SourceConfidence
                })
                .ToList();
        }


        public async Task<ServiceResult> RunQuantityAnalysisAsync(
            int analysisCaseId,
            long versionId,
            int? topCount)
        {
            var version =
                await _context.AnalysisBidVersions
                    .Include(x => x.AnalysisCase)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.AnalysisBidVersionId == versionId &&
                        x.AnalysisCaseId == analysisCaseId);

            if (version == null)
            {
                return ServiceResult.NotFound("找不到指定的待偵錯版本");
            }

            var sourceItems = ToPreprocessSourceItems(
                version.Items.OrderBy(x => x.AnalysisBidItemId).ToList());

            var preprocess =
                _twoAPreprocessService.Build(
                    analysisCaseId,
                    sourceItems);

            var target =
                ToSimilarityTarget(version.AnalysisCase);

            var analysis =
                await _quantityAnalysisService.AnalyzeAsync(
                    analysisCaseId,
                    preprocess,
                    new TwoAQuantityAnalysisRequest
                    {
                        Target = target,
                        TopCount = topCount
                    });

            if (!analysis.Success)
            {
                return analysis;
            }

            var run =
                new AnalysisRun
                {
                    AnalysisBidVersionId = versionId,
                    RunType = "Quantity",
                    Status = "Completed",
                    ResultJson = JsonSerializer.Serialize(analysis.Data, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    CreatedAt = DateTime.UtcNow
                };

            _context.AnalysisRuns.Add(run);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                analysisRunId = run.AnalysisRunId,
                analysisCaseId,
                analysisBidVersionId = versionId,
                versionNo = version.VersionNo,
                sourceType = version.SourceType,
                result = analysis.Data
            });
        }


        public async Task<ServiceResult> RunMajorPriceAnalysisAsync(
            int analysisCaseId,
            long versionId,
            string majorCategory,
            int? topCount)
        {
            majorCategory = majorCategory?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(majorCategory))
            {
                return ServiceResult.Validation("請提供工程大項");
            }

            var version =
                await _context.AnalysisBidVersions
                    .Include(x => x.AnalysisCase)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x =>
                        x.AnalysisBidVersionId == versionId &&
                        x.AnalysisCaseId == analysisCaseId);

            if (version == null)
            {
                return ServiceResult.NotFound("找不到指定的待偵錯版本");
            }

            var matchingItems =
                version.Items
                    .Where(x => string.Equals(
                        x.MajorCategory,
                        majorCategory,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (!matchingItems.Any())
            {
                return ServiceResult.Validation(
                    $"此版本沒有工程大項「{majorCategory}」的工項。");
            }

            decimal observedAmount =
                matchingItems.Sum(ResolveLineAmount);

            if (observedAmount <= 0)
            {
                return ServiceResult.Validation(
                    "此版本沒有可用的工程大項金額。1D 目前只提供分類與數量時，需要另外取得本次標單的價格資料後才能做大項價格異常。");
            }

            var analysis =
                await _priceRangeService.EstimateAsync(
                    new MajorCategoryPriceRangeRequest
                    {
                        Target = ToSimilarityTarget(version.AnalysisCase),
                        MajorCategory = majorCategory,
                        ObservedAmount = observedAmount,
                        TopCount = topCount
                    });

            if (!analysis.Success)
            {
                return analysis;
            }

            var run =
                new AnalysisRun
                {
                    AnalysisBidVersionId = versionId,
                    RunType = $"MajorPrice:{majorCategory}",
                    Status = "Completed",
                    ResultJson = JsonSerializer.Serialize(analysis.Data, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    CreatedAt = DateTime.UtcNow
                };

            _context.AnalysisRuns.Add(run);
            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                analysisRunId = run.AnalysisRunId,
                analysisCaseId,
                analysisBidVersionId = versionId,
                versionNo = version.VersionNo,
                sourceType = version.SourceType,
                result = analysis.Data
            });
        }


        public async Task<ServiceResult> GetRunsAsync(
            int analysisCaseId)
        {
            bool exists =
                await _context.AnalysisCases
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.AnalysisCaseId == analysisCaseId);

            if (!exists)
            {
                return ServiceResult.NotFound("找不到待偵錯案件");
            }

            var runs =
                await _context.AnalysisRuns
                    .AsNoTracking()
                    .Where(x =>
                        x.AnalysisBidVersion.AnalysisCaseId ==
                        analysisCaseId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new AnalysisRunSummaryDto
                    {
                        AnalysisRunId = x.AnalysisRunId,
                        AnalysisBidVersionId = x.AnalysisBidVersionId,
                        VersionNo = x.AnalysisBidVersion.VersionNo,
                        RunType = x.RunType,
                        Status = x.Status,
                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync();

            return ServiceResult.Ok(runs);
        }


        public async Task<ServiceResult> GetRunAsync(
            int analysisCaseId,
            long analysisRunId)
        {
            var run =
                await _context.AnalysisRuns
                    .AsNoTracking()
                    .Include(x => x.AnalysisBidVersion)
                    .FirstOrDefaultAsync(x =>
                        x.AnalysisRunId == analysisRunId &&
                        x.AnalysisBidVersion.AnalysisCaseId ==
                            analysisCaseId);

            if (run == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的偵錯紀錄");
            }

            JsonElement result;

            try
            {
                result =
                    JsonSerializer.Deserialize<JsonElement>(
                        run.ResultJson);
            }
            catch (JsonException)
            {
                result =
                    JsonSerializer.SerializeToElement(
                        new
                        {
                            message =
                                "此偵錯紀錄的結果資料格式無法解析"
                        });
            }

            return ServiceResult.Ok(
                new AnalysisRunDetailDto
                {
                    AnalysisRunId =
                        run.AnalysisRunId,
                    AnalysisBidVersionId =
                        run.AnalysisBidVersionId,
                    VersionNo =
                        run.AnalysisBidVersion.VersionNo,
                    RunType =
                        run.RunType,
                    Status =
                        run.Status,
                    CreatedAt =
                        run.CreatedAt,
                    Result =
                        result
                });
        }


        private static ServiceResult? ValidateMetadata(
            AnalysisCaseMetadataRequest metadata)
        {
            if (metadata == null)
            {
                return ServiceResult.Validation("請提供待偵錯案件資料");
            }

            if (string.IsNullOrWhiteSpace(metadata.CaseName))
            {
                return ServiceResult.Validation("請輸入案件名稱");
            }

            if (string.IsNullOrWhiteSpace(metadata.ProjectType))
            {
                return ServiceResult.Validation("請輸入建案類型");
            }

            if (string.IsNullOrWhiteSpace(metadata.StructureType))
            {
                return ServiceResult.Validation("請輸入結構類型");
            }

            if (string.IsNullOrWhiteSpace(metadata.City))
            {
                return ServiceResult.Validation("請輸入縣市");
            }

            if (metadata.TotalFloorArea <= 0)
            {
                return ServiceResult.Validation("總樓地板面積必須大於 0");
            }

            if (metadata.SiteArea <= 0)
            {
                return ServiceResult.Validation("基地面積必須大於 0");
            }

            if (metadata.FloorCount <= 0)
            {
                return ServiceResult.Validation("地上樓層必須大於 0");
            }

            if (metadata.BasementFloorCount < 0)
            {
                return ServiceResult.Validation("地下樓層不可小於 0");
            }

            if (metadata.ProjectDate == default)
            {
                return ServiceResult.Validation("請提供建案日期");
            }

            return null;
        }


        private static AnalysisCase CreateCaseEntity(
            AnalysisCaseMetadataRequest metadata)
        {
            var now = DateTime.UtcNow;

            return new AnalysisCase
            {
                CaseName = metadata.CaseName.Trim(),
                ProjectType = metadata.ProjectType.Trim(),
                StructureType = metadata.StructureType.Trim(),
                City = metadata.City.Trim(),
                TotalFloorArea = metadata.TotalFloorArea,
                SiteArea = metadata.SiteArea,
                FloorCount = metadata.FloorCount,
                BasementFloorCount = metadata.BasementFloorCount,
                ProjectDate = metadata.ProjectDate.Date,
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            };
        }


        private static AnalysisBidVersion CreateOneDVersionEntity(
            int analysisCaseId,
            int versionNo,
            int oneDProjectId,
            string? note,
            IReadOnlyCollection<OneDSavedResultItem> sourceItems)
        {
            var version =
                new AnalysisBidVersion
                {
                    AnalysisCaseId = analysisCaseId,
                    VersionNo = versionNo,
                    SourceType = "OneD",
                    SourceOneDProjectId = oneDProjectId,
                    SourceNote = NormalizeOptional(note),
                    IsCurrent = true,
                    CreatedAt = DateTime.UtcNow
                };

            foreach (var source in sourceItems)
            {
                version.Items.Add(
                    new AnalysisBidItem
                    {
                        SourceSequence = source.SeqInChapter,
                        ItemCode = NormalizeOptional(source.Code),
                        SourceCategory1 = NormalizeOptional(source.Category),
                        SourceCategory2 = NormalizeOptional(source.Chapter),
                        MajorCategory = NormalizeOptional(source.Chapter),
                        ItemName = source.Name?.Trim() ?? string.Empty,
                        Unit = NormalizeOptional(source.Unit),
                        Quantity = source.Quantity,
                        SourceConfidence = source.Confidence
                    });
            }

            return version;
        }


        private static AnalysisBidVersion CreateUploadVersionEntity(
            int analysisCaseId,
            int versionNo,
            byte[] excelBytes,
            string fileName,
            string? note,
            IReadOnlyCollection<TenderImportRowDto> rows)
        {
            string fileHash =
                Convert.ToHexString(
                    SHA256.HashData(excelBytes))
                    .ToLowerInvariant();

            var version =
                new AnalysisBidVersion
                {
                    AnalysisCaseId = analysisCaseId,
                    VersionNo = versionNo,
                    SourceType = "Upload",
                    SourceFileName = fileName,
                    SourceFileHash = fileHash,
                    SourceNote = NormalizeOptional(note),
                    IsCurrent = true,
                    CreatedAt = DateTime.UtcNow
                };

            foreach (var row in rows)
            {
                string? majorCategory =
                    TwoAScopePolicy.ResolveMajorCategory(
                        row.Category1,
                        row.Category2,
                        row.Category3);

                version.Items.Add(
                    new AnalysisBidItem
                    {
                        SheetName = NormalizeOptional(row.SheetName),
                        RowNumber = row.RowNumber,
                        ItemCode = NormalizeOptional(row.ItemCode),
                        SourceCategory1 = NormalizeOptional(row.Category1),
                        SourceCategory2 = NormalizeOptional(row.Category2),
                        SourceCategory3 = NormalizeOptional(row.Category3),
                        MajorCategory = majorCategory,
                        ItemName = row.ItemName?.Trim() ?? string.Empty,
                        Spec = NormalizeOptional(row.Spec),
                        Unit = NormalizeOptional(row.Unit),
                        Quantity = row.Qty,
                        UnitPrice = row.UnitPrice,
                        Amount = row.Amount,
                        Note = NormalizeOptional(row.Note)
                    });
            }

            return version;
        }


        private static ServiceResult?
            ValidateUploadStructure(
                IReadOnlyCollection<TenderImportRowDto>
                    rows)
        {
            var detailRows =
                rows
                    .Where(IsAnalysisDetailRow)
                    .ToList();

            if (!detailRows.Any())
            {
                return ServiceResult.Validation(
                    "直接上傳的待偵錯標單沒有可分析的工程明細。暫定規格要求至少包含工項、單位與數量或價格資料。"
                );
            }

            var uncategorizedRows =
                detailRows
                    .Where(row =>
                        TwoAScopePolicy.ResolveMajorCategory(
                            row.Category1,
                            row.Category2,
                            row.Category3
                        ) == null)
                    .ToList();

            if (uncategorizedRows.Any())
            {
                string examples =
                    string.Join(
                        "、",
                        uncategorizedRows
                            .Take(5)
                            .Select(row =>
                                !string.IsNullOrWhiteSpace(
                                    row.ItemName)
                                    ? row.ItemName.Trim()
                                    : !string.IsNullOrWhiteSpace(
                                        row.Spec)
                                        ? row.Spec.Trim()
                                        : $"第 {row.RowNumber} 列"
                            )
                    );

                return ServiceResult.Validation(
                    $"直接上傳目前暫定只接受已具工程大項分類的結構化標單，不接受尚未分類的原始文件。共有 {uncategorizedRows.Count} 筆工程明細找不到 1D 同層級工程大項，例如：{examples}。"
                );
            }

            bool hasQuantityData =
                detailRows.Any(row =>
                    row.Qty.HasValue &&
                    row.Qty.Value > 0 &&
                    !string.IsNullOrWhiteSpace(
                        row.Unit
                    ));

            if (!hasQuantityData)
            {
                return ServiceResult.Validation(
                    "直接上傳的標單沒有可供細項數量異常分析的「單位 + 數量」資料。"
                );
            }

            return null;
        }


        private static bool IsAnalysisDetailRow(
            TenderImportRowDto row)
        {
            bool hasBusinessValue =
                row.Qty.HasValue ||
                row.UnitPrice.HasValue ||
                row.Amount.HasValue ||
                !string.IsNullOrWhiteSpace(
                    row.Unit
                );

            bool hasDescription =
                !string.IsNullOrWhiteSpace(
                    row.ItemName
                ) ||
                !string.IsNullOrWhiteSpace(
                    row.Spec
                );

            return
                hasBusinessValue &&
                hasDescription;
        }


        private static (
            bool Success,
            List<TenderImportRowDto>? Rows,
            ServiceResult? Result)
            ParseUpload(
                byte[] excelBytes,
                string fileName,
                IReadOnlyCollection<string>? includedSheetNames)
        {
            if (excelBytes == null ||
                excelBytes.Length == 0)
            {
                return (
                    false,
                    null,
                    ServiceResult.Validation(
                        "請先選擇待偵錯標單"));
            }

            try
            {
                var parser = new TenderExcelParser();
                using var stream =
                    new MemoryStream(excelBytes);

                var rows =
                    parser.Parse(stream, fileName, includedSheetNames);

                if (rows.Count == 0)
                {
                    return (
                        false,
                        null,
                        ServiceResult.Validation(
                            "標單中沒有解析到可用的工程明細"));
                }

                return (true, rows, null);
            }
            catch (Exception ex)
            {
                return (
                    false,
                    null,
                    ServiceResult.Validation(
                        "待偵錯標單解析失敗：" +
                        (ex.InnerException?.Message ??
                         ex.Message)));
            }
        }


        private async Task<int> GetNextVersionNoAsync(
            int analysisCaseId,
            CancellationToken cancellationToken)
        {
            int max =
                await _context.AnalysisBidVersions
                    .Where(x =>
                        x.AnalysisCaseId == analysisCaseId)
                    .Select(x => (int?)x.VersionNo)
                    .MaxAsync(cancellationToken)
                    ?? 0;

            return max + 1;
        }


        private async Task ClearCurrentVersionAsync(
            int analysisCaseId,
            CancellationToken cancellationToken)
        {
            var currentVersions =
                await _context.AnalysisBidVersions
                    .Where(x =>
                        x.AnalysisCaseId == analysisCaseId &&
                        x.IsCurrent)
                    .ToListAsync(cancellationToken);

            foreach (var version in currentVersions)
            {
                version.IsCurrent = false;
            }
        }


        private void AddCaseAudit(
            AnalysisCase analysisCase,
            AnalysisBidVersion version,
            string actionType,
            string description,
            string actionUser)
        {
            _auditLogService.AddLog(
                module: "2A待偵錯案件",
                actionType: actionType,
                entityType: "AnalysisCase",
                entityId: analysisCase.AnalysisCaseId,
                description: description,
                actionUser: actionUser,
                newValue: new
                {
                    analysisCase.AnalysisCaseId,
                    analysisCase.CaseName,
                    version.AnalysisBidVersionId,
                    version.VersionNo,
                    version.SourceType,
                    version.SourceOneDProjectId,
                    version.SourceFileName,
                    itemCount = version.Items.Count
                });
        }


        private static AnalysisBidVersionDto ToVersionDto(
            AnalysisBidVersion version)
        {
            return new AnalysisBidVersionDto
            {
                AnalysisBidVersionId =
                    version.AnalysisBidVersionId,
                VersionNo = version.VersionNo,
                SourceType = version.SourceType,
                SourceOneDProjectId =
                    version.SourceOneDProjectId,
                SourceFileName = version.SourceFileName,
                SourceNote = version.SourceNote,
                IsCurrent = version.IsCurrent,
                ItemCount = version.Items.Count,
                CreatedAt = version.CreatedAt
            };
        }


        private static AnalysisBidItemDto ToItemDto(
            AnalysisBidItem item)
        {
            return new AnalysisBidItemDto
            {
                AnalysisBidItemId =
                    item.AnalysisBidItemId,
                SourceSequence = item.SourceSequence,
                SheetName = item.SheetName,
                RowNumber = item.RowNumber,
                ItemCode = item.ItemCode,
                SourceCategory1 = item.SourceCategory1,
                SourceCategory2 = item.SourceCategory2,
                SourceCategory3 = item.SourceCategory3,
                MajorCategory = item.MajorCategory,
                ItemName = item.ItemName,
                Spec = item.Spec,
                Unit = item.Unit,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount,
                SourceConfidence = item.SourceConfidence,
                Note = item.Note
            };
        }


        private static ProjectSimilarityTargetDto
            ToSimilarityTarget(
                AnalysisCase analysisCase)
        {
            return new ProjectSimilarityTargetDto
            {
                ProjectType = analysisCase.ProjectType,
                StructureType = analysisCase.StructureType,
                City = analysisCase.City,
                TotalFloorArea = analysisCase.TotalFloorArea,
                SiteArea = analysisCase.SiteArea,
                FloorCount = analysisCase.FloorCount,
                BasementFloorCount =
                    analysisCase.BasementFloorCount,
                ProjectDate = analysisCase.ProjectDate
            };
        }


        private static string BuildAnalysisText(
            string? itemName,
            string? spec)
        {
            return string.Join(
                " ",
                new[] { itemName, spec }
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim()));
        }


        private static decimal ResolveLineAmount(
            AnalysisBidItem item)
        {
            if (item.Amount.HasValue &&
                item.Amount.Value > 0)
            {
                return item.Amount.Value;
            }

            if (item.Quantity.HasValue &&
                item.Quantity.Value > 0 &&
                item.UnitPrice.HasValue &&
                item.UnitPrice.Value > 0)
            {
                return
                    item.Quantity.Value *
                    item.UnitPrice.Value;
            }

            return 0m;
        }


        private static string? NormalizeOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
