using System.Net;
using System.Net.Http.Json;
using 崴正.Models;

namespace 崴正.Services
{
    public class OneDIntegrationService
    {
        private readonly HttpClient _httpClient;

        public OneDIntegrationService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public bool IsConfigured =>
            _httpClient.BaseAddress != null;


        public async Task<OneDProjectReadResult>
            GetProjectResultAsync(
                int projectId,
                CancellationToken cancellationToken = default)
        {
            if (projectId <= 0)
            {
                return new OneDProjectReadResult
                {
                    Success = false,
                    StatusCode =
                        StatusCodes.Status400BadRequest,
                    Message =
                        "ProjectId 必須大於 0。"
                };
            }

            if (!IsConfigured)
            {
                return new OneDProjectReadResult
                {
                    Success = false,
                    StatusCode =
                        StatusCodes.Status503ServiceUnavailable,
                    Message =
                        "尚未設定 1D API 位址。請設定 OneD:BaseUrl 或 ONE_D_API_BASE_URL。"
                };
            }

            try
            {
                using var response =
                    await _httpClient.GetAsync(
                        $"api/BidConversion/result/{projectId}",
                        cancellationToken
                    );

                if (response.StatusCode ==
                    HttpStatusCode.NotFound)
                {
                    return new OneDProjectReadResult
                    {
                        Success = false,
                        StatusCode =
                            StatusCodes.Status404NotFound,
                        Message =
                            $"1D 找不到 ProjectId={projectId} 的分類結果。"
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new OneDProjectReadResult
                    {
                        Success = false,
                        StatusCode =
                            StatusCodes.Status502BadGateway,
                        Message =
                            $"讀取 1D API 失敗，HTTP {(int)response.StatusCode}。"
                    };
                }

                var payload =
                    await response.Content
                        .ReadFromJsonAsync<
                            OneDApiResponse<
                                OneDSavedResultData>>(
                            cancellationToken:
                                cancellationToken
                        );

                if (payload == null)
                {
                    return new OneDProjectReadResult
                    {
                        Success = false,
                        StatusCode =
                            StatusCodes.Status502BadGateway,
                        Message =
                            "1D API 回傳內容無法解析。"
                    };
                }

                if (!payload.Success ||
                    payload.Data == null)
                {
                    return new OneDProjectReadResult
                    {
                        Success = false,
                        StatusCode =
                            StatusCodes.Status502BadGateway,
                        Message =
                            payload.Message ??
                            "1D API 未回傳可用的建案分類結果。"
                    };
                }

                return new OneDProjectReadResult
                {
                    Success = true,
                    StatusCode =
                        StatusCodes.Status200OK,
                    Message =
                        payload.Message ??
                        "已取得 1D 建案分類結果。",
                    Data =
                        payload.Data
                };
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return new OneDProjectReadResult
                {
                    Success = false,
                    StatusCode =
                        StatusCodes.Status504GatewayTimeout,
                    Message =
                        "讀取 1D API 逾時。"
                };
            }
            catch (HttpRequestException ex)
            {
                return new OneDProjectReadResult
                {
                    Success = false,
                    StatusCode =
                        StatusCodes.Status502BadGateway,
                    Message =
                        $"無法連線到 1D API：{ex.Message}"
                };
            }
        }
    }
}
