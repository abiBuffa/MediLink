var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Misma clave que MediLink envía en PharmaLink:ApiKey (appsettings.json de MediLink).
var apiKeyEsperada = app.Configuration["ApiKey"] ?? "MediLink-Secret-Key-2026";

// La farmacia exige X-API-Key en todas sus rutas /api.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var clave) || clave != apiKeyEsperada)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "API Key inválida o ausente." });
            return;
        }
    }
    await next();
});

// 1. Recibe la notificación de una receta nueva.
//    Contrato (lo que envía FarmaciaClient.NotificarRecetaAsync): { codigo, medicamentos, fechaEmision }
app.MapPost("/api/recetas/notificar", (RecetaNotificacionDto dto) =>
{
    Console.WriteLine($"[FARMACIA MOCK] Receta recibida: {dto.Codigo} ({dto.Medicamentos})");
    return Results.Ok(new { success = true, message = "Receta recibida correctamente en la farmacia." });
});

// 2. Recibe una solicitud de reposición de insumos.
//    Contrato (lo que envía FarmaciaClient.SolicitarInsumosAsync): { insumo, cantidad }
//    Respuesta (lo que lee RespuestaReposicion): { confirmado, codigoEnvio, mensaje }
app.MapPost("/api/reposiciones", (PedidoInsumoDto dto) =>
{
    Console.WriteLine($"[FARMACIA MOCK] Pedido de reposición recibido para: {dto.Insumo} x {dto.Cantidad}");

    // Para probar la rama "Rechazado" de MediLink: la farmacia no despacha más de 1000 unidades.
    if (dto.Cantidad > 1000)
    {
        return Results.Ok(new
        {
            confirmado = false,
            codigoEnvio = "",
            mensaje = "Cantidad superior al stock disponible en la farmacia."
        });
    }

    var codigoEnvio = "ENV-" + Guid.NewGuid().ToString("N")[..8].ToUpper();

    return Results.Ok(new
    {
        confirmado = true,
        codigoEnvio,
        mensaje = "Solicitud procesada y despacho programado.",
        fechaEstimada = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
    });
});

app.Run("http://localhost:5050");

record RecetaNotificacionDto(string Codigo, string Medicamentos, DateTime FechaEmision);
record PedidoInsumoDto(string Insumo, int Cantidad);