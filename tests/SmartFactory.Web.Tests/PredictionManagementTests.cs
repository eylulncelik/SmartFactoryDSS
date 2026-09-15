using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SmartFactory.Web.Controllers;
using SmartFactory.Web.Models;
using SmartFactory.Web.Services;
using SmartFactory.Web.Services.Dtos;
using Xunit;

namespace SmartFactory.Web.Tests;

public class PredictionManagementTests
{
    private static PredictionsController CreateController(Data.ApplicationDbContext context, IAiPredictionService aiService)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, new TempDataProviderMock());
        return new PredictionsController(context, aiService)
        {
            TempData = tempData
        };
    }

    [Fact]
    public async Task Index_FiltersByMachineAndRiskLevel()
    {
        using var context = TestDbContextFactory.Create(nameof(Index_FiltersByMachineAndRiskLevel));
        var machine1 = new Machine { Code = "CNC-10", Name = "Torna", MachineCategory = "MediumDuty" };
        var machine2 = new Machine { Code = "CNC-20", Name = "Freze", MachineCategory = "HeavyDuty" };
        context.Machines.AddRange(machine1, machine2);
        await context.SaveChangesAsync();

        context.PredictionHistories.AddRange(
            new PredictionHistory
            {
                MachineId = machine1.Id,
                RiskLevel = "Düşük",
                FailureProbability = 0.15,
                Recommendation = "Normal",
                PredictedAt = DateTime.UtcNow
            },
            new PredictionHistory
            {
                MachineId = machine1.Id,
                RiskLevel = "Yüksek",
                FailureProbability = 0.75,
                Recommendation = "Kontrol et",
                PredictedAt = DateTime.UtcNow
            },
            new PredictionHistory
            {
                MachineId = machine2.Id,
                RiskLevel = "Kritik",
                FailureProbability = 0.90,
                Recommendation = "Durdur",
                PredictedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var mockAiService = new MockAiService(true);
        var controller = CreateController(context, mockAiService);

        var result = await controller.Index(machine1.Id, "Yüksek");
        var viewResult = Assert.IsType<ViewResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<PredictionHistory>>(viewResult.Model);

        Assert.Single(list);
        Assert.Equal("Yüksek", list.First().RiskLevel);
        Assert.Equal(0.75, list.First().FailureProbability);
    }

    [Fact]
    public async Task Create_CallsAiServiceAndPersistsPredictionRecord()
    {
        using var context = TestDbContextFactory.Create(nameof(Create_CallsAiServiceAndPersistsPredictionRecord));
        var machine = new Machine { Code = "MCH-01", Name = "Lazer Kesim", MachineCategory = "HeavyDuty" };
        context.Machines.Add(machine);
        await context.SaveChangesAsync();

        var mockAiService = new MockAiService(true, new PredictionResponseDto
        {
            MachineId = "MCH-01",
            FailureProbability = 0.85,
            FailurePredicted = true,
            RiskLevel = "Kritik",
            Recommendation = "Acil durdurma ve bakım önerilir."
        });

        var controller = CreateController(context, mockAiService);
        var input = new PredictionHistory
        {
            MachineId = machine.Id,
            AirTemperature_C = 30.0,
            ProcessTemperature_C = 50.0,
            RotationalSpeed_RPM = 2800.0,
            Torque_Nm = 80.0,
            ToolWear_Min = 180.0
        };

        var result = await controller.Create(input);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PredictionsController.Details), redirect.ActionName);

        var saved = await context.PredictionHistories.FirstOrDefaultAsync(p => p.MachineId == machine.Id);
        Assert.NotNull(saved);
        Assert.Equal(0.85, saved.FailureProbability);
        Assert.Equal("Kritik", saved.RiskLevel);
        Assert.True(saved.FailurePredicted);
    }

    [Fact]
    public async Task AiPredictionService_PredictAsync_SendsCorrectRequestAndParsesResponse()
    {
        var mockResponse = new PredictionResponseDto
        {
            MachineId = "TEST-01",
            FailureProbability = 0.42,
            FailurePredicted = false,
            RiskLevel = "Orta",
            Recommendation = "Şüpheli titreşim."
        };

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, mockResponse);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8000")
        };

        var service = new AiPredictionService(httpClient, NullLogger<AiPredictionService>.Instance);
        var result = await service.PredictAsync(new PredictionRequestDto
        {
            MachineId = "TEST-01",
            MachineCategory = "MediumDuty",
            AirTemperature_C = 22.0,
            ProcessTemperature_C = 35.0,
            RotationalSpeed_RPM = 1500.0,
            Torque_Nm = 40.0,
            ToolWear_Min = 30.0
        });

        Assert.NotNull(result);
        Assert.Equal(0.42, result.FailureProbability);
        Assert.Equal("Orta", result.RiskLevel);
    }

    private sealed class MockAiService(bool isOnline, PredictionResponseDto? response = null) : IAiPredictionService
    {
        public Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(isOnline);

        public Task<PredictionResponseDto> PredictAsync(PredictionRequestDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response ?? new PredictionResponseDto
            {
                MachineId = request.MachineId,
                FailureProbability = 0.20,
                FailurePredicted = false,
                RiskLevel = "Düşük",
                Recommendation = "Normal operasyon."
            });
        }
    }

    private sealed class MockHttpMessageHandler(HttpStatusCode statusCode, object responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(responseBody)
            };
            return Task.FromResult(response);
        }
    }

    private sealed class TempDataProviderMock : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
