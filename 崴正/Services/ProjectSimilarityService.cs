using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class ProjectSimilarityService
    {
        private readonly ApplicationDbContext _context;

        public ProjectSimilarityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProjectSimilarityDto>> FindSimilarProjectsAsync(
            int targetProjectId,
            int topCount = 5)
        {
            // 1. 只先取得目標建案本身
            var target = await _context.ProjectMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProjectId == targetProjectId &&
                    !p.IsDeleted);

            if (target == null)
            {
                return new List<ProjectSimilarityDto>();
            }

            // 建案類型是第一層分群條件
            if (string.IsNullOrWhiteSpace(target.ProjectType))
            {
                return new List<ProjectSimilarityDto>();
            }

            var targetType = target.ProjectType;
            var targetStructure = target.StructureType;
            var targetCity = target.City;
            var targetArea = target.TotalFloorArea;
            var targetFloor = target.FloorCount;
            var targetBasementFloor = target.BasementFloorCount;
            var targetYear = target.StartDate?.Year;

            /*
             * 2. PostgreSQL 先依建案類型分群
             *
             * 例如目標是住宅：
             * 只讓住宅進入後續評分。
             *
             * 注意：
             * 這裡還沒有 ToListAsync，
             * 所以資料目前仍留在 PostgreSQL。
             */
            var candidates = _context.ProjectMasters
                .AsNoTracking()
                .Where(p =>
                    !p.IsDeleted &&
                    p.ProjectId != targetProjectId &&
                    p.ProjectType == targetType);

            /*
             * 3. 由 PostgreSQL 計算各項分數
             */
            var scoredQuery = candidates.Select(p => new
            {
                Project = p,

                // 類型已經先分群，所以只要進來就是30分
                TypeScore = 30.0,

                // 結構相同 = 25分
                StructureScore =
                    targetStructure != null &&
                    p.StructureType == targetStructure
                        ? 25.0
                        : 0.0,

                // 城市相同 = 5分
                CityScore =
                    targetCity != null &&
                    p.City == targetCity
                        ? 5.0
                        : 0.0,

                /*
                 * 面積：
                 * 完全相同 = 20
                 * 差距50%以上 = 0
                 */
                AreaScore =
                    targetArea.HasValue &&
                    targetArea.Value > 0 &&
                    p.TotalFloorArea.HasValue
                        ? (
                            Math.Abs(
                                p.TotalFloorArea.Value -
                                targetArea.Value
                            ) / targetArea.Value >= 0.5m

                            ? 0.0

                            : (double)(
                                20m *
                                (
                                    1m -
                                    (
                                        Math.Abs(
                                            p.TotalFloorArea.Value -
                                            targetArea.Value
                                        ) / targetArea.Value
                                    ) / 0.5m
                                )
                            )
                        )
                        : 0.0,

                /*
                * 樓層配置：滿分10分
                *
                * 地上樓層：
                * 每差1層扣0.3分
                *
                * 地下樓層：
                * 每差1層扣1分
                *
                * 最低0分
                */
                FloorScore =
                    targetFloor.HasValue &&
                    targetBasementFloor.HasValue &&
                    p.FloorCount.HasValue &&
                    p.BasementFloorCount.HasValue
                        ? Math.Max(
                            0.0,
                            10.0
                            - Math.Abs(
                                p.FloorCount.Value -
                                targetFloor.Value
                            ) * 0.3
                            - Math.Abs(
                                p.BasementFloorCount.Value -
                                targetBasementFloor.Value
                            ) * 1.0
                        )
                        : 0.0,

                /*
                 * 年份：
                 * 同年 = 10
                 * 差6年以上 = 0
                 */
                YearScore =
                    targetYear.HasValue &&
                    p.StartDate.HasValue
                        ? (
                            Math.Abs(
                                p.StartDate.Value.Year -
                                targetYear.Value
                            ) >= 6

                            ? 0.0

                            : 10.0 *
                              (
                                  1.0 -
                                  Math.Abs(
                                      p.StartDate.Value.Year -
                                      targetYear.Value
                                  ) / 6.0
                              )
                        )
                        : 0.0
            });

            /*
             * 4. PostgreSQL 加總、排序、只取 Top N
             *
             * 到這個 ToListAsync() 才真的執行 SQL。
             */
            var results = await scoredQuery
                .Select(x => new ProjectSimilarityDto
                {
                    ProjectId = x.Project.ProjectId,
                    ProjectName = x.Project.ProjectName,

                    ProjectType = x.Project.ProjectType,
                    StructureType = x.Project.StructureType,
                    City = x.Project.City,

                    TotalFloorArea = x.Project.TotalFloorArea,
                    FloorCount = x.Project.FloorCount,
                    BasementFloorCount = x.Project.BasementFloorCount,

                    ProjectYear = x.Project.StartDate.HasValue
                        ? x.Project.StartDate.Value.Year
                        : null,

                    TypeScore = x.TypeScore,
                    StructureScore = x.StructureScore,
                    AreaScore = x.AreaScore,
                    FloorScore = x.FloorScore,
                    YearScore = x.YearScore,
                    CityScore = x.CityScore,

                    SimilarityScore =
                        x.TypeScore +
                        x.StructureScore +
                        x.AreaScore +
                        x.FloorScore +
                        x.YearScore +
                        x.CityScore
                })
                .OrderByDescending(x => x.SimilarityScore)
                .Take(topCount)
                .ToListAsync();

            // 最後才在C#做顯示用四捨五入
            foreach (var item in results)
            {
                item.TypeScore = Math.Round(item.TypeScore, 2);
                item.StructureScore = Math.Round(item.StructureScore, 2);
                item.AreaScore = Math.Round(item.AreaScore, 2);
                item.FloorScore = Math.Round(item.FloorScore, 2);
                item.YearScore = Math.Round(item.YearScore, 2);
                item.CityScore = Math.Round(item.CityScore, 2);
                item.SimilarityScore = Math.Round(item.SimilarityScore, 2);
            }

            return results;
        }
    }
}