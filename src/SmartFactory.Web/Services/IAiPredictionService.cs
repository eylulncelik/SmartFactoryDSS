using SmartFactory.Web.Services.Dtos;

namespace SmartFactory.Web.Services;

public interface IAiPredictionService
{
    Task<PredictionResponseDto> PredictAsync(PredictionRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
}
