namespace MediLink.Models;

// Respuesta que devuelve la farmacia cuando el hospital le pide insumos.
// No es una tabla de la base: es un objeto para transportar la respuesta externa.
public class RespuestaReposicion
{
    public bool Confirmado { get; set; }
    public string CodigoEnvio { get; set; } = "";
    public string Mensaje { get; set; } = "";
}