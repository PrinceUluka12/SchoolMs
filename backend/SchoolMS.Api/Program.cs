using Azure.Identity;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SchoolMS.Api.Converters;
using SchoolMS.Api.HealthChecks;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using SchoolMS.Infrastructure.Interceptors;
using SchoolMS.Infrastructure.Services;
using Serilog;
using System.Text;

// Npgsql 6+ requires DateTimeKind.Utc for timestamptz columns.
// This switch makes it treat Unspecified/Local datetimes as UTC,
// so JSON-deserialized dates from the frontend work without per-field conversion.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ── Azure Key Vault (production only) ─────────────────────────────────────────
if (builder.Environment.IsProduction())
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
    }
}

// ── Serilog ───────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// ── Application Insights (production only — requires connection string) ───────
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditInterceptor>();

// File storage: Azure Blob in production, local filesystem in development
if (builder.Environment.IsProduction())
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
else
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// ── Redis Distributed Cache ───────────────────────────────────────────────────
var redisConnection = builder.Configuration["Redis:Connection"];
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "SchoolMS:";
    });
}
else
{
    // Fallback to in-memory cache for local development
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddScoped<RedisCacheService>();

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
});

// ── Application Services ──────────────────────────────────────────────────────
// Auth
builder.Services.AddScoped<IAuthService, AuthService>();

// Sprint 2
builder.Services.AddScoped<StudentNumberGenerator>();
builder.Services.AddScoped<StaffNumberGenerator>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGuardianService, GuardianService>();

// Sprint 3
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IAcademicYearService, AcademicYearService>();
builder.Services.AddScoped<ITermService, TermService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();

// Sprint 4
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Sprint 5
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<IGradeService, GradeService>();

// Sprint 6
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Sprint 7
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<ITransportService, TransportService>();
builder.Services.AddScoped<IHostelService, HostelService>();
builder.Services.AddScoped<IExamService, ExamService>();

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<DatabaseHealthCheck>();

var healthBuilder = builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, new[] { "db" })
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgresql",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db" });

// Add Redis health check only if Redis is configured
if (!string.IsNullOrEmpty(redisConnection))
{
    healthBuilder.AddRedis(redisConnection, name: "redis",
        failureStatus: HealthStatus.Degraded, tags: new[] { "cache" });
}


// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexibleDateTimeConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SchoolMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Security Headers ──────────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    ctx.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    if (app.Environment.IsProduction())
    {
        ctx.Response.Headers.Append("Strict-Transport-Security",
            "max-age=31536000; includeSubDomains");
    }

    await next();
});



// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseCors("FrontendPolicy");

if (app.Environment.IsDevelopment())
{
    
    app.UseSwagger();                          // serves /swagger/v1/swagger.json
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SchoolMS API v1"));
    // available at /swagger
}
app.UseAuthentication();
app.UseAuthorization();

// Local file serving — development only (production uses Azure Blob Storage)
if (app.Environment.IsDevelopment())
{
    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

// ── Health Check Endpoints ────────────────────────────────────────────────────
// Liveness: is the process alive?
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks — just confirms process is running
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Readiness: can we serve traffic?
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Full health: all checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});


app.MapControllers();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
using var seedScope = app.Services.CreateScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Seed grading scale
    if (!seedDb.GradingScales.Any())
    {
        seedDb.GradingScales.AddRange(
            new SchoolMS.Core.Entities.GradingScale { Name = "A+", MinScore = 90, MaxScore = 100, Remark = "Excellent", GradePoint = 4.0m, IsDefault = true },
            new SchoolMS.Core.Entities.GradingScale { Name = "A", MinScore = 80, MaxScore = 89, Remark = "Very Good", GradePoint = 4.0m },
            new SchoolMS.Core.Entities.GradingScale { Name = "B+", MinScore = 75, MaxScore = 79, Remark = "Good", GradePoint = 3.5m },
            new SchoolMS.Core.Entities.GradingScale { Name = "B", MinScore = 70, MaxScore = 74, Remark = "Good", GradePoint = 3.0m },
            new SchoolMS.Core.Entities.GradingScale { Name = "C+", MinScore = 65, MaxScore = 69, Remark = "Average", GradePoint = 2.5m },
            new SchoolMS.Core.Entities.GradingScale { Name = "C", MinScore = 60, MaxScore = 64, Remark = "Average", GradePoint = 2.0m },
            new SchoolMS.Core.Entities.GradingScale { Name = "D", MinScore = 50, MaxScore = 59, Remark = "Below Average", GradePoint = 1.0m },
            new SchoolMS.Core.Entities.GradingScale { Name = "F", MinScore = 0, MaxScore = 49, Remark = "Fail", GradePoint = 0.0m }
        );
        seedDb.SaveChanges();
        Console.WriteLine("[SEED] Grading scales seeded.");
    }



app.Run();