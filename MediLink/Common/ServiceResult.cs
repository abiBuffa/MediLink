namespace MediLink.Common;

/// <summary>
/// Resultado que devuelven los servicios: el código HTTP que corresponde,
/// un mensaje y los datos. El controlador solo lo traduce a ApiResponse.
/// </summary>
public class ServiceResult<T>
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public bool IsSuccess => StatusCode is >= 200 and < 300;

    public static ServiceResult<T> Ok(T data, string message = "Operación exitosa") =>
        new() { StatusCode = StatusCodes.Status200OK, Message = message, Data = data };

    public static ServiceResult<T> Created(T data, string message = "Recurso creado") =>
        new() { StatusCode = StatusCodes.Status201Created, Message = message, Data = data };

    public static ServiceResult<T> NotFound(string message) =>
        new() { StatusCode = StatusCodes.Status404NotFound, Message = message };

    public static ServiceResult<T> BadRequest(string message) =>
        new() { StatusCode = StatusCodes.Status400BadRequest, Message = message };

    public static ServiceResult<T> Conflict(string message) =>
        new() { StatusCode = StatusCodes.Status409Conflict, Message = message };

    public static ServiceResult<T> ServiceUnavailable(string message, T? data = default) =>
        new() { StatusCode = StatusCodes.Status503ServiceUnavailable, Message = message, Data = data };
}
