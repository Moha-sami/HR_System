using System.Text;
using Buy2.Api.Services.PointsAutomation;
using Buy2.Application;
using Buy2.Application.Features.Points.Automation;
using Buy2.Infrastructure;
using Buy2.Infrastructure.Persistence;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers
builder.Services.AddControllers();

// Configure CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure JWT Bearer Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "Buy2HRMS_SuperSecretKey_ForJWTTokenGeneration_2026";
var issuer = jwtSettings["Issuer"] ?? "Buy2.Api";
var audience = jwtSettings["Audience"] ?? "Buy2.Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = System.TimeSpan.Zero
    };

});

builder.Services.AddAuthorization();

// Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Buy2 HRMS Core API",
        Version = "v1",
        Description = "Enterprise Human Resource Management & Shift Market Board API for Buy2 System",
        Contact = new OpenApiContact
        {
            Name = "Buy2 Dev Team",
            Email = "dev@buy2hrms.com"
        }
    });

    // Add JWT Bearer Auth Definition to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token directly."
    });

    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

// Add Application Layer Services (MediatR Handlers)
builder.Services.AddApplicationServices();

// Add Infrastructure Layer Services (DbContext, Repositories, UnitOfWork, JWT Generator)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Points automation options (Hangfire dispatcher)
builder.Services.Configure<PointsAutomationOptions>(
    builder.Configuration.GetSection(PointsAutomationOptions.SectionName));

// Hangfire background jobs (SQL Server storage)
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.;Database=HrSystemDb;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString, new SqlServerStorageOptions
    {
        SchemaName = "hangfire",
        QueuePollInterval = TimeSpan.FromSeconds(15),
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        PrepareSchemaIfNecessary = true
    }));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<PointsAutomationDispatcher>();

var app = builder.Build();

// Auto-Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<Buy2DbContext>();
        await DatabaseSeeder.SeedAsync(dbContext);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "FATAL: Database seeding or startup check failed: {Message}", ex.Message);
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Enable Swagger UI middleware at root ("/")
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Buy2 HRMS Core API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root URL
});

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");

// Authentication must run BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Hangfire dashboard (Admin roles only)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthFilter() }
});

// Daily points automation dispatcher (12:00 PM Africa/Cairo by default)
var automationOptions = builder.Configuration
    .GetSection(PointsAutomationOptions.SectionName)
    .Get<PointsAutomationOptions>() ?? new PointsAutomationOptions();

if (automationOptions.Enabled)
{
    var cairoTimeZone = CairoPeriodResolver.GetTimeZone(automationOptions.TimeZone);
    RecurringJob.AddOrUpdate<PointsAutomationDispatcher>(
        "points-automation-dispatcher",
        dispatcher => dispatcher.DispatchDailyAsync(),
        automationOptions.DailyDispatcherCron,
        new RecurringJobOptions { TimeZone = cairoTimeZone });
}

app.Run();
