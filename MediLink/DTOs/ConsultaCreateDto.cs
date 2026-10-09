using System.ComponentModel.DataAnnotations;

namespace MediLink.DTOs;

// Lo que el cliente ENVÍA para registrar una consulta.
// No tiene Id (lo genera la base) ni objetos anidados: solo referencias por id.
public class ConsultaCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar el médico.")]
    public int MedicoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar el paciente.")]
    public int PacienteId { get; set; }

    // Opcional: si no viene, se usa la fecha actual.
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
    [StringLength(500, MinimumLength = 1)]
    public string Diagnostico { get; set; } = "";

    [StringLength(1000)]
    public string Observaciones { get; set; } = "";
}