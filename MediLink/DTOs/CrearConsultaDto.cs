namespace MediLink.DTOs
{
    public class CrearConsultaDto
    {
        public int PacienteId { get; set; }
        public int MedicoId { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
    }
}