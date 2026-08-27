using 崴正.Data;
using 崴正.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var dataProtectionKeysPath =
    builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(
        builder.Environment.ContentRootPath,
        ".data-protection-keys");

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("BiddingSystem2A");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ProjectSimilarityService>();
builder.Services.AddScoped<SimilarityEstimationService>();
builder.Services.AddScoped<FinalBidService>();
builder.Services.AddScoped<MaterialMappingService>();
builder.Services.AddScoped<FinalBidImportService>();
builder.Services.AddScoped<FinalBidQueryService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddSingleton<UnitConversionService>();
builder.Services.AddScoped<EstimationSettingsService>();
builder.Services.AddScoped<RangeEstimationService>();
builder.Services.AddScoped<ProjectManagementService>();
builder.Services.AddScoped<EstimationTargetService>();
builder.Services.AddScoped<BatchEstimationService>();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 加入 Cookie 登入驗證

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseRouting();

// 一定要在 UseAuthorization 前面
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

