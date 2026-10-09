namespace MediLink.DTOs;

// Lo que la API DEVUELVE de una receta.
public class RecetaReadDto
{
    public string Codigo { get; set; } = "";
    public int ConsultaId { get; set; }
    public string Paciente { get; set; } = "";
    public string Medico { get; set; } = "";
    public string Medicamentos { get; set; } = "";
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = "";
    public DateTime? FechaDispensacion { get; set; }
}