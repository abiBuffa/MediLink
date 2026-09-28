namespace MediLink.Models;

public class Consulta
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public Medico? Medico { get; set; }
    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }
    public DateTime Fecha { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
}