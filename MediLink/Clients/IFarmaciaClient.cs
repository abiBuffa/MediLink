namespace MediLink.Clients;

/// <summary>
/// Contrato para comunicarse con la API externa de la farmacia (PharmaLink).
/// La implementación con HttpClient es FarmaciaClient.cs (Guille).
///
/// Si PharmaLink está caída, no responde o da timeout, la implementación debe lanzar
/// FarmaciaNoDisponibleException (o dejar pasar HttpRequestException / TaskCanceledException):
/// los servicios capturan esas excepciones y responden 503 sin romper el sistema.
/// </summary>
public interface IFarmaciaClient
{
    /// <summary>POST {PharmaLink}/api/reposiciones</summary>
    Task<SolicitudInsumosResponse> SolicitarInsumosAsync(SolicitudInsumosRequest solicitud, CancellationToken ct = default);

    /// <summary>POST {PharmaLink}/api/recetas/notificar</summary>
    Task<NotificacionRecetaResponse> NotificarRecetaAsync(NotificacionRecetaRequest notificacion, CancellationToken ct = default);
}

// ---------- Contratos JSON con PharmaLink (camelCase, fechas ISO 8601) ----------

public class SolicitudInsumosRequest
{
    public int PedidoId { get; set; }
    public int InsumoId { get; set; }
    public string NombreInsumo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaPedido { get; set; }
}

public class SolicitudInsumosResponse
{
    public bool Aceptado { get; set; }
    public string? CodigoEnvio { get; set; }
    public string? Mensaje { get; set; }
}

public class NotificacionRecetaRequest
{
    public string Codigo { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Medicamentos { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class NotificacionRecetaResponse
{
    public bool Recibida { get; set; }
    public string? Mensaje { get; set; }
}

/// <summary>Se lanza cuando PharmaLink no está disponible (caída, timeout, 5xx).</summary>
public class FarmaciaNoDisponibleException : Exception
{
    public FarmaciaNoDisponibleException(string message, Exception? inner = null) : base(message, inner) { }
}
