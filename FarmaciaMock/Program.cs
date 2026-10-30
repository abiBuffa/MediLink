var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 1. Endpoint que simula recibir notificación de receta nueva
app.MapPost("/api/farmacia/notificar-receta", (RecetaNotificacionDto dto) =>
{
    Console.WriteLine($"[FARMACIA MOCK] Receta recibida: {dto.CodigoReceta}");
    return Results.Ok(new { success = true, message = "Receta recibida correctamente en la farmacia." });
});

// 2. Endpoint que simula recibir solicitud de reposición de insumos
app.MapPost("/api/farmacia/pedidos-reposicion", (PedidoInsumoDto dto) =>
{
    Console.WriteLine($"[FARMACIA MOCK] Pedido de reposición recibido para: {dto.InsumoNombre} x {dto.Cantidad}");
    var codigoEnvio = "ENV-" + Guid.NewGuid().ToString()[..8].ToUpper();
    
    return Results.Ok(new
    {
        success = true,
        message = "Solicitud procesada y despacho programado.",
        codigoEnvio = codigoEnvio,
        fechaEstimada = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd")
    });
});

app.Run("http://localhost:5050");

record RecetaNotificacionDto(string CodigoReceta, string DniPaciente, string MatriculaMedico);
record PedidoInsumoDto(string InsumoNombre, int Cantidad);