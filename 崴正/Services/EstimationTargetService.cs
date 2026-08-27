using System.Globalization;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class EstimationTargetService
    {
        private readonly ApplicationDbContext _context;

        public EstimationTargetService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<ServiceResult>
            ResolveAsync(
                EstimationTargetRequest request)
        {
            return request.TargetMode switch
            {
                EstimationTargetMode.HistoricalProject =>
                    await ResolveHistoricalProjectAsync(
                        request
                    ),

                EstimationTargetMode.ManualInput =>
                    ResolveManualInput(
                        request
                    ),

                _ =>
                    ServiceResult.Validation(
                        "不支援的估算目標模式"
                    )
            };
        }


        private async Task<ServiceResult>
            ResolveHistoricalProjectAsync(
                EstimationTargetRequest request)
        {
            if (!request.HistoricalProjectId.HasValue)
            {
                return ServiceResult.Validation(
                    "請選擇歷史建案"
                );
            }

            var project =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId ==
                            request.HistoricalProjectId.Value &&
                        !x.IsDeleted
                    );

            if (project == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的歷史建案"
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    project.ProjectType) ||
                string.IsNullOrWhiteSpace(
                    project.StructureType) ||
                string.IsNullOrWhiteSpace(
                    project.City) ||
                !project.TotalFloorArea.HasValue ||
                project.TotalFloorArea.Value <= 0 ||
                !project.FloorCount.HasValue ||
                project.FloorCount.Value <= 0 ||
                !project.BasementFloorCount.HasValue ||
                !project.StartDate.HasValue
            )
            {
                return ServiceResult.Validation(
                    "此歷史建案的建案屬性不完整，請先至建案管理修正"
                );
            }

            return ServiceResult.Ok(
                new ResolvedEstimationTarget
                {
                    SourceMode =
                        EstimationTargetMode
                            .HistoricalProject,

                    HistoricalProjectId =
                        project.ProjectId,

                    DisplayName =
                        project.ProjectName,

                    SimilarityTarget =
                        new ProjectSimilarityTargetDto
                        {
                            ProjectType =
                                project.ProjectType,

                            StructureType =
                                project.StructureType,

                            City =
                                project.City,

                            TotalFloorArea =
                                project.TotalFloorArea.Value,

                            FloorCount =
                                project.FloorCount.Value,

                            BasementFloorCount =
                                project.BasementFloorCount.Value,

                            ProjectDate =
                                project.StartDate.Value
                        }
                }
            );
        }


        private ServiceResult ResolveManualInput(
            EstimationTargetRequest request)
        {
            string projectType =
                request.ProjectType?.Trim()
                ?? string.Empty;

            string structureType =
                request.StructureType?.Trim()
                ?? string.Empty;

            string city =
                request.City?.Trim()
                ?? string.Empty;


            if (
                projectType != "住宅" &&
                projectType != "廠房"
            )
            {
                return ServiceResult.Validation(
                    "建案類型只允許住宅或廠房"
                );
            }

            if (
                structureType != "RC" &&
                structureType != "SC" &&
                structureType != "SRC"
            )
            {
                return ServiceResult.Validation(
                    "結構類型只允許 RC、SC 或 SRC"
                );
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                return ServiceResult.Validation(
                    "請選擇縣市"
                );
            }

            if (!request.TotalFloorArea.HasValue ||
                request.TotalFloorArea.Value <= 0)
            {
                return ServiceResult.Validation(
                    "總樓地板面積必須大於 0"
                );
            }

            if (!request.FloorCount.HasValue ||
                request.FloorCount.Value <= 0)
            {
                return ServiceResult.Validation(
                    "地上樓層數必須大於 0"
                );
            }

            if (!request.BasementFloorCount.HasValue ||
                request.BasementFloorCount.Value < 0)
            {
                return ServiceResult.Validation(
                    "地下樓層數不可小於 0"
                );
            }

            if (!DateTime.TryParseExact(
                    request.ProjectMonth,
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var projectDate))
            {
                return ServiceResult.Validation(
                    "請選擇正確的建案年月"
                );
            }

            projectDate =
                new DateTime(
                    projectDate.Year,
                    projectDate.Month,
                    1
                );


            return ServiceResult.Ok(
                new ResolvedEstimationTarget
                {
                    SourceMode =
                        EstimationTargetMode
                            .ManualInput,

                    HistoricalProjectId =
                        null,

                    DisplayName =
                        "新建案估算",

                    SimilarityTarget =
                        new ProjectSimilarityTargetDto
                        {
                            ProjectType =
                                projectType,

                            StructureType =
                                structureType,

                            City =
                                city,

                            TotalFloorArea =
                                request.TotalFloorArea.Value,

                            FloorCount =
                                request.FloorCount.Value,

                            BasementFloorCount =
                                request
                                    .BasementFloorCount
                                    .Value,

                            ProjectDate =
                                projectDate
                        }
                }
            );
        }
    }
}