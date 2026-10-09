namespace MediLink.DTOs;

// Lo que la API DEVUELVE de una consulta: nombres en vez de objetos completos.
public class ConsultaReadDto
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public string Medico { get; set; } = "";
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Diagnostico { get; set; } = "";
    public string Observaciones { get; set; } = "";
}