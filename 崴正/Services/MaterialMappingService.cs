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

        public string UnitName { get; set; }
            = string.Empty;

        public int UnitId { get; set; }

        public string? Category { get; set; }
    }
    public class MaterialMappingService
    {
        private readonly ApplicationDbContext _context;
        private readonly FinalBidService _finalBidService;
        private readonly AuditLogService _auditLogService;

        public MaterialMappingService(
            ApplicationDbContext context,
            FinalBidService finalBidService,
            AuditLogService auditLogService)
        {
            _context = context;
            _finalBidService =finalBidService;
            _auditLogService =auditLogService;
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

                if (string.Equals(
                        bidItem.ProjectMaster.HistoryStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult.Conflict(
                        "此建案已納入 2A 歷史基準。若要修改材料對應，請先重新開啟歷史基準審核。"
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

                if (!material.IsActive)
                {
                    return ServiceResult.Validation(
                        "此標準材料目前已停用，無法建立新的人工對應"
                    );
                }

                int? oldMaterialId =
                    bidItem.MaterialId;

                string oldStatus =
                    bidItem.MappingStatus;

                string? keyword = SafeMaterialDictionary.QualifiedKeyword(bidItem.ItemName, bidItem.Spec);
                string? dictionaryWarning = null;

                if (keyword is not null && bidItem.Qty > 0 &&
                    _finalBidService.CanUseMaterialUnit(bidItem.Unit, material.UnitId))
                {
                    dictionaryWarning = UpdateDictionary(keyword, bidItem, material);
                }
                else
                {
                    dictionaryWarning = "已儲存本筆人工對應。規格、數量或單位不足，未學習字典。";
                }

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
                            bidItem
                        );

                _auditLogService.AddLog(
                    module:
                        "材料對應",

                    actionType:
                        "UpdateMapping",

                    entityType:
                        "FinalBidItem",

                    entityId:
                        bidItem.FinalBidItemId,

                    description:
                        $"將工項「{bidItem.ItemName}」" +
                        $"人工對應至「{material.MaterialName}」",

                    actionUser:
                        actionUser,

                    projectId:
                        bidItem.ProjectMasterId,

                    oldValue:
                        new
                        {
                            materialId =
                                oldMaterialId,

                            mappingStatus =
                                oldStatus
                        },

                    newValue:
                        new
                        {
                            materialId =
                                material.MaterialId,

                            materialName =
                                material.MaterialName,

                            mappingStatus =
                                "Manual",
                            dictionaryWarning
                        }
                );

                _context.SaveChanges();
                transaction.Commit();

                return ServiceResult.Ok(new
                {
                    message =
                        $"「{bidItem.ItemName}」已人工對應至「{material.MaterialName}」。{dictionaryWarning}",

                    dictionaryWarning,

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

                        UnitName =
                            material.UnitMaster.UnitName,

                        UnitId =
                            material.UnitId,

                        Category =material.MaterialCategory.CategoryName
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

        private string? UpdateDictionary(string keyword, FinalBidItem item, MaterialMaster material)
        {
            // Conflicting confirmations preserve both candidates; they never overwrite an approval.
            var entries = SafeMaterialDictionary.Read(_context).ToList();
            if (!entries.Any(x => SafeMaterialDictionary.Normalize(x.Keyword) == keyword &&
                                  x.MaterialId == material.MaterialId))
            {
                entries.Add(new DictionaryMaterial(keyword, material.MaterialId, material.UnitId, material.IsActive));
                _context.ItemDictionaries.Add(new ItemDictionary { Keyword = keyword, Material_Id = material.MaterialId });
            }

            var decision = new SafeMaterialDictionary(entries).Decide(item.ItemName, item.Spec, item.Unit, item.Qty,
                _finalBidService.CanUseMaterialUnit);
            return decision.Code == "DICTIONARY_CONFLICT"
                ? "本筆人工對應已儲存。字典保留不同材料候選，後續匯入會待審。"
                : null;
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