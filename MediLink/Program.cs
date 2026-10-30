using MediLink.Clients;

using MediLink.Common;
>>>>>>> origin/feature/kiandra-insumos
using MediLink.Data;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediLink.Middleware;

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

// Inyección de dependencias
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IRecetaService, RecetaService>();

// Cliente HTTP Real de la Farmacia
builder.Services.AddHttpClient<IFarmaciaClient, FarmaciaClient>();

origin/feature/integracion-farmacia
builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=medilink.db"));

// --- Kiandra: Insumos y Reposición ---
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IReposicionService, ReposicionService>();

// ⚠️ TEMPORAL hasta que Guille registre el cliente real:
// builder.Services.AddHttpClient<IFarmaciaClient, FarmaciaClient>(...);
// Cuando eso exista, borrar esta línea.
builder.Services.AddScoped<IFarmaciaClient, FarmaciaClientTemporal>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
 HEAD

app.Run();

app.Run();
 origin/feature/kiandra-insumos
