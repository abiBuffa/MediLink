using MediLink.Models;
using MediLink.DTOs;
using MediLink.Clients;
using Microsoft.Extensions.Logging;

namespace MediLink.Services
{
    public interface IRecetaService
    {
        Task<Receta?> EmitirRecetaAsync(int consultaId, CrearRecetaDto dto);
        Task<Receta?> ObtenerEstadoAsync(string codigo);
    }

    public class RecetaService : IRecetaService
    {
        private static readonly List<Receta> _recetas = new();
        private readonly IConsultaService _consultaService;
        private readonly IFarmaciaClient _farmaciaClient;
        private readonly ILogger<RecetaService> _logger;

        public RecetaService(
            IConsultaService consultaService, 
            IFarmaciaClient farmaciaClient, 
            ILogger<RecetaService> logger)
        {
            _consultaService = consultaService;
            _farmaciaClient = farmaciaClient;
            _logger = logger;
        }

        public async Task<Receta?> EmitirRecetaAsync(int consultaId, CrearRecetaDto dto)
        {
            var consulta = await _consultaService.ObtenerPorIdAsync(consultaId);
            if (consulta == null)
            {
                _logger.LogWarning("Intento fallido de emisión de receta. Consulta ID {ConsultaId} no encontrada.", consultaId);
                return null;
            }

            string codigoGenerado = $"RX-{Guid.NewGuid():N}"[..11].ToUpper();
            var fechaEmision = DateTime.UtcNow;
            var fechaVencimiento = fechaEmision.AddDays(30);

            var receta = new Receta
            {
                Id = _recetas.Count + 1,
                ConsultaId = consultaId,
                Codigo = codigoGenerado,
                FechaEmision = fechaEmision,
                FechaVencimiento = fechaVencimiento,
                Medicamentos = dto.Medicamentos,
                Estado = EstadoReceta.Activa
            };

            _recetas.Add(receta);

            _logger.LogInformation("RECETA EMITIDA | Código: {Codigo} | Emisión: {FechaEmision} | Vencimiento: {FechaVencimiento}", 
                codigoGenerado, fechaEmision, fechaVencimiento);

            await _farmaciaClient.NotificarRecetaAsync(codigoGenerado, dto.Medicamentos);

            return receta;
        }

        public Task<Receta?> ObtenerEstadoAsync(string codigo)
        {
            var receta = _recetas.FirstOrDefault(r => r.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
            if (receta == null) return Task.FromResult<Receta?>(null);

            if (receta.Estado == EstadoReceta.Activa && DateTime.UtcNow > receta.FechaVencimiento)
            {
                receta.Estado = EstadoReceta.Vencida;
                _logger.LogInformation("RECETA VENCIDA AUTOMÁTICAMENTE | Código: {Codigo} | Fecha Vencimiento: {FechaVencimiento}", 
                    receta.Codigo, receta.FechaVencimiento);
            }

            return Task.FromResult<Receta?>(receta);
        }
    }
}