using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class FinalBidService
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitConversionService _unitConversionService;
        private readonly AuditLogService _auditLogService;
        private SafeMaterialDictionary? _historyDictionary;

        public FinalBidService(
            ApplicationDbContext context,
            UnitConversionService unitConversionService,
            AuditLogService auditLogService)
        {
            _context = context;
            _unitConversionService =unitConversionService;

            _auditLogService =auditLogService;
        }

        public ServiceResult UpdateBidItemData(
            long finalBidItemId,
            string? itemName,
            decimal? qty,
            decimal? unitPrice,
            string? unit,
            string actionUser)
        {
            itemName =
                NormalizeText(
                    itemName
                );

            if (string.IsNullOrWhiteSpace(
                    itemName))
            {
                return ServiceResult.Validation(
                    "工項名稱不可空白"
                );
            }


            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                var bidItem =
                    _context.FinalBidItems
                        .Include(x =>
                            x.Material)
                        .Include(x =>
                            x.ProjectMaster)
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                            finalBidItemId);

                if (bidItem == null)
                {
                    return ServiceResult.NotFound(
                        "找不到此工項"
                    );
                }

                if (IsProjectHistoryApproved(
                        bidItem))
                {
                    return ServiceResult.Conflict(
                        "此建案已納入 2A 歷史基準。若要修改工項，請先重新開啟歷史基準審核。"
                    );
                }

                var oldValue = new
                {
                    itemName =
                        bidItem.ItemName,

                    qty =
                        bidItem.Qty,

                    unitPrice =
                        bidItem.UnitPrice,

                    unit =
                        bidItem.Unit,

                    dataIssueCode =
                        bidItem.DataIssueCode,

                    dataIssueMessage =
                        bidItem.DataIssueMessage
                };


                // ==============================
                // 更新工項資料
                // ==============================

                bidItem.ItemName =
                    itemName;

                bidItem.Qty =
                    qty;

                bidItem.UnitPrice =
                    unitPrice;

                bidItem.Unit =
                    NormalizeText(
                        unit
                    );

                bidItem.UpdatedAt =
                    DateTime.UtcNow;


                // ==============================
                // 重新檢查資料品質
                // ==============================

                var dataIssue =
                    EvaluateDataIssues(
                        bidItem.Qty,
                        bidItem.UnitPrice,
                        bidItem.Unit,
                        bidItem.Material
                    );

                bidItem.DataIssueCode =
                    dataIssue.Code;

                bidItem.DataIssueMessage =
                    dataIssue.Message;


                // ==============================
                // 同步模型歷史交易
                // ==============================

                string historyAction =
                    SyncHistoricalTransaction(
                        bidItem
                    );

                _auditLogService.AddLog(
                    module:
                        "決標工項",

                    actionType:
                        "UpdateBidItem",

                    entityType:
                        "FinalBidItem",

                    entityId:
                        bidItem.FinalBidItemId,

                    description:
                        $"更新決標工項「{bidItem.ItemName}」",

                    actionUser:
                        actionUser,

                    projectId:
                        bidItem.ProjectMasterId,

                    oldValue:
                        oldValue,

                    newValue:
                        new
                        {
                            itemName =
                                bidItem.ItemName,

                            qty =
                                bidItem.Qty,

                            unitPrice =
                                bidItem.UnitPrice,

                            unit =
                                bidItem.Unit,

                            dataIssueCode =
                                bidItem.DataIssueCode,

                            dataIssueMessage =
                                bidItem.DataIssueMessage,

                            historyAction
                        }
                );


                _context.SaveChanges();

                transaction.Commit();


                bool historyEligible =
                    IsHistoryEligible(
                        bidItem
                    );


                return ServiceResult.Ok(new
                {
                    message =
                        "工項資料已更新",

                    finalBidItemId =
                        bidItem.FinalBidItemId,

                    itemName =
                        bidItem.ItemName,

                    qty =
                        bidItem.Qty,

                    unitPrice =
                        bidItem.UnitPrice,

                    unit =
                        bidItem.Unit,

                    dataIssueCode =
                        bidItem.DataIssueCode,

                    dataIssueMessage =
                        bidItem.DataIssueMessage,

                    historyEligible,

                    historyAction
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[FinalBidTransaction] Operation failed: " + ex);
                try { transaction.Rollback(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[FinalBidTransaction] Rollback also failed: " + rollbackEx);
                }
                throw;
            }
        }

        // 僅移除「材料數量歷史」對應；保留原始明細與結構體工程價格。
        public ServiceResult ExcludeQuantityHistory(long finalBidItemId, string actionUser)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var bidItem = _context.FinalBidItems
                    .Include(x => x.ProjectMaster)
                    .FirstOrDefault(x => x.FinalBidItemId == finalBidItemId);
                if (bidItem == null) return ServiceResult.NotFound("找不到此工項");
                if (IsProjectHistoryApproved(bidItem))
                    return ServiceResult.Conflict("已核准歷史必須先 Reopen History Review 才能修改。");

                int? previousMaterialId = bidItem.MaterialId;
                bidItem.MaterialId = null;
                bidItem.Material = null;
                bidItem.MappingStatus = "Pending";
                bidItem.MappingIssueCode = "EXCLUDED_FROM_QUANTITY_HISTORY";
                bidItem.MappingIssueMessage = "人工確認為施工、試驗或其他非材料數量工項；保留大項價格。";
                bidItem.MatchedKeyword = null;
                bidItem.IsIgnored = false;
                var issue = EvaluateDataIssues(bidItem.Qty, bidItem.UnitPrice, bidItem.Unit, null);
                bidItem.DataIssueCode = issue.Code;
                bidItem.DataIssueMessage = issue.Message;
                bidItem.UpdatedAt = DateTime.UtcNow;

                var oldTransactions = _context.HistoricalTransactions
                    .Where(x => x.FinalBidItemId == finalBidItemId).ToList();
                foreach (var history in oldTransactions) history.IsDeleted = true;

                _auditLogService.AddLog(
                    module: "決標工項",
                    actionType: "ExcludeQuantityHistory",
                    entityType: "FinalBidItem",
                    entityId: bidItem.FinalBidItemId,
                    description: $"工項「{bidItem.ItemName}」保留結構體價格，僅排除材料數量歷史",
                    actionUser: actionUser,
                    projectId: bidItem.ProjectMasterId,
                    oldValue: new { materialId = previousMaterialId },
                    newValue: new { quantityHistoryExcluded = true, isIgnored = false });

                _context.SaveChanges();
                transaction.Commit();
                return ServiceResult.Ok(new { message = "已保留工程價格並排除材料數量歷史" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[FinalBidTransaction] Operation failed: " + ex);
                try { transaction.Rollback(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[FinalBidTransaction] Rollback also failed: " + rollbackEx);
                }
                throw;
            }
        }

        public ServiceResult IgnoreBidItem(
            long finalBidItemId,string actionUser)
        {
            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                var bidItem = _context.FinalBidItems
                    .Include(x =>
                        x.ProjectMaster)
                    .FirstOrDefault(x =>
                        x.FinalBidItemId ==
                        finalBidItemId);

                if (bidItem == null)
                {
                    return ServiceResult.NotFound(
                        "找不到此工項"
                    );
                }

                if (IsProjectHistoryApproved(
                        bidItem))
                {
                    return ServiceResult.Conflict(
                        "此建案已納入 2A 歷史基準。若要修改工項，請先重新開啟歷史基準審核。"
                    );
                }

                var oldValue = new
                {
                    isIgnored =
                        bidItem.IsIgnored
                };

                bidItem.IsIgnored = true;
                bidItem.UpdatedAt = DateTime.UtcNow;

                var history =
                    _context.HistoricalTransactions
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                            finalBidItemId);

                if (history != null)
                {
                    history.IsDeleted = true;
                }


                _auditLogService.AddLog(
                    module:
                        "決標工項",

                    actionType:
                        "IgnoreBidItem",

                    entityType:
                        "FinalBidItem",

                    entityId:
                        bidItem.FinalBidItemId,

                    description:
                        $"忽略決標工項「{bidItem.ItemName}」",

                    actionUser:
                        actionUser,

                    projectId:
                        bidItem.ProjectMasterId,

                    oldValue:
                        oldValue,

                    newValue:
                        new
                        {
                            isIgnored =
                                bidItem.IsIgnored,

                            historyDeleted =
                                history?.IsDeleted
                        }
                );

                _context.SaveChanges();
                transaction.Commit();

                return ServiceResult.Ok(new
                {
                    message =
                        $"已忽略「{bidItem.ItemName}」"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[FinalBidTransaction] Operation failed: " + ex);
                try { transaction.Rollback(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[FinalBidTransaction] Rollback also failed: " + rollbackEx);
                }
                throw;
            }
        }

        public ServiceResult UnignoreBidItem(
            long finalBidItemId,string actionUser)
        {
            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                var bidItem =
                    _context.FinalBidItems
                        .Include(x => x.Material)
                        .Include(x => x.ProjectMaster)
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                            finalBidItemId);

                if (bidItem == null)
                {
                    return ServiceResult.NotFound(
                        "找不到此工項"
                    );
                }

                if (IsProjectHistoryApproved(
                        bidItem))
                {
                    return ServiceResult.Conflict(
                        "此建案已納入 2A 歷史基準。若要修改工項，請先重新開啟歷史基準審核。"
                    );
                }

                var oldValue = new
                {
                    isIgnored =
                        bidItem.IsIgnored,

                    dataIssueCode =
                        bidItem.DataIssueCode,

                    dataIssueMessage =
                        bidItem.DataIssueMessage
                };

                bidItem.IsIgnored = false;
                bidItem.UpdatedAt = DateTime.UtcNow;

                var dataIssue =
                    EvaluateDataIssues(
                        bidItem.Qty,
                        bidItem.UnitPrice,
                        bidItem.Unit,
                        bidItem.Material
                    );

                bidItem.DataIssueCode =
                    dataIssue.Code;

                bidItem.DataIssueMessage =
                    dataIssue.Message;

                string historyAction =
                    SyncHistoricalTransaction(
                        bidItem
                    );

                _auditLogService.AddLog(
                    module:
                        "決標工項",

                    actionType:
                        "UnignoreBidItem",

                    entityType:
                        "FinalBidItem",

                    entityId:
                        bidItem.FinalBidItemId,

                    description:
                        $"取消忽略決標工項「{bidItem.ItemName}」",

                    actionUser:
                        actionUser,

                    projectId:
                        bidItem.ProjectMasterId,

                    oldValue:
                        oldValue,

                    newValue:
                        new
                        {
                            isIgnored =
                                bidItem.IsIgnored,

                            dataIssueCode =
                                bidItem.DataIssueCode,

                            dataIssueMessage =
                                bidItem.DataIssueMessage,

                            historyAction
                        }
                );

                _context.SaveChanges();

                transaction.Commit();

                return ServiceResult.Ok(new
                {
                    message = "已取消忽略",
                    historyAction
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[FinalBidTransaction] Operation failed: " + ex);
                try { transaction.Rollback(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[FinalBidTransaction] Rollback also failed: " + rollbackEx);
                }
                throw;
            }
        }

        internal bool CanUseMaterialUnit(string? sourceUnit, int targetUnitId) =>
            _unitConversionService.CanConvert(sourceUnit, targetUnitId);

        public (
            string? Code,
            string? Message
        ) EvaluateDataIssues(
            decimal? qty,
            decimal? unitPrice,
            string? unit,
            MaterialMaster? material)
        {
            string? code = null;

            var messages =
                new List<string>();

            if (!qty.HasValue ||
                qty.Value <= 0)
            {
                code ??=
                    "INVALID_QUANTITY";

                messages.Add(
                    "數量空白或小於等於 0"
                );
            }

            if (!unitPrice.HasValue ||
                unitPrice.Value <= 0)
            {
                code ??=
                    "INVALID_UNIT_PRICE";

                messages.Add(
                    "單價空白或小於等於 0"
                );
            }

            if (material != null &&
                !_unitConversionService.CanConvert(
                    unit,
                    material.UnitId))
            {
                code ??=
                    "UNIT_MISMATCH";

                messages.Add(
                    $"標單單位「{unit ?? "空白"}」" +
                    $"無法轉換為標準材料單位「{_unitConversionService.GetUnitName(material.UnitId)}」"
                );
            }

            return (
                code,
                messages.Count > 0
                    ? string.Join(
                        "；",
                        messages)
                    : null
            );
        }

        public string SyncHistoricalTransaction(
            FinalBidItem bidItem)
        {
            var history =
                _context.HistoricalTransactions
                    .FirstOrDefault(x =>
                        x.FinalBidItemId ==
                        bidItem.FinalBidItemId);

            if (!IsProjectHistoryApproved(
                    bidItem))
            {
                if (history != null)
                {
                    history.IsDeleted = true;
                }

                return
                    "歷史基準尚未人工確認，暫不納入模型";
            }

            if (!IsInTwoAHistoryScope(
                    bidItem))
            {
                if (history != null)
                {
                    history.IsDeleted = true;
                }

                return
                    "此工程大項或材料不納入目前 2A 數量歷史基準";
            }

            bool historyEligible =
                IsHistoryEligible(
                    bidItem
                );

            if (!historyEligible)
            {
                if (history != null)
                {
                    history.IsDeleted = true;

                    return
                        "資料不符合條件，既有歷史交易已停用";
                }

                return
                    "資料尚不符合歷史交易條件";
            }

            MaterialMaster? material =
                bidItem.Material;

            if (material == null &&
                bidItem.MaterialId.HasValue)
            {
                material =
                    _context.MaterialMasters
                        .FirstOrDefault(x =>
                            x.MaterialId ==
                            bidItem.MaterialId.Value);
            }

            if (material == null || !material.IsActive)
            {
                if (history != null)
                {
                    history.IsDeleted = true;
                }

                return
                    "找不到標準材料，未建立歷史交易";
            }

            var conversion =
                _unitConversionService.Convert(
                    bidItem.Qty!.Value,
                    0m,
                    bidItem.Unit,
                    material.UnitId
                );

            if (!conversion.Success)
            {
                if (history != null)
                {
                    history.IsDeleted = true;
                }

                return
                    conversion.ErrorMessage ??
                    "單位無法轉換，未建立歷史交易";
            }

            if (history == null)
            {
                history =
                    new HistoricalTransaction
                    {
                        MaterialId =
                            material.MaterialId,

                        ActualQuantity =
                            conversion.Quantity,

                        TransactionDate =
                            bidItem.ProjectMaster.StartDate
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

                _context.HistoricalTransactions.Add(
                    history
                );

                return
                    "建立歷史交易";
            }

            history.MaterialId =
                material.MaterialId;

            history.ActualQuantity =
                conversion.Quantity;

            history.TransactionDate =
                bidItem.ProjectMaster.StartDate
                ?? history.TransactionDate;

            history.ProjectMasterId =
                bidItem.ProjectMasterId;

            history.IsDeleted =
                false;

            return
                "更新歷史交易";
        }

        public bool CanBecomeHistoricalTransaction(
            FinalBidItem bidItem)
        {
            if (!IsHistoryCandidate(
                    bidItem))
            {
                return false;
            }

            MaterialMaster? material =
                bidItem.Material;

            if (material == null &&
                bidItem.MaterialId.HasValue)
            {
                material =
                    _context.MaterialMasters
                        .AsNoTracking()
                        .FirstOrDefault(x =>
                            x.MaterialId ==
                            bidItem.MaterialId.Value);
            }

            if (material == null || !material.IsActive)
            {
                return false;
            }

            var conversion =
                _unitConversionService.Convert(
                    bidItem.Qty!.Value,
                    0m,
                    bidItem.Unit,
                    material.UnitId
                );

            return conversion.Success;
        }

        private bool IsHistoryEligible(
            FinalBidItem bidItem)
        {
            return
                IsProjectHistoryApproved(
                    bidItem
                ) &&
                IsHistoryCandidate(
                    bidItem
                );
        }

        private bool IsHistoryCandidate(
            FinalBidItem bidItem)
        {
            if (!IsInTwoAHistoryScope(
                    bidItem))
            {
                return false;
            }

            if (bidItem.IsIgnored)
            {
                return false;
            }

            bool mappingCompleted =
                bidItem.MaterialId.HasValue &&
                (
                    bidItem.MappingStatus == "Auto" ||
                    bidItem.MappingStatus == "Manual"
                );

            if (!mappingCompleted || !bidItem.Qty.HasValue || bidItem.Qty.Value <= 0)
                return false;

            if (bidItem.MappingStatus == "Manual")
                return true;

            // Recheck legacy Draft Auto rows and aliases that became ambiguous since import.
            _historyDictionary ??= new SafeMaterialDictionary(SafeMaterialDictionary.Read(_context));
            var decision = _historyDictionary.Decide(bidItem.ItemName, bidItem.Spec, bidItem.Unit,
                bidItem.Qty, CanUseMaterialUnit);
            return decision.Match?.MaterialId == bidItem.MaterialId;
        }

        private bool IsProjectHistoryApproved(
            FinalBidItem bidItem)
        {
            string? historyStatus =
                bidItem.ProjectMaster?
                    .HistoryStatus;

            if (string.IsNullOrWhiteSpace(
                    historyStatus))
            {
                historyStatus =
                    _context.ProjectMasters
                        .AsNoTracking()
                        .Where(x =>
                            x.ProjectId ==
                            bidItem.ProjectMasterId)
                        .Select(x =>
                            x.HistoryStatus)
                        .FirstOrDefault();
            }

            return string.Equals(
                historyStatus,
                "Approved",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private bool
            IsInTwoAHistoryScope(
                FinalBidItem bidItem)
        {
            if (TwoAScopePolicy.IsExplicitNonMaterialCharge(bidItem.ItemName))
                return false;

            string? majorCategory =
                TwoAScopePolicy
                    .ResolveEffectiveMajorCategory(
                        bidItem.MajorCategoryOverride,
                        bidItem.Category1,
                        bidItem.Category2,
                        bidItem.Category3
                    );

            if (!TwoAScopePolicy
                .IsQuantityEstimableMajorCategory(
                    majorCategory
                ))
            {
                return false;
            }

            if (!bidItem.MaterialId.HasValue)
            {
                return false;
            }

            string? materialName =
                bidItem.Material?.MaterialName;

            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                materialName =
                    _context.MaterialMasters
                        .AsNoTracking()
                        .Where(x =>
                            x.MaterialId ==
                            bidItem.MaterialId.Value)
                        .Select(x =>
                            x.MaterialName)
                        .FirstOrDefault();
            }

            return TwoAScopePolicy
                .IsQuantityHistoryMaterial(
                    materialName
                );
        }


        public ServiceResult
            UpdateMajorCategoryOverride(
                long finalBidItemId,
                string? majorCategory,
                string actionUser)
        {
            string? normalized =
                string.IsNullOrWhiteSpace(
                    majorCategory)
                    ? null
                    : majorCategory.Trim();

            if (normalized != null &&
                !TwoAScopePolicy
                    .IsKnownMajorCategory(
                        normalized))
            {
                return ServiceResult.Validation(
                    "不支援的工程大項"
                );
            }

            using var transaction =
                _context.Database
                    .BeginTransaction();

            try
            {
                var bidItem =
                    _context.FinalBidItems
                        .Include(x =>
                            x.Material)
                        .Include(x =>
                            x.ProjectMaster)
                        .FirstOrDefault(x =>
                            x.FinalBidItemId ==
                                finalBidItemId);

                if (bidItem == null)
                {
                    return ServiceResult.NotFound(
                        "找不到此工項"
                    );
                }

                if (IsProjectHistoryApproved(
                        bidItem))
                {
                    return ServiceResult.Conflict(
                        "此建案已納入 2A 歷史基準。若要修改工程大項，請先重新開啟歷史基準審核。"
                    );
                }

                string? oldEffective =
                    TwoAScopePolicy
                        .ResolveEffectiveMajorCategory(
                            bidItem.MajorCategoryOverride,
                            bidItem.Category1,
                            bidItem.Category2,
                            bidItem.Category3
                        );

                var oldValue = new
                {
                    majorCategoryOverride =
                        bidItem.MajorCategoryOverride,

                    effectiveMajorCategory =
                        oldEffective
                };

                bidItem.MajorCategoryOverride =
                    normalized;

                bidItem.UpdatedAt =
                    DateTime.UtcNow;

                string historyAction =
                    SyncHistoricalTransaction(
                        bidItem
                    );

                string? effective =
                    TwoAScopePolicy
                        .ResolveEffectiveMajorCategory(
                            bidItem.MajorCategoryOverride,
                            bidItem.Category1,
                            bidItem.Category2,
                            bidItem.Category3
                        );

                _auditLogService.AddLog(
                    module:
                        "決標工項",

                    actionType:
                        "UpdateMajorCategory",

                    entityType:
                        "FinalBidItem",

                    entityId:
                        bidItem.FinalBidItemId,

                    description:
                        $"更新決標工項「{bidItem.ItemName}」工程大項",

                    actionUser:
                        actionUser,

                    projectId:
                        bidItem.ProjectMasterId,

                    oldValue:
                        oldValue,

                    newValue:
                        new
                        {
                            majorCategoryOverride =
                                bidItem.MajorCategoryOverride,

                            effectiveMajorCategory =
                                effective,

                            historyAction
                        }
                );

                _context.SaveChanges();
                transaction.Commit();

                return ServiceResult.Ok(
                    new
                    {
                        message =
                            "工程大項已更新",

                        finalBidItemId =
                            bidItem.FinalBidItemId,

                        majorCategoryOverride =
                            bidItem.MajorCategoryOverride,

                        effectiveMajorCategory =
                            effective,

                        historyAction
                    }
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[FinalBidTransaction] Operation failed: " + ex);
                try { transaction.Rollback(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[FinalBidTransaction] Rollback also failed: " + rollbackEx);
                }
                throw;
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