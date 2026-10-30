using System.Net;
using System.Text.Json;

namespace MediLink.Middleware;

// Clase genérica para estandarizar las respuestas JSON de la API
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(bool success, string message, T? data = default)
    {
        Success = success;
        Message = message;
        Data = data;
    }
}
//Middleware para capturar cualquier excepcion/error no controlado en la API
//y devolver siempre una respuesta JSON uniforme con el codigo HTTP correspondiente.
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    // El constructor recibe por inyección de dependencias el siguiente delegado y el logger
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    //Método principal que ejecuta ASP.NET Core automáticamente para cada petición HTTP saliente/entrante.
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ha ocurrido un error no controlado en la aplicación.");
            await HandleExceptionAsync(context, ex);
        }
    }
    //Método que construye la respuesta JSON estandarizada según el tipo de excepción capturada.
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Asignamos codigo segun el tipo de excepcion (o 500 por defecto)
        var statusCode = exception switch
        {
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException or InvalidOperationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };
        // Asigna el código HTTP a la respuesta
        context.Response.StatusCode = (int)statusCode;

        var response = new ApiResponse<object>(
            success: false,
            message: exception.Message ?? "Ocurrio un error interno en el servidor."
        );

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}