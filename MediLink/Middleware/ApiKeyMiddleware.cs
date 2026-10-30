using System.Net;
using System.Text.Json;

namespace MediLink.Middleware;

// Middleware para validar la presencia y validez de la API Key en las rutas protegidas de la API
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string APIKEYHEADER = "X-API-Key";
    // El constructor recibe por inyección de dependencias el siguiente delegado
    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    // Método principal que ejecuta ASP.NET Core automáticamente para cada petición HTTP saliente/entrante.
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // Validamos si la ruta actual está en la lista de rutas protegidas
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        // Rutas que requieren API Key 
        bool isProtectedPath = path.Contains("/api/consultas/") && path.Contains("/recetas") || // Emisión
                                path.Contains("/api/farmacia/recetas") ||                       // Validación y dispensación
                                path.Contains("/api/reposiciones");                            // Solicitud de reposición
        // Si la ruta es protegida, validamos la presencia y validez de la API Key
        if (isProtectedPath)
        {
            if (!context.Request.Headers.TryGetValue(APIKEYHEADER, out var extractedApiKey))
            {
                await RespondUnauthorized(context, "Se requiere la cabecera 'X-API-Key' para acceder a este recurso.");
                return;
            }
            // Obtenemos la API Key configurada en appsettings.json
            var apiKey = configuration.GetValue<string>("ApiKey");

            if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
            {
                await RespondUnauthorized(context, "API Key inválida o no autorizada.");
                return;
            }
        }

        await _next(context);
    }
    // Método auxiliar para responder con un mensaje de error JSON estandarizado 
    // cuando la API Key es inválida o no está presente
    private static Task RespondUnauthorized(HttpContext context, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var response = new ApiResponse<object>(
            success: false,
            message: message
        );

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}