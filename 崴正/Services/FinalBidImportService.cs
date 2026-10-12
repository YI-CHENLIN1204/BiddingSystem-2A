using System.Globalization;
using System.Security.Cryptography;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class FinalBidImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly FinalBidService _finalBidService;
        private readonly AuditLogService _auditLogService;

        public FinalBidImportService(
            ApplicationDbContext context,
            FinalBidService finalBidService,
            AuditLogService auditLogService)
        {
            _context = context;
            _finalBidService = finalBidService;
            _auditLogService = auditLogService;
        }

        public ServiceResult Import(
            FinalBidImportRequest request,
            string actionUser)
        {


            if (request.ExcelBytes == null ||
                request.ExcelBytes.Length == 0)
            {
                return ServiceResult.Validation(
                    "請先選擇 Excel 檔案"
                );
            }

            string extension =
                Path.GetExtension(
                    request.FileName
                ).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".xlsx",
                ".xls",
                ".xlsm",
                ".xlsb",
                ".csv"
            };

            if (!allowedExtensions.Contains(
                    extension))
            {
                return ServiceResult.Validation(
                    "支援格式：.xlsx、.xls、.xlsm、.xlsb、.csv"
                );
            }

            string projectName =
                NormalizeText(
                    request.ProjectName
                );

            string projectType =
                NormalizeText(
                    request.ProjectType
                );

            string structureType =
                NormalizeText(
                    request.StructureType
                );

            string city =
                NormalizeText(
                    request.City
                );

            if (string.IsNullOrWhiteSpace(
                    projectName))
            {
                return ServiceResult.Validation(
                    "請輸入工程專案名稱"
                );
            }

            if (!DateTime.TryParseExact(
                    request.ProjectMonth,
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedProjectMonth))
            {
                return ServiceResult.Validation(
                    "請選擇正確的建案年月"
                );
            }

            if (!IsAllowedValue(
                    projectType,
                    "住宅",
                    "廠房"))
            {
                return ServiceResult.Validation(
                    "請選擇建案類型：住宅或廠房"
                );
            }

            if (!IsAllowedValue(
                    structureType,
                    "SC",
                    "RC",
                    "SRC"))
            {
                return ServiceResult.Validation(
                    "請選擇結構類型：SC、RC 或 SRC"
                );
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                return ServiceResult.Validation(
                    "請輸入縣市"
                );
            }

            if (!request.TotalFloorArea.HasValue ||
                request.TotalFloorArea.Value <= 0)
            {
                return ServiceResult.Validation(
                    "請輸入正確的總樓地板面積"
                );
            }

            if (!request.SiteArea.HasValue ||
                request.SiteArea.Value <= 0)
            {
                return ServiceResult.Validation(
                    "請輸入正確的基地面積"
                );
            }

            if (!request.FloorCount.HasValue ||
                request.FloorCount.Value <= 0)
            {
                return ServiceResult.Validation(
                    "請輸入正確的地上樓層數"
                );
            }

            if (!request.BasementFloorCount.HasValue ||
                request.BasementFloorCount.Value < 0)
            {
                return ServiceResult.Validation(
                    "請輸入正確的地下樓層數"
                );
            }

            string fileHash =
                Convert.ToHexString(
                    SHA256.HashData(
                        request.ExcelBytes
                    )
                ).ToLowerInvariant();

            bool sameFileExists =
                _context.ProjectMasters.Any(x =>
                    !x.IsDeleted &&
                    x.SourceFileHash == fileHash);

            if (sameFileExists)
            {
                return ServiceResult.Conflict(
                    "此最終標單已經回寫過，不能重複上傳。"
                );
            }

            bool sameProjectExists =
                _context.ProjectMasters.Any(x =>
                    !x.IsDeleted &&
                    x.ProjectName == projectName);

            if (sameProjectExists)
            {
                return ServiceResult.Conflict(
                    "此建案已完成決標回寫。如需重新上傳，請先至建案管理刪除原建案。"
                );
            }



            var parser =
                new TenderExcelParser();

            var rows =
                parser.Parse(
                    new MemoryStream(
                        request.ExcelBytes
                    ),
                    request.FileName
                );




            if (rows.Count == 0)
            {
                return ServiceResult.Validation(
                    "標單中沒有解析到可用的工程明細"
                );
            }

            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                DateTime now =
                    DateTime.UtcNow;

                var project =
                    new ProjectMaster
                    {
                        ProjectName =
                            projectName,

                        ProjectType =
                            projectType,

                        StructureType =
                            structureType,

                        City =
                            city,

                        TotalFloorArea =
                            request.TotalFloorArea,

                        SiteArea =
                            request.SiteArea,

                        FloorCount =
                            request.FloorCount.Value,

                        BasementFloorCount =
                            request.BasementFloorCount.Value,

                        StartDate =
                            new DateTime(
                                parsedProjectMonth.Year,
                                parsedProjectMonth.Month,
                                1
                            ),

                        SourceFileName =
                            request.FileName,

                        SourceFileHash =
                            fileHash,

                        ImportedAt =
                            now,

                        IsDeleted =
                            false
                    };

                _context.ProjectMasters.Add(
                    project
                );



                _context.SaveChanges();


       

                var dictionaryList =
                    _context.ItemDictionaries
                        .ToList();

                var materials =
                    _context.MaterialMasters
                        .ToList();



                var materialDictionary =
                    materials.ToDictionary(
                        x => x.MaterialId,
                        x => x
                    );

                var createdItems =
                    new List<FinalBidItem>();

                int autoMappedCount = 0;
                int pendingCount = 0;

                int invalidQuantityCount = 0;
                int invalidUnitPriceCount = 0;
                int unitMismatchCount = 0;

              

                foreach (var row in rows)
                {
                    var matchedDictionary =
                        dictionaryList
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.Keyword
                                ) &&
                                (
                                    (row.ItemName ?? "")
                                        .Contains(
                                            x.Keyword,
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                    ||
                                    (row.Spec ?? "")
                                        .Contains(
                                            x.Keyword,
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                ))
                            .OrderByDescending(
                                x => x.Keyword.Length
                            )
                            .FirstOrDefault();

                    MaterialMaster? material =
                        null;

                    int? materialId =
                        null;

                    string? matchedKeyword =
                        null;

                    string mappingStatus;
                    string? mappingIssueCode =
                        null;

                    string? mappingIssueMessage =
                        null;

                    if (matchedDictionary == null)
                    {
                        mappingStatus =
                            "Pending";

                        mappingIssueCode =
                            "NO_DICTIONARY_MATCH";

                        mappingIssueMessage =
                            "找不到可對應的字典關鍵字";

                        pendingCount++;
                    }
                    else
                    {
                        matchedKeyword =
                            matchedDictionary.Keyword;

                        if (!materialDictionary.TryGetValue(
                                matchedDictionary.Material_Id,
                                out material))
                        {
                            mappingStatus =
                                "Pending";

                            mappingIssueCode =
                                "MATERIAL_NOT_FOUND";

                            mappingIssueMessage =
                                $"字典指向不存在的材料 ID：{matchedDictionary.Material_Id}";

                            pendingCount++;
                        }
                        else
                        {
                            mappingStatus =
                                "Auto";

                            materialId =
                                material.MaterialId;

                            autoMappedCount++;
                        }
                    }

                    var dataIssue =
                        _finalBidService
                            .EvaluateDataIssues(
                                row.Qty,
                                row.UnitPrice,
                                row.Unit,
                                material
                            );

                    if (!row.Qty.HasValue ||
                        row.Qty.Value <= 0)
                    {
                        invalidQuantityCount++;
                    }

                    if (!row.UnitPrice.HasValue ||
                        row.UnitPrice.Value <= 0)
                    {
                        invalidUnitPriceCount++;
                    }

                    if (
                        material != null &&
                        dataIssue.Message != null &&
                        dataIssue.Message.Contains(
                            "標準材料單位"
                        )
                    )
                    {
                        unitMismatchCount++;
                    }

                    var bidItem =
                        new FinalBidItem
                        {
                            ProjectMasterId =
                                project.ProjectId,

                            ProjectMaster =
                                project,

                            SourceFileName =
                                request.FileName,

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
                                row.ItemName ??
                                string.Empty,

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

                            Material =
                                material,

                            MappingStatus =
                                mappingStatus,

                            MappingIssueCode =
                                mappingIssueCode,

                            MappingIssueMessage =
                                mappingIssueMessage,

                            DataIssueCode =
                                dataIssue.Code,

                            DataIssueMessage =
                                dataIssue.Message,

                            IsIgnored =
                                false,

                            MatchedKeyword =
                                matchedKeyword,

                            CreatedAt =
                                now,

                            UpdatedAt =
                                now
                        };

                    _context.FinalBidItems.Add(
                        bidItem
                    );

                    createdItems.Add(
                        bidItem
                    );
                }

  







                _context.SaveChanges();


            int recognizedMajorCategoryCount =
                createdItems.Count(x =>
                    TwoAScopePolicy
                        .ResolveMajorCategory(
                            x.Category1,
                            x.Category2,
                            x.Category3
                        ) != null);

            int unclassifiedMajorCategoryCount =
                createdItems.Count -
                recognizedMajorCategoryCount;

            var structuralItems =
                createdItems
                    .Where(x =>
                        string.Equals(
                            TwoAScopePolicy
                                .ResolveMajorCategory(
                                    x.Category1,
                                    x.Category2,
                                    x.Category3
                                ),
                            "結構體工程",
                            StringComparison.OrdinalIgnoreCase
                        ))
                    .ToList();

            int structuralPricedItemCount =
                structuralItems.Count(x =>
                    (
                        x.Amount.HasValue &&
                        x.Amount.Value > 0
                    ) ||
                    (
                        x.Qty.HasValue &&
                        x.Qty.Value > 0 &&
                        x.UnitPrice.HasValue &&
                        x.UnitPrice.Value > 0
                    ));

            decimal structuralAmount =
                structuralItems.Sum(x =>
                    x.Amount.HasValue &&
                    x.Amount.Value > 0
                        ? x.Amount.Value
                        : x.Qty.HasValue &&
                          x.Qty.Value > 0 &&
                          x.UnitPrice.HasValue &&
                          x.UnitPrice.Value > 0
                            ? x.Qty.Value *
                              x.UnitPrice.Value
                            : 0m);


            int historyInsertCount = 0;



            var histories =
                new List<HistoricalTransaction>();

            foreach (var bidItem in createdItems)
            {
                var history =
                    _finalBidService
                        .BuildHistoricalTransactionForNewImport(
                            bidItem
                        );

                if (history == null)
                {
                    continue;
                }

                histories.Add(
                    history
                );
            }

            historyInsertCount =
                histories.Count;

            int rebarHistoryInsertCount =
                histories.Count(x =>
                    materialDictionary
                        .TryGetValue(
                            x.MaterialId,
                            out var material) &&
                    string.Equals(
                        material.MaterialName,
                        "鋼筋",
                        StringComparison.OrdinalIgnoreCase
                    ));

            int concreteHistoryInsertCount =
                histories.Count(x =>
                    materialDictionary
                        .TryGetValue(
                            x.MaterialId,
                            out var material) &&
                    string.Equals(
                        material.MaterialName,
                        "混凝土",
                        StringComparison.OrdinalIgnoreCase
                    ));

            _context.HistoricalTransactions
                .AddRange(
                    histories
                );


                _auditLogService.AddLog(
                    module:
                        "決標回寫",

                    actionType:
                        "ImportFinalBid",

                    entityType:
                        "ProjectMaster",

                    entityId:
                        project.ProjectId,

                    description:
                        $"回寫決標標單「{project.ProjectName}」",

                    actionUser:
                        actionUser,

                    projectId:
                        project.ProjectId,

                    newValue:
                        new
                        {
                            projectId =
                                project.ProjectId,

                            projectName =
                                project.ProjectName,

                            sourceFileName =
                                project.SourceFileName,

                            totalCount =
                                rows.Count,

                            autoMappedCount,

                            unmappedCount =
                                pendingCount,

                            historyInsertCount,

                            recognizedMajorCategoryCount,
                            unclassifiedMajorCategoryCount,
                            structuralItemCount =
                                structuralItems.Count,
                            structuralPricedItemCount,
                            structuralAmount =
                                Math.Round(
                                    structuralAmount,
                                    2
                                ),
                            rebarHistoryInsertCount,
                            concreteHistoryInsertCount,

                            invalidQuantityCount,

                            invalidUnitPriceCount,

                            unitMismatchCount
                        }
                );


                _context.SaveChanges();


                transaction.Commit();


                return ServiceResult.Ok(new
                {
                    message =
                        "決標標單回寫成功",

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

                    siteArea =
                        project.SiteArea,

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

                    autoMappedCount,

                    unmappedCount =
                        pendingCount,

                    historyInsertCount,

                    recognizedMajorCategoryCount,
                    unclassifiedMajorCategoryCount,
                    structuralItemCount =
                        structuralItems.Count,
                    structuralPricedItemCount,
                    structuralAmount =
                        Math.Round(
                            structuralAmount,
                            2
                        ),
                    rebarHistoryInsertCount,
                    concreteHistoryInsertCount,

                    invalidQuantityCount,

                    invalidUnitPriceCount,

                    unitMismatchCount
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                return ServiceResult.Validation(
                    "決標標單回寫失敗：" +
                    (
                        ex.InnerException?.Message ??
                        ex.Message
                    )
                );
            }

            
        }

        private static string NormalizeText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static bool IsAllowedValue(
            string value,
            params string[] allowedValues)
        {
            return allowedValues.Any(x =>
                string.Equals(
                    x,
                    value,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }
    }
}