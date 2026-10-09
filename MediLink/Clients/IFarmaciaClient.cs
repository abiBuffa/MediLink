using Microsoft.Extensions.Logging;

namespace MediLink.Clients
{
    public interface IFarmaciaClient
    {
        Task NotificarRecetaAsync(string codigoReceta, string detalles);
    }

    public class FarmaciaClient : IFarmaciaClient
    {
        private readonly ILogger<FarmaciaClient> _logger;

        public FarmaciaClient(ILogger<FarmaciaClient> logger)
        {
            _logger = logger;
        }

        public Task NotificarRecetaAsync(string codigoReceta, string detalles)
        {
            _logger.LogInformation("[FARMACIA CLIENT] Receta notificada con éxito. Código: {Codigo}, Detalles: {Detalles}", codigoReceta, detalles);
            return Task.CompletedTask;
        }
    }
}