using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Web.Models;

public enum MaintenanceStatus
{
    [Display(Name = "Planlandı")]
    Scheduled = 0,

    [Display(Name = "Devam Ediyor")]
    InProgress = 1,

    [Display(Name = "Tamamlandı")]
    Completed = 2,

    [Display(Name = "İptal Edildi")]
    Cancelled = 3
}
