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
                        bidItem,
                        dataIssue.Code
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
                        bidItem,
                        dataIssue.Code
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
            catch
            {
                transaction.Rollback();

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
                    .FirstOrDefault(x =>
                        x.FinalBidItemId ==
                        finalBidItemId);

                if (bidItem == null)
                {
                    return ServiceResult.NotFound(
                        "找不到此工項"
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
            catch
            {
                transaction.Rollback();
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
                        bidItem,
                        dataIssue.Code
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
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

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

        public HistoricalTransaction?
            BuildHistoricalTransactionForNewImport(
                FinalBidItem bidItem,
                string? dataIssueCode)
        {
            if (!IsHistoryEligible(
                    bidItem,
                    dataIssueCode))
            {
                return null;
            }

            var material =
                bidItem.Material;

            if (material == null)
            {
                return null;
            }

            var conversion =
                _unitConversionService.Convert(
                    bidItem.Qty!.Value,
                    bidItem.UnitPrice!.Value,
                    bidItem.Unit,
                    material.UnitId
                );

            if (!conversion.Success)
            {
                return null;
            }

            return new HistoricalTransaction
            {
                MaterialId =
                    material.MaterialId,

                ActualQuantity =
                    conversion.Quantity,

                UnitPrice =
                    conversion.UnitPrice,

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
        }

        public string SyncHistoricalTransaction(
            FinalBidItem bidItem,
            string? dataIssueCode)
        {
            var history =
                _context.HistoricalTransactions
                    .FirstOrDefault(x =>
                        x.FinalBidItemId ==
                        bidItem.FinalBidItemId);

            bool historyEligible =
                IsHistoryEligible(
                    bidItem,
                    dataIssueCode
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

            if (material == null)
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
                    bidItem.UnitPrice!.Value,
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

                        UnitPrice =
                            conversion.UnitPrice,

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

            history.UnitPrice =
                conversion.UnitPrice;

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

        private static bool IsHistoryEligible(
            FinalBidItem bidItem,
            string? dataIssueCode)
        {
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

            return
                mappingCompleted &&
                dataIssueCode == null &&
                bidItem.Qty.HasValue &&
                bidItem.Qty.Value > 0 &&
                bidItem.UnitPrice.HasValue &&
                bidItem.UnitPrice.Value > 0
                ;
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