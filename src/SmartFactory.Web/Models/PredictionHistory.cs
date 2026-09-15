using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Web.Models;

public sealed class PredictionHistory
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Makine seçimi zorunludur.")]
    [Display(Name = "Makine")]
    public int MachineId { get; set; }

    public Machine? Machine { get; set; }

    [Display(Name = "Hava Sıcaklığı (°C)")]
    [Range(-50, 100, ErrorMessage = "Hava sıcaklığı -50°C ile 100°C arasında olmalıdır.")]
    public double AirTemperature_C { get; set; }

    [Display(Name = "Proses Sıcaklığı (°C)")]
    [Range(-50, 200, ErrorMessage = "Proses sıcaklığı -50°C ile 200°C arasında olmalıdır.")]
    public double ProcessTemperature_C { get; set; }

    [Display(Name = "Dönme Hızı (RPM)")]
    [Range(0, 15000, ErrorMessage = "Dönme hızı 0 ile 15000 RPM arasında olmalıdır.")]
    public double RotationalSpeed_RPM { get; set; }

    [Display(Name = "Tork (Nm)")]
    [Range(0, 2000, ErrorMessage = "Tork 0 ile 2000 Nm arasında olmalıdır.")]
    public double Torque_Nm { get; set; }

    [Display(Name = "Takım Aşınması (Dk)")]
    [Range(0, 10000, ErrorMessage = "Takım aşınması 0 ile 10000 dakika arasında olmalıdır.")]
    public double ToolWear_Min { get; set; }

    [Display(Name = "Arıza Olasılığı")]
    [DisplayFormat(DataFormatString = "{0:P2}")]
    public double FailureProbability { get; set; }

    [Display(Name = "Arıza Öngörüsü")]
    public bool FailurePredicted { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Risk Seviyesi")]
    public string RiskLevel { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Karar Destek Önerisi")]
    public string Recommendation { get; set; } = string.Empty;

    [Display(Name = "Tahmin Zamanı")]
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
}
