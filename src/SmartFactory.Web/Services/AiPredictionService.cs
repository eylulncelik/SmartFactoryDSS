using System.Net.Http.Json;
using SmartFactory.Web.Services.Dtos;

namespace SmartFactory.Web.Services;

public sealed class AiPredictionService(HttpClient httpClient, ILogger<AiPredictionService> logger)
    : IAiPredictionService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AiPredictionService> _logger = logger;

    public async Task<PredictionResponseDto> PredictAsync(PredictionRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/predict", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("FastAPI returned error status {StatusCode}: {Error}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Yapay zeka servisinden hata döndü: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResponseDto>(cancellationToken: cancellationToken);
            return result ?? throw new InvalidOperationException("Yapay zeka servisinden boş yanıt alındı.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to FastAPI AI prediction service.");
            throw new HttpRequestException("Yapay zeka tahmin servisine (FastAPI) ulaşılamadı. Lütfen servisin çalıştığından emin olun.", ex);
        }
    }

    public async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var response = await _httpClient.GetAsync("/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
