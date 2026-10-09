using MediLink.Common;
using MediLink.DTOs;
using MediLink.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

[ApiController]
[Route("api/[controller]")]   // /api/recetas
public class RecetasController : ControllerBase
{
    private readonly IRecetaService _servicio;

    public RecetasController(IRecetaService servicio)
    {
        _servicio = servicio;
    }

    // POST /api/consultas/5/recetas
    // El "~/" ignora la ruta base del controlador: la receta es un subrecurso de la consulta.
    // TODO (Sol): agregar [RequiereApiKey] cuando el middleware esté listo. Esta ruta debe quedar protegida.
    [HttpPost("~/api/consultas/{consultaId}/recetas")]
    public async Task<ActionResult<ApiResponse<RecetaEmitidaDto>>> Emitir(
        int consultaId, [FromBody] RecetaCreateDto dto)
    {
        try
        {
            var emitida = await _servicio.EmitirAsync(consultaId, dto);

            var mensaje = emitida.FarmaciaNotificada
                ? "Receta emitida y notificada a la farmacia."
                : "Receta emitida, pero no se pudo notificar a la farmacia.";

            return CreatedAtAction(nameof(GetPorCodigo), new { codigo = emitida.Receta.Codigo },
                ApiResponse<RecetaEmitidaDto>.Ok(emitida, mensaje));
        }
        catch (KeyNotFoundException ex)   // la consulta no existe
        {
            return NotFound(ApiResponse<RecetaEmitidaDto>.Fail(ex.Message));
        }
    }

    // GET /api/recetas/RX-1A2B3C4D
    [HttpGet("{codigo}")]
    public async Task<ActionResult<ApiResponse<RecetaReadDto>>> GetPorCodigo(string codigo)
    {
        var receta = await _servicio.GetPorCodigoAsync(codigo);

        if (receta is null)
            return NotFound(ApiResponse<RecetaReadDto>.Fail($"No existe la receta con código {codigo}."));

        return Ok(ApiResponse<RecetaReadDto>.Ok(receta, "Receta obtenida correctamente."));
    }
}