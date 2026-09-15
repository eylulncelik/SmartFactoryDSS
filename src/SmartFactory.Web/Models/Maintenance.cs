using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Web.Models;

public sealed class Maintenance
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Makine seçimi zorunludur.")]
    [Display(Name = "Makine")]
    public int MachineId { get; set; }

    public Machine? Machine { get; set; }

    [Required(ErrorMessage = "Bakım başlığı zorunludur.")]
    [StringLength(120, ErrorMessage = "Bakım başlığı en fazla 120 karakter olabilir.")]
    [Display(Name = "Bakım Başlığı")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Bakım Türü")]
    public MaintenanceType Type { get; set; } = MaintenanceType.Planned;

    [Display(Name = "Öncelik")]
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    [Display(Name = "Durum")]
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    [DataType(DataType.Date)]
    [Display(Name = "Planlanan Tarih")]
    public DateTime ScheduledDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Tamamlanma Tarihi")]
    public DateTime? CompletedDate { get; set; }

    [DataType(DataType.Currency)]
    [Range(0, 10000000, ErrorMessage = "Maliyet 0 ile 10.000.000 arasında olmalıdır.")]
    [Display(Name = "Maliyet (₺)")]
    public decimal? Cost { get; set; }

    [StringLength(100, ErrorMessage = "Teknisyen bilgisi en fazla 100 karakter olabilir.")]
    [Display(Name = "Teknisyen / Ekip")]
    public string? PerformedBy { get; set; }

    [Display(Name = "Kayıt Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
