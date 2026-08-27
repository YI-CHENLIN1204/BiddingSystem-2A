using 崴正.Models;

namespace 崴正.Services
{
    public class BatchEstimationService
    {
        private readonly EstimationTargetService
            _targetService;

        private readonly ProjectSimilarityService
            _similarityService;

        private readonly RangeEstimationService
            _rangeEstimationService;


        public BatchEstimationService(
            EstimationTargetService targetService,
            ProjectSimilarityService similarityService,
            RangeEstimationService rangeEstimationService)
        {
            _targetService =
                targetService;

            _similarityService =
                similarityService;

            _rangeEstimationService =
                rangeEstimationService;
        }


        public async Task<ServiceResult>
            EstimateMaterialsAsync(
                EstimationTargetRequest targetRequest,
                IEnumerable<int> materialIds)
        {
            // ==========================================
            // 1. 整理材料清單
            // ==========================================

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


            // ==========================================
            // 2. Target 只解析一次
            // ==========================================

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


            // ==========================================
            // 3. Similarity 只計算一次
            // ==========================================

            var similarityCandidates =
                await _similarityService
                    .GetScoredCandidateProjectsAsync(
                        resolvedTarget
                    );


            // ==========================================
            // 4. 建立批次結果
            // ==========================================

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

                    RequestedMaterialCount =
                        requestedMaterialIds.Count
                };


            // ==========================================
            // 5. 每種材料使用同一批 Similarity
            // ==========================================

            foreach (
                int materialId
                in requestedMaterialIds)
            {
                var rangeResult =
                    await _rangeEstimationService
                        .EstimateMaterialRangeAsync(
                            resolvedTarget,
                            materialId,
                            similarityCandidates
                        );

                if (!rangeResult.Success)
                {
                    return rangeResult;
                }

                if (
                    rangeResult.Data
                    is not RangeEstimationDto range
                )
                {
                    return ServiceResult.Validation(
                        $"材料 {materialId} 的估算結果格式錯誤"
                    );
                }

                batchResult.Results.Add(
                    range
                );
            }


            batchResult.ResultMaterialCount =
                batchResult.Results.Count;


            return ServiceResult.Ok(
                batchResult
            );
        }
    }
}