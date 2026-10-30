using System.ComponentModel.DataAnnotations;

namespace MediLink.DTOs;

public class PacienteDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento es obligatorio.")]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateTime? FechaNacimiento { get; set; }

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Correo { get; set; } = string.Empty;
}