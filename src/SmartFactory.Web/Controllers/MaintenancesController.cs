using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Data;
using SmartFactory.Web.Models;

namespace SmartFactory.Web.Controllers;

public sealed class MaintenancesController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IActionResult> Index(int? machineId, MaintenanceStatus? status, MaintenancePriority? priority)
    {
        var query = _context.Maintenances
            .Include(maintenance => maintenance.Machine)
            .AsNoTracking()
            .AsQueryable();

        if (machineId.HasValue)
        {
            query = query.Where(maintenance => maintenance.MachineId == machineId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(maintenance => maintenance.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(maintenance => maintenance.Priority == priority.Value);
        }

        var maintenances = await query
            .OrderByDescending(maintenance => maintenance.ScheduledDate)
            .ThenByDescending(maintenance => maintenance.CreatedAt)
            .ToListAsync();

        await PopulateFilterDropdownsAsync(machineId, status, priority);

        return View(maintenances);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var maintenance = await _context.Maintenances
            .Include(m => m.Machine)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        return maintenance is null ? NotFound() : View(maintenance);
    }

    public async Task<IActionResult> Create(int? machineId)
    {
        var model = new Maintenance
        {
            ScheduledDate = DateTime.Today,
            MachineId = machineId ?? 0
        };

        await PopulateMachinesDropDownListAsync(machineId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MachineId,Title,Description,Type,Priority,Status,ScheduledDate,CompletedDate,Cost,PerformedBy")] Maintenance maintenance)
    {
        NormalizeTextFields(maintenance);
        await ValidateMachineAsync(maintenance.MachineId);

        if (maintenance.Status == MaintenanceStatus.Completed && maintenance.CompletedDate is null)
        {
            maintenance.CompletedDate = DateTime.Today;
        }

        if (!ModelState.IsValid)
        {
            await PopulateMachinesDropDownListAsync(maintenance.MachineId);
            return View(maintenance);
        }

        maintenance.CreatedAt = DateTime.UtcNow;
        _context.Add(maintenance);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bakım kaydı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index), new { machineId = maintenance.MachineId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var maintenance = await _context.Maintenances.FindAsync(id);
        if (maintenance is null)
        {
            return NotFound();
        }

        await PopulateMachinesDropDownListAsync(maintenance.MachineId);
        return View(maintenance);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,MachineId,Title,Description,Type,Priority,Status,ScheduledDate,CompletedDate,Cost,PerformedBy")] Maintenance input)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        NormalizeTextFields(input);
        await ValidateMachineAsync(input.MachineId);

        if (input.Status == MaintenanceStatus.Completed && input.CompletedDate is null)
        {
            input.CompletedDate = DateTime.Today;
        }

        if (!ModelState.IsValid)
        {
            await PopulateMachinesDropDownListAsync(input.MachineId);
            return View(input);
        }

        var maintenance = await _context.Maintenances.FindAsync(id);
        if (maintenance is null)
        {
            return NotFound();
        }

        maintenance.MachineId = input.MachineId;
        maintenance.Title = input.Title;
        maintenance.Description = input.Description;
        maintenance.Type = input.Type;
        maintenance.Priority = input.Priority;
        maintenance.Status = input.Status;
        maintenance.ScheduledDate = input.ScheduledDate;
        maintenance.CompletedDate = input.CompletedDate;
        maintenance.Cost = input.Cost;
        maintenance.PerformedBy = input.PerformedBy;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Bakım kaydı güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var maintenance = await _context.Maintenances
            .Include(m => m.Machine)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        return maintenance is null ? NotFound() : View(maintenance);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var maintenance = await _context.Maintenances.FindAsync(id);
        if (maintenance is null)
        {
            return NotFound();
        }

        _context.Maintenances.Remove(maintenance);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bakım kaydı silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateMachinesDropDownListAsync(object? selectedMachine = null)
    {
        var machines = await _context.Machines
            .AsNoTracking()
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, Display = $"{m.Code} - {m.Name}" })
            .ToListAsync();

        ViewBag.MachineList = new SelectList(machines, "Id", "Display", selectedMachine);
    }

    private async Task PopulateFilterDropdownsAsync(int? selectedMachineId, MaintenanceStatus? selectedStatus, MaintenancePriority? selectedPriority)
    {
        var machines = await _context.Machines
            .AsNoTracking()
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, Display = $"{m.Code} - {m.Name}" })
            .ToListAsync();

        ViewBag.FilterMachines = new SelectList(machines, "Id", "Display", selectedMachineId);
        ViewBag.SelectedMachineId = selectedMachineId;
        ViewBag.SelectedStatus = selectedStatus;
        ViewBag.SelectedPriority = selectedPriority;
    }

    private async Task ValidateMachineAsync(int machineId)
    {
        var machineExists = await _context.Machines.AnyAsync(m => m.Id == machineId);
        if (!machineExists)
        {
            ModelState.AddModelError(nameof(Maintenance.MachineId), "Seçilen makine sistemde bulunamadı.");
        }
    }

    private static void NormalizeTextFields(Maintenance maintenance)
    {
        maintenance.Title = maintenance.Title.Trim();
        maintenance.Description = maintenance.Description?.Trim();
        maintenance.PerformedBy = maintenance.PerformedBy?.Trim();
    }
}
