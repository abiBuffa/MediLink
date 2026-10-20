using MediLink.Clients;
using MediLink.Common;
using MediLink.Data;
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

// Configuración del DbContext para SQLite
builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=medilink.db"));

// --- Kiandra: Insumos y Reposición ---
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IReposicionService, ReposicionService>();

// ⚠️ TEMPORAL hasta que Guille registre el cliente real:
// builder.Services.AddHttpClient<IFarmaciaClient, FarmaciaClient>(...);
// Cuando eso exista, borrar esta línea.
builder.Services.AddScoped<IFarmaciaClient, FarmaciaClientTemporal>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
