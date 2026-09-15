using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Controllers;
using SmartFactory.Web.Models;
using Xunit;

namespace SmartFactory.Web.Tests;

public class MachineManagementTests
{
    private static MachinesController CreateControllerWithTempData(Data.ApplicationDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TempDataProviderMock());
        return new MachinesController(context)
        {
            TempData = tempData
        };
    }

    [Fact]
    public async Task Index_ReturnsViewWithAllMachines()
    {
        using var context = TestDbContextFactory.Create(nameof(Index_ReturnsViewWithAllMachines));
        context.Machines.AddRange(
            new Machine { Code = "M-01", Name = "Torna", MachineCategory = "HeavyDuty" },
            new Machine { Code = "M-02", Name = "Freze", MachineCategory = "LightDuty" }
        );
        await context.SaveChangesAsync();

        var controller = new MachinesController(context);
        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Machine>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Create_ValidMachine_AddsMachineAndRedirects()
    {
        using var context = TestDbContextFactory.Create(nameof(Create_ValidMachine_AddsMachineAndRedirects));
        var controller = CreateControllerWithTempData(context);

        var newMachine = new Machine
        {
            Code = "CNC-101",
            Name = "5 Eksen CNC",
            MachineCategory = "MediumDuty",
            InstallationDate = DateTime.Today
        };

        var result = await controller.Create(newMachine);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(MachinesController.Index), redirectResult.ActionName);

        var saved = await context.Machines.FirstOrDefaultAsync(m => m.Code == "CNC-101");
        Assert.NotNull(saved);
        Assert.Equal("5 Eksen CNC", saved.Name);
    }

    [Fact]
    public async Task Create_DuplicateCode_FailsValidation()
    {
        using var context = TestDbContextFactory.Create(nameof(Create_DuplicateCode_FailsValidation));
        context.Machines.Add(new Machine { Code = "CNC-101", Name = "İlk Makine", MachineCategory = "General" });
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);
        var duplicateMachine = new Machine { Code = "CNC-101", Name = "İkinci Makine", MachineCategory = "General" };

        var result = await controller.Create(duplicateMachine);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Machine.Code)));
    }

    [Fact]
    public async Task DeleteConfirmed_MachineWithMaintenances_PreventsDeletion()
    {
        using var context = TestDbContextFactory.Create(nameof(DeleteConfirmed_MachineWithMaintenances_PreventsDeletion));
        var machine = new Machine { Code = "M-99", Name = "Pres Makinesi", MachineCategory = "Heavy" };
        context.Machines.Add(machine);
        await context.SaveChangesAsync();

        var maintenance = new Maintenance
        {
            MachineId = machine.Id,
            Title = "Hidrolik Filtre Değişimi",
            ScheduledDate = DateTime.Today
        };
        context.Maintenances.Add(maintenance);
        await context.SaveChangesAsync();

        var controller = CreateControllerWithTempData(context);
        var result = await controller.DeleteConfirmed(machine.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(MachinesController.Details), redirectResult.ActionName);
        Assert.Equal("Bu makineye ait bakım kayıtları bulunduğu için makine silinemez. Önce ilişkili bakım kayıtlarını silmelisiniz.", controller.TempData["ErrorMessage"]);

        var stillExists = await context.Machines.AnyAsync(m => m.Id == machine.Id);
        Assert.True(stillExists);
    }

    private sealed class TempDataProviderMock : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
