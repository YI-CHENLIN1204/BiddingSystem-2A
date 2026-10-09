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
builder.Services.AddScoped<TwoAPreprocessService>();
builder.Services.AddScoped<DetectionCategoryService>();
builder.Services.AddScoped<DetectionCategoryRangeService>();
builder.Services.AddScoped<TwoAQuantityAnalysisService>();

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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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


// ==========================================
// 資料密集型 Request 共用同一條 DB Connection
// ==========================================

app.Use(async (context, next) =>
{
    var path =
        context.Request.Path;

    bool useSharedDbConnection =
        path.StartsWithSegments("/api/range-estimation") ||
        path.StartsWithSegments("/api/bid-anomaly") ||
        path.StartsWithSegments("/api/Estimation") ||
        path.StartsWithSegments("/api/detection-category");

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


app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
