using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{

    public class MaterialMappingLookupItem
    {
        public string Keyword { get; set; }
            = string.Empty;

        public int MaterialId { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;

        public string BaseUnit { get; set; }
            = string.Empty;

        public string? Category { get; set; }
    }
    public class MaterialMappingService
    {
        private readonly ApplicationDbContext _context;
        private readonly FinalBidService _finalBidService;

        public MaterialMappingService(
            ApplicationDbContext context,
            FinalBidService finalBidService)
        {
            _context = context;
            _finalBidService =
                finalBidService;
        }

        public ServiceResult CreateMaterial(
            string materialName,
            string category,
            string baseUnit)
        {
            materialName =
                NormalizeText(materialName);

            category =
                NormalizeText(category);

            baseUnit =
                NormalizeText(baseUnit);

            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return ServiceResult.Validation(
                    "請輸入標準材料名稱"
                );
            }

            if (string.IsNullOrWhiteSpace(
                    category))
            {
                return ServiceResult.Validation(
                    "請選擇材料分類"
                );
            }

            if (string.IsNullOrWhiteSpace(
                    baseUnit))
            {
                return ServiceResult.Validation(
                    "請選擇標準單位"
                );
            }

            bool exists =
                _context.MaterialMasters.Any(x =>
                    EF.Functions.ILike(
                        x.MaterialName,
                        materialName
                    ) &&
                    x.Category == category &&
                    x.BaseUnit == baseUnit
                );

            if (exists)
            {
                return ServiceResult.Conflict(
                    "相同名稱、分類與單位的標準材料已經存在"
                );
            }

            var material =
                new MaterialMaster
                {
                    MaterialName =
                        materialName,

                    Category =
                        category,

                    BaseUnit =
                        baseUnit
                };

            _context.MaterialMasters.Add(
                material
            );

            _context.SaveChanges();

            return ServiceResult.Ok(new
            {
                message =
                    $"已新增標準材料「{material.MaterialName}」",

                materialId =
                    material.MaterialId,

                materialName =
                    material.MaterialName,

                category =
                    material.Category,

                baseUnit =
                    material.BaseUnit
            });
        }

        public ServiceResult UpdateMaterialMapping(
            long finalBidItemId,
            int materialId,
            string actionUser)
        {
            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                var bidItem =
                    _context.FinalBidItems
                        .Include(x =>
                            x.ProjectMaster)
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                            finalBidItemId);

                if (bidItem == null)
                {
                    return ServiceResult.NotFound(
                        "找不到此決標標單工項"
                    );
                }

                var material =
                    _context.MaterialMasters
                        .FirstOrDefault(x =>
                            x.MaterialId ==
                            materialId);

                if (material == null)
                {
                    return ServiceResult.Validation(
                        "找不到指定的標準材料"
                    );
                }

                int? oldMaterialId =
                    bidItem.MaterialId;

                string oldStatus =
                    bidItem.MappingStatus;

                string keyword =
                    !string.IsNullOrWhiteSpace(
                        bidItem.ItemName)
                        ? bidItem.ItemName.Trim()
                        : bidItem.Spec?.Trim()
                            ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        keyword))
                {
                    return ServiceResult.Validation(
                        "工項名稱與規格皆為空，無法建立字典"
                    );
                }

                UpdateDictionary(
                    keyword,
                    material.MaterialId
                );

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

                var dataIssue =
                    _finalBidService
                        .EvaluateDataIssues(
                            bidItem.Qty,
                            bidItem.UnitPrice,
                            bidItem.Unit,
                            material
                        );

                bidItem.DataIssueCode =
                    dataIssue.Code;

                bidItem.DataIssueMessage =
                    dataIssue.Message;

                bidItem.UpdatedAt =
                    DateTime.UtcNow;

                string historyAction =
                    _finalBidService
                        .SyncHistoricalTransaction(
                            bidItem,
                            dataIssue.Code
                        );

                var log =
                    new AuditLog
                    {
                        SourceFileName =
                            bidItem.SourceFileName,

                        SheetName =
                            bidItem.SheetName ??
                            string.Empty,

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
                            string.IsNullOrWhiteSpace(
                                actionUser)
                                ? "Estimator_Staff"
                                : actionUser,

                        CreatedAt =
                            DateTime.UtcNow
                    };

                _context.AuditLogs.Add(
                    log
                );

                _context.SaveChanges();
                transaction.Commit();

                return ServiceResult.Ok(new
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

                    dataIssueCode =
                        bidItem.DataIssueCode,

                    dataIssueMessage =
                        bidItem.DataIssueMessage,

                    historyAction
                });
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ==========================================
        // 異常偵測 / 暫存 Excel 專用
        //
        // 一次把「字典 + 標準材料」載入記憶體。
        // 只讀取，不修改任何資料。
        // ==========================================
        public async Task<List<MaterialMappingLookupItem>>
            GetReadOnlyLookupAsync()
        {
            var lookup =
                await (
                    from dictionary
                        in _context.ItemDictionaries
                            .AsNoTracking()

                    join material
                        in _context.MaterialMasters
                            .AsNoTracking()

                    on dictionary.Material_Id
                        equals material.MaterialId

                    where
                        dictionary.Keyword != null &&
                        dictionary.Keyword != ""

                    select new MaterialMappingLookupItem
                    {
                        Keyword =
                            dictionary.Keyword,

                        MaterialId =
                            material.MaterialId,

                        MaterialName =
                            material.MaterialName,

                        BaseUnit =
                            material.BaseUnit,

                        Category =
                            material.Category
                    }
                )
                .ToListAsync();


            return lookup;
        }


        // ==========================================
        // 從唯讀 Lookup 找最適合的標準材料
        //
        // Excel：
        // ItemName 或 Spec
        //      ↓
        // ItemDictionary.Keyword
        //
        // 若同時命中多個 Keyword，
        // 優先採用較長、較具體的 Keyword。
        // ==========================================
        public MaterialMappingLookupItem?
            FindBestMatch(
                IReadOnlyCollection<MaterialMappingLookupItem> lookup,
                string? itemName,
                string? spec)
        {
            string normalizedItemName =
                NormalizeText(itemName);

            string normalizedSpec =
                NormalizeText(spec);


            if (
                string.IsNullOrWhiteSpace(normalizedItemName) &&
                string.IsNullOrWhiteSpace(normalizedSpec)
            )
            {
                return null;
            }


            var matched =
                lookup
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.Keyword
                        )
                        &&
                        (
                            (
                                !string.IsNullOrWhiteSpace(
                                    normalizedItemName
                                )
                                &&
                                normalizedItemName.Contains(
                                    x.Keyword,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    normalizedSpec
                                )
                                &&
                                normalizedSpec.Contains(
                                    x.Keyword,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                        )
                    )

                    // ------------------------------------------
                    // 例如同時命中：
                    //
                    // 「鋼筋」
                    // 「SD420鋼筋」
                    //
                    // 優先使用「SD420鋼筋」
                    // ------------------------------------------
                    .OrderByDescending(x =>
                        x.Keyword.Length
                    )

                    .FirstOrDefault();


            return matched;
        }

        private void UpdateDictionary(
            string keyword,
            int materialId)
        {
            var existingRows =
                _context.ItemDictionaries
                    .Where(x =>
                        x.Keyword == keyword)
                    .ToList();

            if (existingRows.Count == 0)
            {
                _context.ItemDictionaries.Add(
                    new ItemDictionary
                    {
                        Keyword =
                            keyword,

                        Material_Id =
                            materialId
                    }
                );

                return;
            }

            foreach (
                var dictionaryRow
                in existingRows)
            {
                dictionaryRow.Material_Id =
                    materialId;
            }
        }

        private static string NormalizeText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}