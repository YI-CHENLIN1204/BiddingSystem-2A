using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class CoreMaterialService
    {
        private readonly ApplicationDbContext _context;
        

        public CoreMaterialService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // 取得某種建案情境下的核心材料 ID
        // 給 BatchEstimationService 使用
        // =====================================================
        public async Task<List<int>>
            GetCoreMaterialIdsAsync(
                ResolvedEstimationTarget target)
        {
            var projectType =
                target.SimilarityTarget.ProjectType;

            var structureType =
                target.SimilarityTarget.StructureType;


            return await _context.QuantityRuleBases

                .AsNoTracking()

                .Where(r =>
                    r.Is_Core_Estimation &&

                    (
                        string.IsNullOrEmpty(r.Project_Type) ||
                        r.Project_Type == projectType
                    ) &&

                    (
                        string.IsNullOrEmpty(r.Structure_Type) ||
                        r.Structure_Type == structureType
                    )
                )

                .Select(r =>
                    r.Material_ID
                )

                .Distinct()

                .ToListAsync();
        }

        // =====================================================
        // 估算頁使用：取得已啟用的核心材料
        //
        // 只讀核心材料設定，不統計歷史資料。
        // =====================================================
        public async Task<List<CoreMaterialDto>>
            GetCoreMaterialOptionsAsync()
        {
            var data =
                await (
                    from r in _context.QuantityRuleBases
                        .AsNoTracking()

                    join m in _context.MaterialMasters
                        .AsNoTracking()

                    on r.Material_ID
                    equals m.MaterialId

                    where r.Is_Core_Estimation

                    select new CoreMaterialDto
                    {
                        MaterialId =
                            m.MaterialId,

                        MaterialName =
                            m.MaterialName,

                        BaseUnit =
                            m.BaseUnit,

                        Category =
                            m.Category,

                        ProjectType =
                            r.Project_Type,

                        StructureType =
                            r.Structure_Type,

                        IsCoreEstimation =
                            true
                    }
                )
                .ToListAsync();


            return data
                .GroupBy(x => new
                {
                    x.MaterialId,
                    x.ProjectType,
                    x.StructureType
                })
                .Select(g => g.First())
                .OrderBy(x =>
                    x.Category
                )
                .ThenBy(x =>
                    x.MaterialName
                )
                .ToList();
        }
        
        // =====================================================
        // 取得參數管理畫面所需的材料設定
        // =====================================================
        public async Task<List<CoreMaterialDto>>
            GetMaterialSettingsAsync()
        {
            // =====================================================
            // 1. 取得目前 QuantityRuleBase 中的材料情境設定
            //
            // 同一材料可能存在：
            //
            // SD420 / 住宅 / RC
            // SD420 / 住宅 / SRC
            // SD420 / 廠房 / RC
            // SD420 / 廠房 / SC
            //
            // 這些是不同情境，不合併成一筆。
            // =====================================================

            var settingData =
                await (
                    from r in _context.QuantityRuleBases
                        .AsNoTracking()

                    join m in _context.MaterialMasters
                        .AsNoTracking()

                    on r.Material_ID
                    equals m.MaterialId

                    select new CoreMaterialDto
                    {
                        MaterialId =
                            m.MaterialId,

                        MaterialName =
                            m.MaterialName,

                        BaseUnit =
                            m.BaseUnit,

                        Category =
                            m.Category,

                        ProjectType =
                            r.Project_Type,

                        StructureType =
                            r.Structure_Type,

                        IsCoreEstimation =
                            r.Is_Core_Estimation
                    }
                )
                .ToListAsync();


            // =====================================================
            // 2. 同材料 + 同建案類型 + 同結構類型
            //    合併 QuantityRuleBase 的重複 FloorZone 規則
            //
            // 例如：
            //
            // SD420 / 住宅 / RC / 全棟
            // SD420 / 住宅 / RC / 地下層
            //
            // 核心材料管理畫面只顯示：
            //
            // SD420 / 住宅 / RC
            //
            // 一筆。
            // =====================================================

            var groupedSettings =
                settingData
                    .GroupBy(x => new
                    {
                        x.MaterialId,
                        x.ProjectType,
                        x.StructureType
                    })
                    .Select(g =>
                        new CoreMaterialDto
                        {
                            MaterialId =
                                g.Key.MaterialId,

                            MaterialName =
                                g.First().MaterialName,

                            BaseUnit =
                                g.First().BaseUnit,

                            Category =
                                g.First().Category,

                            ProjectType =
                                g.Key.ProjectType,

                            StructureType =
                                g.Key.StructureType,

                            IsCoreEstimation =
                                g.Any(x =>
                                    x.IsCoreEstimation)
                        })
                    .ToList();


            // =====================================================
            // 3. 計算 HistoricalTransaction 的有效資料量
            //
            // 有效條件與 Range Estimation 一致：
            //
            // - HistoricalTransaction 未刪除
            // - 不是 Outlier
            // - ActualQuantity > 0
            // - ProjectMaster 未刪除
            //
            // 依：
            //
            // Material
            // + ProjectType
            // + StructureType
            //
            // 分開統計。
            // =====================================================

            var historyStats =
                await (
                    from h in _context.HistoricalTransactions
                        .AsNoTracking()

                    where
                        h.ProjectMasterId.HasValue &&
                        !h.IsDeleted &&
                        !h.IsOutlier &&
                        h.ActualQuantity > 0

                    join p in _context.ProjectMasters
                        .AsNoTracking()

                    on h.ProjectMasterId
                    equals (int?)p.ProjectId

                    where !p.IsDeleted

                    group h by new
                    {
                        h.MaterialId,
                        p.ProjectType,
                        p.StructureType
                    }
                    into g

                    select new
                    {
                        MaterialId =
                            g.Key.MaterialId,

                        ProjectType =
                            g.Key.ProjectType,

                        StructureType =
                            g.Key.StructureType,

                        // 真正模型樣本量
                        ValidProjectCount =
                            g.Select(x =>
                                x.ProjectMasterId
                            )
                            .Distinct()
                            .Count(),

                        // 原始有效歷史列數
                        HistoryRowCount =
                            g.Count()
                    }
                )
                .ToListAsync();


            // =====================================================
            // 4. 將統計數量放回每個情境
            //
            // 如果 QuantityRuleBase 是：
            //
            // 住宅 + RC
            //
            // 就只計算：
            //
            // 住宅 + RC
            //
            // 不會把住宅 SRC、廠房 RC、廠房 SC 加進來。
            //
            // 如果 ProjectType / StructureType 是空值，
            // 才視為通用設定並統計符合的所有情境。
            // =====================================================

            foreach (var material in groupedSettings)
            {
                var matchingStats =
                    historyStats
                        .Where(x =>
                            x.MaterialId ==
                                material.MaterialId &&

                            (
                                string.IsNullOrWhiteSpace(
                                    material.ProjectType
                                ) ||
                                x.ProjectType ==
                                    material.ProjectType
                            ) &&

                            (
                                string.IsNullOrWhiteSpace(
                                    material.StructureType
                                ) ||
                                x.StructureType ==
                                    material.StructureType
                            )
                        )
                        .ToList();


                material.ValidProjectCount =
                    matchingStats.Sum(x =>
                        x.ValidProjectCount
                    );


                material.HistoryRowCount =
                    matchingStats.Sum(x =>
                        x.HistoryRowCount
                    );
            }


            // =====================================================
            // 5. 預設依有效歷史建案數由多到少
            //
            // 讓管理者第一眼看到最有資料價值的材料。
            // =====================================================

            return groupedSettings
                .OrderByDescending(x =>
                    x.ValidProjectCount)

                .ThenByDescending(x =>
                    x.HistoryRowCount)

                .ThenBy(x =>
                    x.Category)

                .ThenBy(x =>
                    x.MaterialName)

                .ToList();
        }

        public async Task<ServiceResult>
            UpdateCoreStatusAsync(
                CoreMaterialUpdateRequest request)
        {
            if (request.MaterialId <= 0)
            {
                return ServiceResult.Validation(
                    "材料編號不正確"
                );
            }


            var projectType =
                string.IsNullOrWhiteSpace(
                    request.ProjectType
                )
                    ? null
                    : request.ProjectType.Trim();


            var structureType =
                string.IsNullOrWhiteSpace(
                    request.StructureType
                )
                    ? null
                    : request.StructureType.Trim();


            var rules =
                await _context.QuantityRuleBases

                    .Where(r =>
                        r.Material_ID ==
                            request.MaterialId &&

                        (
                            r.Project_Type ==
                                projectType ||

                            (
                                projectType == null &&
                                (
                                    r.Project_Type == null ||
                                    r.Project_Type == ""
                                )
                            )
                        ) &&

                        (
                            r.Structure_Type ==
                                structureType ||

                            (
                                structureType == null &&
                                (
                                    r.Structure_Type == null ||
                                    r.Structure_Type == ""
                                )
                            )
                        )
                    )

                    .ToListAsync();


            if (!rules.Any())
            {
                return ServiceResult.NotFound(
                    "找不到對應的材料情境設定"
                );
            }


            foreach (var rule in rules)
            {
                rule.Is_Core_Estimation =
                    request.IsCoreEstimation;
            }


            await _context.SaveChangesAsync();


            return ServiceResult.Ok(
                new
                {
                    request.MaterialId,

                    ProjectType =
                        projectType,

                    StructureType =
                        structureType,

                    request.IsCoreEstimation,

                    UpdatedRuleCount =
                        rules.Count
                }
            );
        }
    }
}