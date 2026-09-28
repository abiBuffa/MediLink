namespace MediLink.Models;

public class Receta
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int ConsultaId { get; set; }
    public Consulta? Consulta { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public EstadoReceta Estado { get; set; }
    public string Medicamentos { get; set; } = string.Empty;
    public DateTime? FechaDispensacion { get; set; }
}