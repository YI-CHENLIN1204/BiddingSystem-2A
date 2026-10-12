using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class ProjectSimilarityService
    {
        private readonly ApplicationDbContext _context;
        private readonly EstimationSettingsService _settingsService;

        public ProjectSimilarityService(
            ApplicationDbContext context,
            EstimationSettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        // ==========================================
        // 給一般畫面使用：
        // 取得 Top K 相似建案
        // ==========================================
        public async Task<List<ProjectSimilarityDto>>
            FindSimilarProjectsAsync(
                int targetProjectId,
                int? topCount = null)
        {
            var settings =
                await _settingsService.GetCurrentAsync();

            int takeCount =
                topCount.HasValue &&
                topCount.Value > 0
                    ? topCount.Value
                    : settings.TopK;

            var candidates =
                await GetScoredCandidateProjectsAsync(
                    targetProjectId
                );

            return candidates
                .Take(takeCount)
                .ToList();
        }

        public async Task<List<ProjectSimilarityDto>>
            FindSimilarProjectsAsync(
                ProjectSimilarityTargetDto target,
                int? topCount = null)
        {
            var settings =
                await _settingsService.GetCurrentAsync();

            int takeCount =
                topCount.HasValue &&
                topCount.Value > 0
                    ? topCount.Value
                    : settings.TopK;

            var candidates =
                await GetScoredCandidateProjectsAsync(
                    target
                );

            return candidates
                .Take(takeCount)
                .ToList();
        }


        // ==========================================
        // 給 RangeEstimationService 使用：
        //
        // 不先取 Top K，
        // 先取得所有已計算相似度的候選建案。
        //
        // 後面可以：
        // 1. 先篩有指定 MaterialId 的建案
        // 2. 再取材料自己的 Top K
        // ==========================================
        private List<ProjectSimilarityDto>
            ScoreCandidates(
                ProjectSimilarityTargetDto target,
                List<ProjectMaster> candidates,
                EstimationSetting settings)
        {
            decimal maximumRawScore =
                settings.StructureWeight +
                settings.AreaWeight +
                settings.SiteAreaWeight +
                settings.FloorWeight +
                settings.YearWeight +
                settings.CityWeight;

            if (maximumRawScore <= 0)
            {
                throw new InvalidOperationException(
                    "相似度權重總和必須大於 0。"
                );
            }

            var results =
                new List<ProjectSimilarityDto>();

            foreach (var candidate in candidates)
            {
                decimal structureScore =
                    CalculateStructureScore(
                        target,
                        candidate,
                        settings
                    );

                decimal areaScore =
                    CalculateAreaScore(
                        target,
                        candidate,
                        settings
                    );

                decimal siteAreaScore =
                    CalculateSiteAreaScore(
                        target,
                        candidate,
                        settings
                    );

                decimal floorScore =
                    CalculateFloorScore(
                        target,
                        candidate,
                        settings
                    );

                decimal yearScore =
                    CalculateYearScore(
                        target,
                        candidate,
                        settings
                    );

                decimal cityScore =
                    CalculateCityScore(
                        target,
                        candidate,
                        settings
                    );

                decimal rawScore =
                    structureScore +
                    areaScore +
                    siteAreaScore +
                    floorScore +
                    yearScore +
                    cityScore;

                decimal similarityScore =
                    rawScore /
                    maximumRawScore *
                    100m;

                if (
                    similarityScore <
                    settings.MinimumSimilarityScore
                )
                {
                    continue;
                }

                results.Add(
                    new ProjectSimilarityDto
                    {
                        ProjectId =
                            candidate.ProjectId,

                        ProjectName =
                            candidate.ProjectName,

                        ProjectType =
                            candidate.ProjectType,

                        StructureType =
                            candidate.StructureType,

                        City =
                            candidate.City,

                        TotalFloorArea =
                            candidate.TotalFloorArea,

                        SiteArea =
                            candidate.SiteArea,

                        FloorCount =
                            candidate.FloorCount,

                        BasementFloorCount =
                            candidate.BasementFloorCount,

                        ProjectYear =
                            candidate.StartDate.HasValue
                                ? candidate
                                    .StartDate
                                    .Value
                                    .Year
                                : null,

                        TypeScore =
                            0,

                        StructureScore =
                            Math.Round(
                                (double)structureScore,
                                2
                            ),

                        AreaScore =
                            Math.Round(
                                (double)areaScore,
                                2
                            ),

                        SiteAreaScore =
                            Math.Round(
                                (double)siteAreaScore,
                                2
                            ),

                        FloorScore =
                            Math.Round(
                                (double)floorScore,
                                2
                            ),

                        YearScore =
                            Math.Round(
                                (double)yearScore,
                                2
                            ),

                        CityScore =
                            Math.Round(
                                (double)cityScore,
                                2
                            ),

                        SimilarityScore =
                            Math.Round(
                                (double)similarityScore,
                                2
                            )
                    }
                );
            }

            return results
                .OrderByDescending(x =>
                    x.SimilarityScore)
                .ThenBy(x =>
                    x.ProjectId)
                .ToList();
        }

        public async Task<List<ProjectSimilarityDto>>
            GetScoredCandidateProjectsAsync(
                int targetProjectId)
        {
            var settings =
                await _settingsService.GetCurrentAsync();

            var targetProject =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.ProjectId ==
                            targetProjectId &&
                        !p.IsDeleted
                    );

            if (targetProject == null)
            {
                return new List<ProjectSimilarityDto>();
            }

            if (
                string.IsNullOrWhiteSpace(
                    targetProject.ProjectType) ||
                string.IsNullOrWhiteSpace(
                    targetProject.StructureType) ||
                string.IsNullOrWhiteSpace(
                    targetProject.City) ||
                !targetProject.TotalFloorArea.HasValue ||
                !targetProject.SiteArea.HasValue ||
                !targetProject.FloorCount.HasValue ||
                !targetProject.BasementFloorCount.HasValue ||
                !targetProject.StartDate.HasValue
            )
            {
                return new List<ProjectSimilarityDto>();
            }

            var target =
                new ProjectSimilarityTargetDto
                {
                    ProjectType =
                        targetProject.ProjectType,

                    StructureType =
                        targetProject.StructureType,

                    City =
                        targetProject.City,

                    TotalFloorArea =
                        targetProject.TotalFloorArea.Value,

                    SiteArea =
                        targetProject.SiteArea.Value,

                    FloorCount =
                        targetProject.FloorCount.Value,

                    BasementFloorCount =
                        targetProject.BasementFloorCount.Value,

                    ProjectDate =
                        targetProject.StartDate.Value
                };

            var candidates =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .Where(p =>
                        !p.IsDeleted &&
                        p.ProjectId !=
                            targetProjectId &&
                        p.ProjectType ==
                            target.ProjectType
                    )
                    .ToListAsync();

            return ScoreCandidates(
                target,
                candidates,
                settings
            );
        }


        public async Task<List<ProjectSimilarityDto>>
            GetScoredCandidateProjectsAsync(
                ProjectSimilarityTargetDto target)
        {
            var settings =
                await _settingsService.GetCurrentAsync();

            ValidateTarget(target);

            var candidates =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .Where(p =>
                        !p.IsDeleted &&
                        p.ProjectType ==
                            target.ProjectType
                    )
                    .ToListAsync();

            return ScoreCandidates(
                target,
                candidates,
                settings
            );
        }

        public Task<List<ProjectSimilarityDto>>
            GetScoredCandidateProjectsAsync(
                ResolvedEstimationTarget target)
        {
            if (
                target.SourceMode ==
                    EstimationTargetMode.HistoricalProject &&
                target.HistoricalProjectId.HasValue
            )
            {
                return GetScoredCandidateProjectsAsync(
                    target.HistoricalProjectId.Value
                );
            }

            return GetScoredCandidateProjectsAsync(
                target.SimilarityTarget
            );
        }


        // ==========================================
        // 結構類型
        // ==========================================
        private static decimal CalculateStructureScore(
            ProjectSimilarityTargetDto target,
            ProjectMaster candidate,
            EstimationSetting settings)
        {
            if (string.IsNullOrWhiteSpace(
                    target.StructureType) ||
                string.IsNullOrWhiteSpace(
                    candidate.StructureType))
            {
                return 0m;
            }

            return string.Equals(
                    target.StructureType,
                    candidate.StructureType,
                    StringComparison.OrdinalIgnoreCase
                )
                ? settings.StructureWeight
                : 0m;
        }


        // ==========================================
        // 總樓地板面積
        //
        // 差距 = 0%
        // → 滿分
        //
        // 差距 >= cutoff
        // → 0 分
        //
        // 中間線性遞減
        // ==========================================
        private static decimal CalculateAreaScore(
            ProjectSimilarityTargetDto target,
            ProjectMaster candidate,
            EstimationSetting settings)
        {
            if (
                target.TotalFloorArea <= 0 ||
                !candidate.TotalFloorArea.HasValue ||
                candidate.TotalFloorArea.Value <= 0
            )
            {
                return 0m;
            }

            decimal differenceRatio =
                Math.Abs(
                    candidate.TotalFloorArea.Value -
                    target.TotalFloorArea
                )
                /
                target.TotalFloorArea;

            if (
                differenceRatio >=
                settings.AreaDifferenceCutoffRatio
            )
            {
                return 0m;
            }

            decimal scoreRatio =
                1m -
                (
                    differenceRatio /
                    settings.AreaDifferenceCutoffRatio
                );

            return
                settings.AreaWeight *
                scoreRatio;
        }


        // ==========================================
        // 基地面積
        // ==========================================
        private static decimal CalculateSiteAreaScore(
            ProjectSimilarityTargetDto target,
            ProjectMaster candidate,
            EstimationSetting settings)
        {
            if (
                target.SiteArea <= 0 ||
                !candidate.SiteArea.HasValue ||
                candidate.SiteArea.Value <= 0
            )
            {
                return 0m;
            }

            decimal differenceRatio =
                Math.Abs(
                    candidate.SiteArea.Value -
                    target.SiteArea
                )
                /
                target.SiteArea;

            if (
                differenceRatio >=
                settings.SiteAreaDifferenceCutoffRatio
            )
            {
                return 0m;
            }

            decimal scoreRatio =
                1m -
                (
                    differenceRatio /
                    settings.SiteAreaDifferenceCutoffRatio
                );

            return
                settings.SiteAreaWeight *
                scoreRatio;
        }


        // ==========================================
        // 樓層配置
        //
        // 地上樓層與地下樓層分開扣分。
        //
        // GroundFloorPenaltyRatio：
        // 每差一層扣 FloorWeight 的幾 %
        //
        // BasementFloorPenaltyRatio：
        // 每差一層扣 FloorWeight 的幾 %
        //
        // 例如：
        // FloorWeight = 10
        // Ground = 0.03
        // → 每差 1 層扣 0.3
        //
        // Basement = 0.10
        // → 每差 1 層扣 1.0
        // ==========================================
        private static decimal CalculateFloorScore(
            ProjectSimilarityTargetDto target,
            ProjectMaster candidate,
            EstimationSetting settings)
        {
            if (
                !candidate.FloorCount.HasValue ||
                !candidate.BasementFloorCount.HasValue
            )
            {
                return 0m;
            }

            int groundDifference =
                Math.Abs(
                    candidate.FloorCount.Value -
                    target.FloorCount
                );

            int basementDifference =
                Math.Abs(
                    candidate.BasementFloorCount.Value -
                    target.BasementFloorCount
                );

            decimal groundPenalty =
                groundDifference *
                settings.FloorWeight *
                settings.GroundFloorPenaltyRatio;

            decimal basementPenalty =
                basementDifference *
                settings.FloorWeight *
                settings.BasementFloorPenaltyRatio;

            decimal score =
                settings.FloorWeight -
                groundPenalty -
                basementPenalty;

            return Math.Max(
                0m,
                score
            );
        }


        // ==========================================
        // 建案年份
        //
        // 同年滿分。
        // 達 cutoff 年以上 → 0 分。
        // 中間線性遞減。
        // ==========================================
        private static decimal CalculateYearScore(
            ProjectSimilarityTargetDto target,
            ProjectMaster candidate,
            EstimationSetting settings)
        {
            if (!candidate.StartDate.HasValue)
            {
                return 0m;
            }

            int yearDifference =
                Math.Abs(
                    candidate.StartDate.Value.Year -
                    target.ProjectDate.Year
                );

            if (
                yearDifference >=
                settings.YearDifferenceCutoffYears
            )
            {
                return 0m;
            }

            decimal scoreRatio =
                1m -
                (
                    (decimal)yearDifference /
                    settings.YearDifferenceCutoffYears
                );

            return
                settings.YearWeight *
                scoreRatio;
        }


        // ==========================================
        // 城市
        // ==========================================
        private static decimal CalculateCityScore(
            ProjectSimilarityTargetDto target,
            ProjectMaster candidate,
            EstimationSetting settings)
        {
            if (
                string.IsNullOrWhiteSpace(
                    target.City) ||
                string.IsNullOrWhiteSpace(
                    candidate.City)
            )
            {
                return 0m;
            }

            return string.Equals(
                    target.City,
                    candidate.City,
                    StringComparison.OrdinalIgnoreCase
                )
                ? settings.CityWeight
                : 0m;
        }

        private static void ValidateTarget(
            ProjectSimilarityTargetDto target)
        {
            if (
                target.ProjectType != "住宅" &&
                target.ProjectType != "廠房"
            )
            {
                throw new ArgumentException(
                    "建案類型只允許住宅或廠房"
                );
            }

            if (
                target.StructureType != "RC" &&
                target.StructureType != "SC" &&
                target.StructureType != "SRC"
            )
            {
                throw new ArgumentException(
                    "結構類型只允許 RC、SC 或 SRC"
                );
            }

            if (string.IsNullOrWhiteSpace(
                    target.City))
            {
                throw new ArgumentException(
                    "縣市不可空白"
                );
            }

            if (target.TotalFloorArea <= 0)
            {
                throw new ArgumentException(
                    "總樓地板面積必須大於 0"
                );
            }

            if (target.SiteArea <= 0)
            {
                throw new ArgumentException(
                    "基地面積必須大於 0"
                );
            }

            if (target.FloorCount <= 0)
            {
                throw new ArgumentException(
                    "地上樓層數必須大於 0"
                );
            }

            if (target.BasementFloorCount < 0)
            {
                throw new ArgumentException(
                    "地下樓層數不可小於 0"
                );
            }

            if (target.ProjectDate == default)
            {
                throw new ArgumentException(
                    "建案年月不可空白"
                );
            }
        }
    }
}