using MediLink.Data;
using Microsoft.EntityFrameworkCore;
using MediLink.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configuración del DbContext para SQLite
builder.Services.AddDbContext<MediLinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=medilink.db"));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();