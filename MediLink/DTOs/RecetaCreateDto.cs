using System.ComponentModel.DataAnnotations;

namespace MediLink.DTOs;

// Lo que el cliente ENVÍA para emitir una receta.
// El código, las fechas y el estado los genera el servidor, no el cliente.
public class RecetaCreateDto
{
    [Required(ErrorMessage = "Debe indicar los medicamentos.")]
    [StringLength(1000, MinimumLength = 1)]
    public string Medicamentos { get; set; } = "";

    [Range(1, 365, ErrorMessage = "Los días de validez deben estar entre 1 y 365.")]
    public int DiasValidez { get; set; } = 30;
}