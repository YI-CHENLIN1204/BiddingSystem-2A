using System.Data;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;


namespace 崴正.Services
{
    public class BatchEstimationService
    {
        private readonly EstimationTargetService _targetService;

        private readonly ProjectSimilarityService _similarityService;

        private readonly RangeEstimationService _rangeEstimationService;

        private readonly CoreMaterialService _coreMaterialService;




        public BatchEstimationService(
            EstimationTargetService targetService,
            ProjectSimilarityService similarityService,
            RangeEstimationService rangeEstimationService,
            CoreMaterialService coreMaterialService)
        {


            _targetService =
                targetService;

            _similarityService =
                similarityService;

            _rangeEstimationService =
                rangeEstimationService;

            _coreMaterialService =
                coreMaterialService;
        }


        public async Task<ServiceResult>
            EstimateMaterialsAsync(
                EstimationTargetRequest targetRequest,
                IEnumerable<int> materialIds)
        {
            var requestedMaterialIds =
                materialIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

            if (!requestedMaterialIds.Any())
            {
                return ServiceResult.Validation(
                    "請至少選擇一個標準材料"
                );
            }

            var targetResult =
                await _targetService.ResolveAsync(
                    targetRequest
                );

            if (!targetResult.Success)
            {
                return targetResult;
            }

            if (
                targetResult.Data
                is not ResolvedEstimationTarget resolvedTarget
            )
            {
                return ServiceResult.Validation(
                    "估算目標解析失敗"
                );
            }

            var similarityCandidates =
                await _similarityService
                    .GetScoredCandidateProjectsAsync(
                        resolvedTarget
                    );

            var rangesResult =
                await _rangeEstimationService
                    .EstimateMaterialRangesAsync(
                        resolvedTarget,
                        requestedMaterialIds,
                        similarityCandidates
                    );

            if (!rangesResult.Success)
            {
                return rangesResult;
            }

            if (
                rangesResult.Data
                is not List<RangeEstimationDto> ranges
            )
            {
                return ServiceResult.Validation(
                    "批次估算結果格式錯誤"
                );
            }

            var batchResult =
                new BatchEstimationDto
                {
                    TargetMode =
                        resolvedTarget.SourceMode,

                    TargetProjectId =
                        resolvedTarget.HistoricalProjectId,

                    TargetProjectName =
                        resolvedTarget.DisplayName,

                    Target =
                        resolvedTarget.SimilarityTarget,

                    CandidateProjectCount =
                        similarityCandidates.Count,

                    SimilarityRanking =
                        similarityCandidates,

                    RequestedMaterialCount =
                        requestedMaterialIds.Count,

                    ResultMaterialCount =
                        ranges.Count
                };

            batchResult.Results.AddRange(
                ranges
            );

            return ServiceResult.Ok(
                batchResult
            );
        }


        public async Task<ServiceResult>
            EstimateCoreMaterialsAsync(
                EstimationTargetRequest targetRequest)
        {

            var targetResult =
                await _targetService
                    .ResolveAsync(
                        targetRequest
                    );


            if (!targetResult.Success)
            {
                return targetResult;
            }

            if (
                targetResult.Data
                is not ResolvedEstimationTarget resolvedTarget
            )
            {
                return ServiceResult.Validation(
                    "估算目標解析失敗"
                );
            }

            try
            {

        
                var materialIds =
                    await _coreMaterialService
                        .GetCoreMaterialIdsAsync(
                            resolvedTarget
                        );


                if (!materialIds.Any())
                {
                    return ServiceResult.Validation(
                        $"目前尚未設定「{resolvedTarget.SimilarityTarget.ProjectType} / {resolvedTarget.SimilarityTarget.StructureType}」的核心估算材料"
                    );
                }


                var similarityCandidates =
                    await _similarityService
                        .GetScoredCandidateProjectsAsync(
                            resolvedTarget
                        );


                var rangesResult =
                    await _rangeEstimationService
                        .EstimateMaterialRangesAsync(
                            resolvedTarget,
                            materialIds,
                            similarityCandidates
                        );

                if (!rangesResult.Success)
                {
                    return rangesResult;
                }

                if (
                    rangesResult.Data
                    is not List<RangeEstimationDto> ranges
                )
                {
                    return ServiceResult.Validation(
                        "核心材料批次估算結果格式錯誤"
                    );
                }

                var batchResult =
                    new BatchEstimationDto
                    {
                        TargetMode =
                            resolvedTarget.SourceMode,

                        TargetProjectId =
                            resolvedTarget.HistoricalProjectId,

                        TargetProjectName =
                            resolvedTarget.DisplayName,

                        Target =
                            resolvedTarget.SimilarityTarget,

                        CandidateProjectCount =
                            similarityCandidates.Count,

                        SimilarityRanking =
                            similarityCandidates,

                        RequestedMaterialCount =
                            materialIds.Count,

                        ResultMaterialCount =
                            ranges.Count
                    };

                batchResult.Results.AddRange(
                    ranges
                );


                return ServiceResult.Ok(
                    batchResult
                );
            }
            finally
            {

            }
        }
    }
}