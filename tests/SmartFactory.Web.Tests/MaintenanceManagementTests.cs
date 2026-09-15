using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Controllers;
using SmartFactory.Web.Models;
using Xunit;

namespace SmartFactory.Web.Tests;

public class MaintenanceManagementTests
{
    private static MaintenancesController CreateControllerWithTempData(Data.ApplicationDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TempDataProviderMock());
        return new MaintenancesController(context)
        {
            TempData = tempData
        };
    }

    [Fact]
    public async Task Create_ValidMaintenance_SavesAndRedirects()
    {
        using var context = TestDbContextFactory.Create(nameof(Create_ValidMaintenance_SavesAndRedirects));
        var machine = new Machine { Code = "CNC-01", Name = "CNC Freze", MachineCategory = "HeavyDuty" };
        context.Machines.Add(machine);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);
        var maintenance = new Maintenance
        {
            MachineId = machine.Id,
            Title = "Aylık Yağ Değişimi",
            Type = MaintenanceType.Periodic,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Scheduled,
            ScheduledDate = DateTime.Today,
            Cost = 1500m,
            PerformedBy = "Bakım Servisi"
        };

        var result = await controller.Create(maintenance);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(MaintenancesController.Index), redirectResult.ActionName);

        var saved = await context.Maintenances.FirstOrDefaultAsync(m => m.Title == "Aylık Yağ Değişimi");
        Assert.NotNull(saved);
        Assert.Equal(1500m, saved.Cost);
        Assert.Equal("Bakım kaydı başarıyla oluşturuldu.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Create_CompletedStatusWithoutCompletedDate_SetsCompletedDateToToday()
    {
        using var context = TestDbContextFactory.Create(nameof(Create_CompletedStatusWithoutCompletedDate_SetsCompletedDateToToday));
        var machine = new Machine { Code = "CNC-02", Name = "Torna", MachineCategory = "HeavyDuty" };
        context.Machines.Add(machine);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);
        var maintenance = new Maintenance
        {
            MachineId = machine.Id,
            Title = "Rulman Yenileme",
            Status = MaintenanceStatus.Completed,
            CompletedDate = null
        };

        var result = await controller.Create(maintenance);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        var saved = await context.Maintenances.FirstOrDefaultAsync(m => m.Title == "Rulman Yenileme");
        Assert.NotNull(saved);
        Assert.NotNull(saved.CompletedDate);
        Assert.Equal(DateTime.Today, saved.CompletedDate.Value.Date);
    }

    [Fact]
    public async Task Index_FiltersByMachineAndStatus()
    {
        using var context = TestDbContextFactory.Create(nameof(Index_FiltersByMachineAndStatus));
        var m1 = new Machine { Code = "M-1", Name = "Makine 1", MachineCategory = "Cat1" };
        var m2 = new Machine { Code = "M-2", Name = "Makine 2", MachineCategory = "Cat2" };
        context.Machines.AddRange(m1, m2);
        await context.SaveChangesAsync();

        context.Maintenances.AddRange(
            new Maintenance { MachineId = m1.Id, Title = "B-1", Status = MaintenanceStatus.Scheduled },
            new Maintenance { MachineId = m1.Id, Title = "B-2", Status = MaintenanceStatus.Completed },
            new Maintenance { MachineId = m2.Id, Title = "B-3", Status = MaintenanceStatus.Scheduled }
        );
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);

        // Filter by m1 and Scheduled
        var result = await controller.Index(m1.Id, MaintenanceStatus.Scheduled, null);
        var viewResult = Assert.IsType<ViewResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<Maintenance>>(viewResult.Model);

        Assert.Single(list);
        Assert.Equal("B-1", list.First().Title);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesMaintenance()
    {
        using var context = TestDbContextFactory.Create(nameof(DeleteConfirmed_RemovesMaintenance));
        var machine = new Machine { Code = "M-1", Name = "Makine 1", MachineCategory = "Cat1" };
        context.Machines.Add(machine);
        await context.SaveChangesAsync();

        var maintenance = new Maintenance { MachineId = machine.Id, Title = "Silinecek Bakım" };
        context.Maintenances.Add(maintenance);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);
        var result = await controller.DeleteConfirmed(maintenance.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(MaintenancesController.Index), redirectResult.ActionName);

        var exists = await context.Maintenances.AnyAsync(m => m.Id == maintenance.Id);
        Assert.False(exists);
    }

    private sealed class TempDataProviderMock : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
