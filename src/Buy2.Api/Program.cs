using Buy2.Application.Features.Roles.CreateRole;
using Buy2.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add API Controllers
builder.Services.AddControllers();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateRoleCommand).Assembly));

// Add Infrastructure Layer Services (DbContext, Repositories, UnitOfWork, JWT Generator)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
