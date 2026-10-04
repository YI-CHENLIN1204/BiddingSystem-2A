using 崴正.Data;
using System.Data;
using 崴正.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;

System.Text.Encoding.RegisterProvider(
    System.Text.CodePagesEncodingProvider.Instance
);

var builder = WebApplication.CreateBuilder(args);
var legacyUiEnabled =
    builder.Configuration.GetValue<bool>("LegacyUi:Enabled");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize =
        100 * 1024 * 1024;
});



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
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins =
            builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
            ?? new[] { "http://localhost:5173" };

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.Configure<FormOptions>(options =>{options.MultipartBodyLengthLimit =100 * 1024 * 1024;});
builder.Services.AddScoped<ProjectSimilarityService>();
builder.Services.AddScoped<FinalBidService>();
builder.Services.AddScoped<MaterialMappingService>();
builder.Services.AddScoped<FinalBidImportService>();
builder.Services.AddScoped<FinalBidQueryService>();
builder.Services.AddScoped<UnitConversionService>();
builder.Services.AddScoped<EstimationSettingsService>();
builder.Services.AddScoped<RangeEstimationService>();
builder.Services.AddScoped<ProjectManagementService>();
builder.Services.AddScoped<EstimationTargetService>();
builder.Services.AddScoped<BatchEstimationService>();
builder.Services.AddScoped<CoreMaterialService>();
builder.Services.AddScoped<BidRangeAnomalyAnalysisService>();
builder.Services.AddScoped<MaterialCategoryService>();
builder.Services.AddScoped<StandardMaterialService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<UnitCategoryService>();
builder.Services.AddScoped<UnitMasterService>();


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
    if (legacyUiEnabled)
    {
        app.UseExceptionHandler("/Home/Error");
    }
    else
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "伺服器處理請求時發生錯誤。",
                    traceId = context.TraceIdentifier
                });
            });
        });
    }

    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("Frontend");


// ==========================================
// 資料密集型 Request 共用同一條 DB Connection
// ==========================================

app.Use(async (context, next) =>
{
    var path =
        context.Request.Path;

    bool useSharedDbConnection =
        path.StartsWithSegments("/QuantityRule") ||
        path.StartsWithSegments("/ProjectManagement") ||
        path.StartsWithSegments("/Home/EstimationUI") ||
        path.StartsWithSegments("/Home/UploadFinalBid") ||
        path.StartsWithSegments("/api/range-estimation") ||
        path.StartsWithSegments("/api/bid-anomaly") ||
        path.StartsWithSegments("/api/Estimation");

    if (!useSharedDbConnection)
    {
        await next();
        return;
    }


    var db =
        context.RequestServices
            .GetRequiredService<ApplicationDbContext>();


    var connection =
        db.Database
            .GetDbConnection();


    bool connectionWasOpen =
        connection.State ==
        ConnectionState.Open;


    if (!connectionWasOpen)
    {

        await db.Database
            .OpenConnectionAsync();
    }


    try
    {
        await next();
    }
    finally
    {
        if (!connectionWasOpen)
        {

            await db.Database
                .CloseConnectionAsync();

        }
    }
});


// 一定要在 UseAuthorization 前面
app.UseAuthorization();

app.MapControllers();

if (legacyUiEnabled)
{
    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "legacy-ui",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();
}

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

