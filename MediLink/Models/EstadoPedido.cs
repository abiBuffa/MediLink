namespace MediLink.Models;

public enum EstadoPedido
{
    Pendiente,
    Enviado,
    Rechazado,
    Fallido   // PharmaLink no respondió (caída / timeout). Se guarda como texto, no requiere migración.
}
