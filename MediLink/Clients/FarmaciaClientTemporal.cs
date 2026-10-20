namespace MediLink.Clients;

/// <summary>
/// ⚠️ TEMPORAL: solo para probar la reposición mientras Guille termina FarmaciaClient.cs.
/// Simula que PharmaLink está CAÍDA para comprobar la resiliencia (pedido Fallido + 503).
/// Borrar este archivo y su línea en Program.cs cuando esté el cliente real.
/// </summary>
public class FarmaciaClientTemporal : IFarmaciaClient
{
    public Task<SolicitudInsumosResponse> SolicitarInsumosAsync(SolicitudInsumosRequest solicitud, CancellationToken ct = default) =>
        throw new FarmaciaNoDisponibleException("PharmaLink no responde (cliente temporal de prueba).");

    public Task<NotificacionRecetaResponse> NotificarRecetaAsync(NotificacionRecetaRequest notificacion, CancellationToken ct = default) =>
        throw new FarmaciaNoDisponibleException("PharmaLink no responde (cliente temporal de prueba).");
}
