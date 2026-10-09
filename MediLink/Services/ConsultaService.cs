using MediLink.Models;
using MediLink.DTOs;

namespace MediLink.Services
{
    public interface IConsultaService
    {
        Task<Consulta?> CrearConsultaAsync(CrearConsultaDto dto);
        Task<Consulta?> ObtenerPorIdAsync(int id);
    }

    public class ConsultaService : IConsultaService
    {
        private static readonly List<Consulta> _consultas = new();
        private static int _nextId = 1;

        public Task<Consulta?> CrearConsultaAsync(CrearConsultaDto dto)
        {
            var consulta = new Consulta
            {
                Id = _nextId++,
                PacienteId = dto.PacienteId,
                MedicoId = dto.MedicoId,
                Diagnostico = dto.Diagnostico,
                Fecha = DateTime.UtcNow
            };

            _consultas.Add(consulta);
            return Task.FromResult<Consulta?>(consulta);
        }

        public Task<Consulta?> ObtenerPorIdAsync(int id)
        {
            var consulta = _consultas.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(consulta);
        }
    }
}