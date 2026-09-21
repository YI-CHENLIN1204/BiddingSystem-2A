using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    public class QuantityRuleController : Controller
    {
        private readonly EstimationSettingsService
            _estimationSettingsService;

        private readonly CoreMaterialService
            _coreMaterialService;


        public QuantityRuleController(
            EstimationSettingsService estimationSettingsService,
            CoreMaterialService coreMaterialService)
        {
            _estimationSettingsService =
                estimationSettingsService;

            _coreMaterialService =
                coreMaterialService;
        }


        // ==========================================
        // 參數管理首頁
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var settings =
                await _estimationSettingsService
                    .GetCurrentAsync();

            var coreMaterials =
                await _coreMaterialService
                    .GetMaterialSettingsAsync();


            var viewModel =
                new QuantityRuleManagementViewModel
                {
                    EstimationSettings =
                        new EstimationSettingsFormViewModel
                        {
                            TopK =
                                settings.TopK,

                            MinimumSampleCount =
                                settings.MinimumSampleCount,


                            LowerQuantilePercent =
                                settings.LowerQuantile *
                                100m,

                            MedianQuantilePercent =
                                settings.MedianQuantile *
                                100m,

                            UpperQuantilePercent =
                                settings.UpperQuantile *
                                100m,


                            SimilarityExponent =
                                settings.SimilarityExponent,

                            TimeHalfLifeYears =
                                settings.TimeHalfLifeYears,

                            MinimumSimilarityScore =
                                settings.MinimumSimilarityScore,


                            StructureWeight =
                                settings.StructureWeight,

                            AreaWeight =
                                settings.AreaWeight,

                            FloorWeight =
                                settings.FloorWeight,

                            YearWeight =
                                settings.YearWeight,

                            CityWeight =
                                settings.CityWeight,


                            AreaDifferenceCutoffPercent =
                                settings.AreaDifferenceCutoffRatio *
                                100m,

                            GroundFloorPenaltyPercent =
                                settings.GroundFloorPenaltyRatio *
                                100m,

                            BasementFloorPenaltyPercent =
                                settings.BasementFloorPenaltyRatio *
                                100m,


                            YearDifferenceCutoffYears =
                                settings.YearDifferenceCutoffYears,

                            UpdatedAt =
                                settings.UpdatedAt,

                            UpdatedBy =
                                settings.UpdatedBy
                        },

                    CoreMaterials =
                        coreMaterials
                };


            return View(
                viewModel
            );
        }


        // ==========================================
        // 更新估算與異常模型參數
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            UpdateEstimationSettings(
                [Bind(Prefix = "EstimationSettings")]
                EstimationSettingsFormViewModel form)
        {
            var request =
                new EstimationSettingsUpdateRequest
                {
                    TopK =
                        form.TopK,

                    MinimumSampleCount =
                        form.MinimumSampleCount,


                    LowerQuantile =
                        form.LowerQuantilePercent /
                        100m,

                    MedianQuantile =
                        form.MedianQuantilePercent /
                        100m,

                    UpperQuantile =
                        form.UpperQuantilePercent /
                        100m,


                    SimilarityExponent =
                        form.SimilarityExponent,

                    TimeHalfLifeYears =
                        form.TimeHalfLifeYears,

                    MinimumSimilarityScore =
                        form.MinimumSimilarityScore,


                    StructureWeight =
                        form.StructureWeight,

                    AreaWeight =
                        form.AreaWeight,

                    FloorWeight =
                        form.FloorWeight,

                    YearWeight =
                        form.YearWeight,

                    CityWeight =
                        form.CityWeight,


                    AreaDifferenceCutoffRatio =
                        form.AreaDifferenceCutoffPercent /
                        100m,

                    GroundFloorPenaltyRatio =
                        form.GroundFloorPenaltyPercent /
                        100m,

                    BasementFloorPenaltyRatio =
                        form.BasementFloorPenaltyPercent /
                        100m,


                    YearDifferenceCutoffYears =
                        form.YearDifferenceCutoffYears
                };


            var result =
                await _estimationSettingsService
                    .UpdateAsync(
                        request,
                        User.Identity?.Name
                            ?? "System"
                    );


            if (!result.Success)
            {
                TempData["ErrorMessage"] =
                    result.ErrorMessage;

                return RedirectToAction(
                    nameof(Index)
                );
            }


            TempData["SuccessMessage"] =
                "估算與異常分析模型參數已更新。";


            return RedirectToAction(
                nameof(Index)
            );
        }


        // ==========================================
        // 更新核心材料狀態
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            UpdateCoreMaterial(
                [FromBody]
                CoreMaterialUpdateRequest request)
        {
            var result =
                await _coreMaterialService
                    .UpdateCoreStatusAsync(
                        request,
                        User.Identity?.Name
                            ?? "System"
                    );


            if (result.Success)
            {
                return Ok(
                    result.Data
                );
            }


            return result.ErrorType switch
            {
                ServiceErrorType.NotFound =>
                    NotFound(new
                    {
                        message =
                            result.ErrorMessage
                    }),

                ServiceErrorType.Conflict =>
                    Conflict(new
                    {
                        message =
                            result.ErrorMessage
                    }),

                _ =>
                    BadRequest(new
                    {
                        message =
                            result.ErrorMessage
                    })
            };
        }
    }
}