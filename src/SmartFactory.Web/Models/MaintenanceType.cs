using System.ComponentModel.DataAnnotations;

namespace SmartFactory.Web.Models;

public enum MaintenanceType
{
    [Display(Name = "Planlı Bakım")]
    Planned = 0,

    [Display(Name = "Arıza / Duruş Onarımı")]
    Unplanned = 1,

    [Display(Name = "Kestirimci Bakım")]
    Predictive = 2,

    [Display(Name = "Periyodik Kontrol")]
    Periodic = 3
}
