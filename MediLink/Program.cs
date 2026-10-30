using MediLink.Clients;
using MediLink.Data;
using MediLink.DTOs;
using MediLink.Interfaces;
using MediLink.Middleware;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Errores de validación (Data Annotations) con el formato estándar ApiResponse -> 400
        options.InvalidModelStateResponseFactory = context =>
        {
            var errores = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            return new BadRequestObjectResult(
                ApiResponse<Dictionary<string, string[]>>.Fail("Datos inválidos.", errores));
        };
    });

// Base de datos SQLite
builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=medilink.db"));

// --- Anael: Pacientes y Médicos ---
builder.Services.AddScoped<PacienteService>();
builder.Services.AddScoped<MedicoService>();

// --- Abi: Consultas y Recetas ---
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IRecetaService, RecetaService>();

// --- Kiandra: Insumos y Reposición ---
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IReposicionService, ReposicionService>();

// --- Guille: Cliente HTTP real de PharmaLink (Typed Client) ---
// URL base y X-API-Key se leen de appsettings.json (sección "PharmaLink").
builder.Services.AddHttpClient<IFarmaciaClient, FarmaciaClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10); // si PharmaLink no responde en 10 s -> pedido Fallido (503)
});

var app = builder.Build();

// --- Sol: Middlewares (el de errores primero, para que atrape todo) ---
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
