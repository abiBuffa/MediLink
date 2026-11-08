using MediLink.DTOs;
using MediLink.Data;
using MediLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediLink.Controllers;

/// <summary>
/// Operaciones que la farmacia puede realizar sobre recetas médicas: validar y dispensar.
/// </summary>
[ApiController]
[Route("api/farmacia")]
[Produces("application/json")]
public class FarmaciaController : ControllerBase
{
    private readonly MediLinkDbContext _context;
    private readonly ILogger<FarmaciaController> _logger;

    public FarmaciaController(MediLinkDbContext context, ILogger<FarmaciaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Valida que una receta pueda dispensarse.</summary>
    /// <remarks>
    /// Requiere la cabecera <c>X-API-Key</c>.
    /// Rechaza la receta si no existe, ya fue dispensada o está vencida.
    /// </remarks>
    /// <param name="codigo">Código de la receta, por ejemplo RX-1A2B3C4D.</param>
    [HttpPost("recetas/{codigo}/validar")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ValidarReceta(string codigo)
    {
        _logger.LogInformation("Solicitud de validación recibida para receta: {Codigo}", codigo);

        var receta = await _context.Recetas
            .FirstOrDefaultAsync(r => r.Codigo == codigo);

        if (receta == null)
        {
            _logger.LogWarning("Validación fallida: Receta {Codigo} no encontrada.", codigo);
            return NotFound(ApiResponse<object>.Fail("La receta no existe en el sistema."));
        }

        if (receta.Estado == EstadoReceta.Activa && receta.FechaVencimiento < DateTime.UtcNow)
        {
            receta.Estado = EstadoReceta.Vencida;
            await _context.SaveChangesAsync();
            _logger.LogInformation("La receta {Codigo} se actualizó a Vencida.", codigo);
        }

        if (receta.Estado == EstadoReceta.Dispensada)
        {
            _logger.LogWarning("Validación fallida: Receta {Codigo} ya dispensada.", codigo);
            return BadRequest(ApiResponse<object>.Fail("La receta ya fue dispensada previamente."));
        }

        if (receta.Estado == EstadoReceta.Vencida)
        {
            _logger.LogWarning("Validación fallida: Receta {Codigo} vencida.", codigo);
            return BadRequest(ApiResponse<object>.Fail("La receta se encuentra vencida."));
        }

        var datosReceta = new
        {
            codigo = receta.Codigo,
            medicamentos = receta.Medicamentos,
            estado = receta.Estado.ToString()
        };

        _logger.LogInformation("Receta {Codigo} validada con éxito.", codigo);
        return Ok(ApiResponse<object>.Ok(datosReceta, "Receta válida para dispensar."));
    }

    /// <summary>Marca una receta como dispensada.</summary>
    /// <remarks>
    /// Requiere la cabecera <c>X-API-Key</c>.
    /// Responde 409 si la receta ya fue dispensada o está vencida.
    /// </remarks>
    /// <param name="codigo">Código de la receta, por ejemplo RX-1A2B3C4D.</param>
    [HttpPost("recetas/{codigo}/dispensar")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DispensarReceta(string codigo)
    {
        _logger.LogInformation("Solicitud de dispensación recibida para receta: {Codigo}", codigo);

        var receta = await _context.Recetas
            .FirstOrDefaultAsync(r => r.Codigo == codigo);

        if (receta == null)
        {
            _logger.LogWarning("Dispensación fallida: Receta {Codigo} no encontrada.", codigo);
            return NotFound(ApiResponse<object>.Fail("Receta no encontrada."));
        }

        if (receta.Estado == EstadoReceta.Dispensada)
        {
            _logger.LogWarning("Dispensación rechazada: Receta {Codigo} ya estaba dispensada.", codigo);
            return Conflict(ApiResponse<object>.Fail("La receta ya había sido dispensada anteriormente."));
        }

        if (receta.Estado == EstadoReceta.Vencida || receta.FechaVencimiento < DateTime.UtcNow)
        {
            if (receta.Estado != EstadoReceta.Vencida)
            {
                receta.Estado = EstadoReceta.Vencida;
                await _context.SaveChangesAsync();
            }
            _logger.LogWarning("Dispensación rechazada: Receta {Codigo} vencida.", codigo);
            return Conflict(ApiResponse<object>.Fail("No se puede dispensar una receta vencida."));
        }

        receta.Estado = EstadoReceta.Dispensada;
        receta.FechaDispensacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Receta {Codigo} dispensada con éxito.", codigo);

        var resultado = new
        {
            codigo = receta.Codigo,
            fechaDispensacion = receta.FechaDispensacion
        };

        return Ok(ApiResponse<object>.Ok(resultado, "Receta dispensada exitosamente."));
    }
}