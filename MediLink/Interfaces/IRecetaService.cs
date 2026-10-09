using MediLink.DTOs;

namespace MediLink.Interfaces;

// Contrato del servicio de recetas.
public interface IRecetaService
{
    Task<RecetaEmitidaDto> EmitirAsync(int consultaId, RecetaCreateDto dto);
    Task<RecetaReadDto?> GetPorCodigoAsync(string codigo);
}