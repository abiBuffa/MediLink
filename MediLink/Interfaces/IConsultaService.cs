using MediLink.DTOs;

namespace MediLink.Interfaces;

// Contrato del servicio de consultas: qué operaciones ofrece, sin decir cómo.
public interface IConsultaService
{
    Task<IEnumerable<ConsultaReadDto>> GetTodasAsync();
    Task<ConsultaReadDto?> GetPorIdAsync(int id);
    Task<ConsultaReadDto> CrearAsync(ConsultaCreateDto dto);
}