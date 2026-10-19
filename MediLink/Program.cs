using MediLink.Data;
using MediLink.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<MedicoService>();
builder.Services.AddScoped<PacienteService>();

// Configuración del DbContext para SQLite
builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=medilink.db"));

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();