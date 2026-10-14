using MediLink.Clients;
using MediLink.Data;
using MediLink.Interfaces;
using MediLink.Services;
using Microsoft.EntityFrameworkCore;
using MediLink.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Inyección de dependencias
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IRecetaService, RecetaService>();

// Cliente HTTP Real de la Farmacia
builder.Services.AddHttpClient<IFarmaciaClient, FarmaciaClient>();

builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=medilink.db"));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();