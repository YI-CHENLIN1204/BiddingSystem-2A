using System.Net.Http.Json;
using System.Text.Json;
using 崴正.Models;

namespace 崴正.Services
{
    /// <summary>
    /// 1D → 2A 唯讀介接。
    ///
    /// 目前只讀取 1D 已完成且已儲存的 BidConversionResult，
    /// 不會要求 1D 重新分類，也不會修改 1D 資料。
    /// </summary>
    public class OneDIntegrationService
    {
        private readonly HttpClient
            _httpClient;

        private readonly IConfiguration
            _configuration;

        private readonly DetectionCategoryService
            _detectionCategoryService;

        private static readonly JsonSerializerOptions
            JsonOptions =
                new()
                {
                    PropertyNameCaseInsensitive =
                        true
                };

        public OneDIntegrationService(
            HttpClient httpClient,
            IConfiguration configuration,
            DetectionCategoryService detectionCategoryService)
        {
            _httpClient =
                httpClient;

            _configuration =
                configuration;

            _detectionCategoryService =
                detectionCategoryService;
        }

        public bool IsConfigured =>
            GetBaseUri() != null;

        public object GetStatus()
        {
            return new
            {
                configured =
                    IsConfigured,

                mode =
                    "read-only",

                sourceEndpoint =
                    "api/BidConversion/result/{projectId}",

                message =
                    IsConfigured
                        ? "1D API 已設定，可讀取已儲存的工程大項分類結果。"
                        : "尚未設定 1D API。請設定 OneD__BaseUrl 或 Docker 的 ONE_D_API_BASE_URL。"
            };
        }

        public async Task<ServiceResult>
            GetSavedResultAsync(
                int projectId,
                CancellationToken cancellationToken =
                    default)
        {
            if (projectId <= 0)
            {
                return ServiceResult.Validation(
                    "1D projectId 必須大於 0"
                );
            }

            var baseUri =
                GetBaseUri();

            if (baseUri == null)
            {
                return ServiceResult.Conflict(
                    "尚未設定 1D API 位址。請設定 OneD__BaseUrl 或 ONE_D_API_BASE_URL。"
                );
            }

            var requestUri =
                new Uri(
                    baseUri,
                    $"api/BidConversion/result/{projectId}"
                );

            try
            {
                using var response =
                    await _httpClient.GetAsync(
                        requestUri,
                        cancellationToken
                    );

                if (
                    response.StatusCode ==
                    System.Net.HttpStatusCode.NotFound
                )
                {
                    return ServiceResult.NotFound(
                        $"1D 找不到 projectId={projectId} 的分類結果"
                    );
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ServiceResult.Conflict(
                        $"1D API 回傳 HTTP {(int)response.StatusCode}，目前無法取得分類結果。"
                    );
                }

                var payload =
                    await response.Content
                        .ReadFromJsonAsync<
                            OneDApiResponse<OneDSavedResultData>
                        >(
                            JsonOptions,
                            cancellationToken
                        );

                if (payload == null)
                {
                    return ServiceResult.Conflict(
                        "1D API 回傳空內容"
                    );
                }

                if (!payload.Success)
                {
                    return ServiceResult.Conflict(
                        string.IsNullOrWhiteSpace(
                            payload.Message
                        )
                            ? "1D API 回報失敗"
                            : payload.Message
                    );
                }

                var data =
                    payload.Data
                    ?? new OneDSavedResultData
                    {
                        ProjectId =
                            projectId
                    };

                data.Items ??=
                    new List<OneDSavedResultItem>();

                data.Count =
                    data.Items.Count;

                return ServiceResult.Ok(
                    data
                );
            }
            catch (
                HttpRequestException ex
            )
            {
                return ServiceResult.Conflict(
                    $"無法連線至 1D API：{ex.Message}"
                );
            }
            catch (
                TaskCanceledException
            )
            {
                return ServiceResult.Conflict(
                    "連線 1D API 逾時"
                );
            }
            catch (
                JsonException ex
            )
            {
                return ServiceResult.Conflict(
                    $"1D API 回傳格式無法解析：{ex.Message}"
                );
            }
        }

        public async Task<ServiceResult>
            GetDetectionPreviewAsync(
                int projectId,
                CancellationToken cancellationToken =
                    default)
        {
            var sourceResult =
                await GetSavedResultAsync(
                    projectId,
                    cancellationToken
                );

            if (!sourceResult.Success)
            {
                return sourceResult;
            }

            if (
                sourceResult.Data
                is not OneDSavedResultData data
            )
            {
                return ServiceResult.Conflict(
                    "1D 資料格式不符合預期"
                );
            }

            var preview =
                _detectionCategoryService
                    .BuildPreview(
                        data.Items
                    );

            return ServiceResult.Ok(
                new
                {
                    oneDProjectId =
                        projectId,

                    sourceItemCount =
                        data.Items.Count,

                    preview.TotalItems,

                    preview.ClassifiedItems,

                    preview.PendingItems,

                    preview.Groups,

                    preview.Items
                }
            );
        }

        private Uri? GetBaseUri()
        {
            string? value =
                _configuration[
                    "OneD:BaseUrl"
                ]
                ?.Trim();

            if (
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Uri.TryCreate(
                    value,
                    UriKind.Absolute,
                    out var uri
                )
            )
            {
                return null;
            }

            var normalized =
                uri.AbsoluteUri
                    .EndsWith("/")
                    ? uri.AbsoluteUri
                    : uri.AbsoluteUri + "/";

            return new Uri(
                normalized,
                UriKind.Absolute
            );
        }
    }
}
