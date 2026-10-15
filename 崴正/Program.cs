using 崴正.Data;
using System.Data;
using 崴正.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;

System.Text.Encoding.RegisterProvider(
    System.Text.CodePagesEncodingProvider.Instance
);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize =
        100 * 1024 * 1024;
});

builder.Services.AddControllers();
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
builder.Services.AddScoped<ProjectManagementService>();
builder.Services.AddScoped<MaterialCategoryService>();
builder.Services.AddScoped<StandardMaterialService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<UnitCategoryService>();
builder.Services.AddScoped<UnitMasterService>();
builder.Services.AddScoped<TwoAPreprocessService>();
builder.Services.AddScoped<DetectionCategoryService>();
builder.Services.AddScoped<DetectionCategoryRangeService>();
builder.Services.AddScoped<TwoAQuantityAnalysisService>();
builder.Services.AddScoped<MajorCategoryPriceRangeService>();
builder.Services.AddScoped<AnalysisCaseService>();

builder.Services.AddHttpClient<OneDIntegrationService>(
    (serviceProvider, client) =>
    {
        var configuration =
            serviceProvider
                .GetRequiredService<IConfiguration>();

        var baseUrl =
            configuration["OneD:BaseUrl"];

        if (Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var baseUri))
        {
            client.BaseAddress =
                baseUri;
        }

        client.Timeout =
            TimeSpan.FromSeconds(20);
    }
);


var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "請使用 ConnectionStrings__DefaultConnection 環境變數設定資料庫連線字串。"
    );
}

// 暫時性診斷設定：避免重用 Supabase Pooler 已關閉的連線。
// 驗證連線穩定後，再依實測恢復連線池與調校效能。
var diagnosticConnectionString = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    Pooling = false
}.ConnectionString;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(diagnosticConnectionString, npgsql =>
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 2,
            maxRetryDelay: TimeSpan.FromSeconds(3),
            errorCodesToAdd: null)));

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

    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("Frontend");


// 資料庫連線由 EF Core / Npgsql 依照查詢與交易生命週期管理。
// 不在每個 HTTP request 手動保持長連線，避免 pooler 中斷造成後續查詢失敗。

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
