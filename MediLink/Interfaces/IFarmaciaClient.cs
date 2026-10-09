using MediLink.Clients;
using MediLink.Models;

namespace MediLink.Interfaces;

// Contrato: qué le puede pedir el hospital a la farmacia.
// La implementación real (FarmaciaClient) la hace Guille.
public interface IFarmaciaClient
{
    Task<bool> NotificarRecetaAsync(Receta receta);
    Task<RespuestaReposicion> SolicitarInsumosAsync(string nombreInsumo, int cantidad);
}