using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Data;
using SmartFactory.Web.Models;

namespace SmartFactory.Web.Controllers;

public sealed class MachinesController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IActionResult> Index()
    {
        var machines = await _context.Machines
            .AsNoTracking()
            .OrderBy(machine => machine.Code)
            .ToListAsync();

        return View(machines);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var machine = await _context.Machines
            .Include(item => item.Maintenances.OrderByDescending(m => m.ScheduledDate))
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return machine is null ? NotFound() : View(machine);
    }

    public IActionResult Create()
    {
        return View(new Machine { InstallationDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Name,MachineCategory,Manufacturer,Model,Status,InstallationDate")] Machine machine)
    {
        NormalizeTextFields(machine);
        await ValidateMachineCodeAsync(machine.Code);

        if (!ModelState.IsValid)
        {
            return View(machine);
        }

        machine.CreatedAt = DateTime.UtcNow;
        _context.Add(machine);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Makine başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var machine = await _context.Machines.FindAsync(id);
        return machine is null ? NotFound() : View(machine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,MachineCategory,Manufacturer,Model,Status,InstallationDate")] Machine input)
    {
        if (id != input.Id)
        {
            return NotFound();
        }

        NormalizeTextFields(input);
        await ValidateMachineCodeAsync(input.Code, input.Id);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var machine = await _context.Machines.FindAsync(id);
        if (machine is null)
        {
            return NotFound();
        }

        machine.Code = input.Code;
        machine.Name = input.Name;
        machine.MachineCategory = input.MachineCategory;
        machine.Manufacturer = input.Manufacturer;
        machine.Model = input.Model;
        machine.Status = input.Status;
        machine.InstallationDate = input.InstallationDate;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Makine bilgileri güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var machine = await _context.Machines
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return machine is null ? NotFound() : View(machine);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var machine = await _context.Machines.FindAsync(id);
        if (machine is null)
        {
            return NotFound();
        }

        var hasMaintenances = await _context.Maintenances.AnyAsync(m => m.MachineId == id);
        if (hasMaintenances)
        {
            TempData["ErrorMessage"] = "Bu makineye ait bakım kayıtları bulunduğu için makine silinemez. Önce ilişkili bakım kayıtlarını silmelisiniz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Machines.Remove(machine);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Makine silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateMachineCodeAsync(string code, int? machineId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var codeExists = await _context.Machines
            .AnyAsync(machine => machine.Code == code && machine.Id != machineId);

        if (codeExists)
        {
            ModelState.AddModelError(nameof(Machine.Code), "Bu makine kodu zaten kullanılıyor.");
        }
    }

    private static void NormalizeTextFields(Machine machine)
    {
        machine.Code = machine.Code.Trim();
        machine.Name = machine.Name.Trim();
        machine.MachineCategory = machine.MachineCategory.Trim();
        machine.Manufacturer = machine.Manufacturer?.Trim();
        machine.Model = machine.Model?.Trim();
    }
}
