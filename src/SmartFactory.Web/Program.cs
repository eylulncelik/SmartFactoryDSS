using Microsoft.EntityFrameworkCore;
using SmartFactory.Web.Data;
using SmartFactory.Web.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddOptions<RiskThresholdOptions>()
    .Bind(builder.Configuration.GetSection(RiskThresholdOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.LowMaximum < options.MediumMaximum
            && options.MediumMaximum < options.HighMaximum,
        "Risk thresholds must be in ascending order.")
    .ValidateOnStart();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
