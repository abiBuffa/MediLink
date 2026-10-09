namespace MediLink.Common;

// Estructura ÚNICA de respuesta para todos los endpoints de MediLink.
// Todos los controladores devuelven ApiResponse<T> para que el JSON sea siempre igual.
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    // Respuesta exitosa (200 / 201)
    public static ApiResponse<T> Ok(T data, string message = "")
        => new() { Success = true, Message = message, Data = data };

    // Respuesta de error (400 / 404 / 409 / etc.)
    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message, Data = default };
}