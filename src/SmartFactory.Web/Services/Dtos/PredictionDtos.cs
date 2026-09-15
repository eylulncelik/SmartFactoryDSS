using System.Text.Json.Serialization;

namespace SmartFactory.Web.Services.Dtos;

public sealed class PredictionRequestDto
{
    [JsonPropertyName("machine_id")]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("machine_category")]
    public string MachineCategory { get; set; } = string.Empty;

    [JsonPropertyName("air_temperature_c")]
    public double AirTemperature_C { get; set; }

    [JsonPropertyName("process_temperature_c")]
    public double ProcessTemperature_C { get; set; }

    [JsonPropertyName("rotational_speed_rpm")]
    public double RotationalSpeed_RPM { get; set; }

    [JsonPropertyName("torque_nm")]
    public double Torque_Nm { get; set; }

    [JsonPropertyName("tool_wear_min")]
    public double ToolWear_Min { get; set; }
}

public sealed class PredictionResponseDto
{
    [JsonPropertyName("machine_id")]
    public string MachineId { get; set; } = string.Empty;

    [JsonPropertyName("failure_probability")]
    public double FailureProbability { get; set; }

    [JsonPropertyName("failure_predicted")]
    public bool FailurePredicted { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("evaluated_at")]
    public string EvaluatedAt { get; set; } = string.Empty;
}
