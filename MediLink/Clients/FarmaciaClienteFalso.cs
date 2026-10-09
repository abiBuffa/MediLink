using MediLink.Interfaces;
using MediLink.Models;

namespace MediLink.Clients;

// TEMPORAL: versión "de mentira" para poder probar sin la farmacia real.
// Cuando Guille termine FarmaciaClient, se cambia UNA línea en Program.cs
// y el resto del código ni se entera (ese es el beneficio de la interfaz).
public class FarmaciaClientFalso : IFarmaciaClient
{
    public Task<bool> NotificarRecetaAsync(Receta receta)
        => Task.FromResult(true);

    public Task<RespuestaReposicion> SolicitarInsumosAsync(string nombreInsumo, int cantidad)
        => Task.FromResult(new RespuestaReposicion
        {
            Confirmado = true,
            CodigoEnvio = "ENV-PRUEBA",
            Mensaje = "Simulado"
        });
}