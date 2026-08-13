using Buy2.Application;
using Buy2.Infrastructure;
using Buy2.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers
builder.Services.AddControllers();

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
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

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

// Add Application Layer Services (MediatR Handlers)
builder.Services.AddApplicationServices();

// Add Infrastructure Layer Services (DbContext, Repositories, UnitOfWork, JWT Generator)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Auto-Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Buy2DbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
}

// Enable Swagger UI middleware at root ("/")
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Buy2 HRMS Core API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at root URL
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
