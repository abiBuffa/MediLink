namespace MediLink.DTOs;

// Respuesta al emitir: la receta + si se pudo avisar a la farmacia.
public class RecetaEmitidaDto
{
    public RecetaReadDto Receta { get; set; } = new();
    public bool FarmaciaNotificada { get; set; }
}