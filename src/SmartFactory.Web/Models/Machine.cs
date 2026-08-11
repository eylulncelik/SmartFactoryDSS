using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Web.Models;

public sealed class Machine
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Makine kodu zorunludur.")]
    [StringLength(30, ErrorMessage = "Makine kodu en fazla 30 karakter olabilir.")]
    [Display(Name = "Makine Kodu")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Makine adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Makine adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Makine Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Makine kategorisi zorunludur.")]
    [StringLength(50, ErrorMessage = "Makine kategorisi en fazla 50 karakter olabilir.")]
    [Display(Name = "Makine Kategorisi")]
    public string MachineCategory { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Üretici en fazla 100 karakter olabilir.")]
    [Display(Name = "Üretici")]
    public string? Manufacturer { get; set; }

    [StringLength(100, ErrorMessage = "Model en fazla 100 karakter olabilir.")]
    [Display(Name = "Model")]
    public string? Model { get; set; }

    [Display(Name = "Durum")]
    public MachineStatus Status { get; set; } = MachineStatus.Active;

    [DataType(DataType.Date)]
    [Display(Name = "Kurulum Tarihi")]
    public DateTime InstallationDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
