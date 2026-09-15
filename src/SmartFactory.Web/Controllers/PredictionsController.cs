using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Data;
using SmartFactory.Web.Models;
using SmartFactory.Web.Services;
using SmartFactory.Web.Services.Dtos;

namespace SmartFactory.Web.Controllers;

public sealed class PredictionsController(ApplicationDbContext context, IAiPredictionService aiService) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly IAiPredictionService _aiService = aiService;

    public async Task<IActionResult> Index(int? machineId, string? riskLevel)
    {
        var query = _context.PredictionHistories
            .Include(p => p.Machine)
            .AsNoTracking()
            .AsQueryable();

        if (machineId.HasValue)
        {
            query = query.Where(p => p.MachineId == machineId.Value);
        }

        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            query = query.Where(p => p.RiskLevel == riskLevel);
        }

        var predictions = await query
            .OrderByDescending(p => p.PredictedAt)
            .Take(100)
            .ToListAsync();

        await PopulateFilterDropdownsAsync(machineId, riskLevel);

        ViewBag.IsAiServiceOnline = await _aiService.IsServiceAvailableAsync();

        return View(predictions);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var prediction = await _context.PredictionHistories
            .Include(p => p.Machine)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return prediction is null ? NotFound() : View(prediction);
    }

    public async Task<IActionResult> Create(int? machineId)
    {
        var model = new PredictionHistory
        {
            MachineId = machineId ?? 0,
            AirTemperature_C = 22.0,
            ProcessTemperature_C = 35.0,
            RotationalSpeed_RPM = 1500.0,
            Torque_Nm = 40.0,
            ToolWear_Min = 30.0
        };

        await PopulateMachinesDropDownListAsync(machineId);
        ViewBag.IsAiServiceOnline = await _aiService.IsServiceAvailableAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("MachineId,AirTemperature_C,ProcessTemperature_C,RotationalSpeed_RPM,Torque_Nm,ToolWear_Min")] PredictionHistory input)
    {
        var machine = await _context.Machines.FindAsync(input.MachineId);
        if (machine is null)
        {
            ModelState.AddModelError(nameof(PredictionHistory.MachineId), "Seçilen makine sistemde bulunamadı.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateMachinesDropDownListAsync(input.MachineId);
            ViewBag.IsAiServiceOnline = await _aiService.IsServiceAvailableAsync();
            return View(input);
        }

        try
        {
            var requestDto = new PredictionRequestDto
            {
                MachineId = machine!.Code,
                MachineCategory = machine.MachineCategory,
                AirTemperature_C = input.AirTemperature_C,
                ProcessTemperature_C = input.ProcessTemperature_C,
                RotationalSpeed_RPM = input.RotationalSpeed_RPM,
                Torque_Nm = input.Torque_Nm,
                ToolWear_Min = input.ToolWear_Min
            };

            var aiResponse = await _aiService.PredictAsync(requestDto);

            var historyRecord = new PredictionHistory
            {
                MachineId = machine.Id,
                AirTemperature_C = input.AirTemperature_C,
                ProcessTemperature_C = input.ProcessTemperature_C,
                RotationalSpeed_RPM = input.RotationalSpeed_RPM,
                Torque_Nm = input.Torque_Nm,
                ToolWear_Min = input.ToolWear_Min,
                FailureProbability = aiResponse.FailureProbability,
                FailurePredicted = aiResponse.FailurePredicted,
                RiskLevel = aiResponse.RiskLevel,
                Recommendation = aiResponse.Recommendation,
                PredictedAt = DateTime.UtcNow
            };

            _context.PredictionHistories.Add(historyRecord);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yapay zeka arıza riski analizi başarıyla tamamlandı.";
            return RedirectToAction(nameof(Details), new { id = historyRecord.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"AI Tahmin servisiyle bağlantı kurulamadı: {ex.Message}");
            await PopulateMachinesDropDownListAsync(input.MachineId);
            ViewBag.IsAiServiceOnline = false;
            return View(input);
        }
    }

    private async Task PopulateMachinesDropDownListAsync(object? selectedMachine = null)
    {
        var machines = await _context.Machines
            .AsNoTracking()
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, Display = $"{m.Code} - {m.Name} ({m.MachineCategory})" })
            .ToListAsync();

        ViewBag.MachineList = new SelectList(machines, "Id", "Display", selectedMachine);
    }

    private async Task PopulateFilterDropdownsAsync(int? selectedMachineId, string? selectedRiskLevel)
    {
        var machines = await _context.Machines
            .AsNoTracking()
            .OrderBy(m => m.Code)
            .Select(m => new { m.Id, Display = $"{m.Code} - {m.Name}" })
            .ToListAsync();

        var riskLevels = new List<SelectListItem>
        {
            new("Tüm Seviyeler", ""),
            new("Düşük Risk", "Düşük"),
            new("Orta Risk", "Orta"),
            new("Yüksek Risk", "Yüksek"),
            new("Kritik Risk", "Kritik")
        };

        ViewBag.FilterMachines = new SelectList(machines, "Id", "Display", selectedMachineId);
        ViewBag.FilterRiskLevels = new SelectList(riskLevels, "Value", "Text", selectedRiskLevel ?? "");
        ViewBag.SelectedMachineId = selectedMachineId;
        ViewBag.SelectedRiskLevel = selectedRiskLevel;
    }
}
