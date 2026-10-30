namespace MediLink.Clients;

/// <summary>
/// Se lanza cuando PharmaLink no está disponible (caída, timeout o error 5xx).
/// ReposicionService la captura, marca el pedido como Fallido y responde 503.
/// </summary>
public class FarmaciaNoDisponibleException : Exception
{
    public FarmaciaNoDisponibleException(string message, Exception? inner = null) : base(message, inner) { }
}
