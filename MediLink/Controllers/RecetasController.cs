
using MediLink.DTOs;
using MediLink.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

/// <summary>
/// Alta y consulta de recetas médicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]   // /api/recetas
[Produces("application/json")]
public class RecetasController : ControllerBase
{
    private readonly IRecetaService _servicio;

    public RecetasController(IRecetaService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Emite una receta a partir de una consulta registrada.</summary>
    /// <remarks>
    /// Requiere la cabecera <c>X-API-Key</c>.
    /// El código, las fechas y el estado (Activa) los genera el servidor.
    /// Después de guardar la receta se notifica a la farmacia; si la notificación falla,
    /// la receta igual queda emitida y <c>farmaciaNotificada</c> será <c>false</c>.
    /// </remarks>
    /// <param name="consultaId">Id de la consulta de origen.</param>
    /// <param name="dto">Medicamentos y días de validez (por defecto 30).</param>
    [HttpPost("~/api/consultas/{consultaId}/recetas")]
    [ProducesResponseType(typeof(ApiResponse<RecetaEmitidaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<RecetaEmitidaDto>), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Obtiene una receta por su código.
    /// </summary>
    /// <remarks>Si la receta seguía Activa pero ya pasó su fecha de vencimiento, se marca como Vencida.</remarks>
    /// <param name="codigo">Código de la receta, por ejemplo: RX-1A2B3C4D</param>
    [HttpGet("{codigo}")]
    [ProducesResponseType(typeof(ApiResponse<RecetaReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecetaReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RecetaReadDto>>> GetPorCodigo(string codigo)
    {
        var receta = await _servicio.GetPorCodigoAsync(codigo);

        if (receta is null)
            return NotFound(ApiResponse<RecetaReadDto>.Fail($"No existe la receta con código {codigo}."));

        return Ok(ApiResponse<RecetaReadDto>.Ok(receta, "Receta obtenida correctamente."));
    }
}