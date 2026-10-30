namespace MediLink.DTOs;

/// <summary>
/// Formato estándar de respuesta para TODA la API: { success, message, data }.
/// Es la única clase ApiResponse del proyecto (antes había 3 copias y chocaban).
/// </summary>
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

    public static ApiResponse<T> Ok(T? data, string message = "Operación exitosa")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, T? data = default)
        => new() { Success = false, Message = message, Data = data };
}
