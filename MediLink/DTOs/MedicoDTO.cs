using System.ComponentModel.DataAnnotations;

namespace MediLink.DTOs;

public class MedicoDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    public string Matricula { get; set; } = string.Empty;

    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    public string Especialidad { get; set; } = string.Empty;
}