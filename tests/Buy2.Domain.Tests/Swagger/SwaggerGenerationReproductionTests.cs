using System;
using Buy2.Api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Buy2.Domain.Tests.Swagger;

public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "Buy2.Api";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public class SwaggerGenerationReproductionTests
{
    [Fact]
    public void SwaggerDoc_ShouldGenerateSuccessfully_WithoutThrowing500Exception()
    {
        // Arrange: Replicate the exact DI and Swagger configuration from Buy2.Api Program.cs
        var services = new ServiceCollection();

        services.AddLogging();
        var env = new TestWebHostEnvironment();
        services.AddSingleton<IWebHostEnvironment>(env);
        services.AddSingleton<IHostEnvironment>(env);
        services.AddRouting();

        var controllerAssembly = typeof(DepartmentsController).Assembly;
        services.AddControllers()
            .AddApplicationPart(controllerAssembly)
            .AddControllersAsServices();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
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

        var serviceProvider = services.BuildServiceProvider();
        var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();

        // Act: Generate Swagger document for v1
        // Expected: Swagger document generates successfully without 500 / SwaggerGeneratorException
        // Actual (Bug): Throws SwaggerGeneratorException due to [FromForm] IFormFile parameter in GetSitesController.UploadDocument
        var swaggerDoc = swaggerProvider.GetSwagger("v1");

        // Assert
        Assert.NotNull(swaggerDoc);
        Assert.NotNull(swaggerDoc.Paths);
        Assert.Contains("/api/v1/sites/{id}/documents", swaggerDoc.Paths.Keys);
    }
}
