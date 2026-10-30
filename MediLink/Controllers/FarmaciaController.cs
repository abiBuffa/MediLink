using MediLink.DTOs;
using MediLink.Data;
using MediLink.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediLink.Controllers;

[ApiController]
[Route("api/farmacia")]
public class FarmaciaController : ControllerBase
{
    private readonly MediLinkDbContext _context;
    private readonly ILogger<FarmaciaController> _logger;

    public FarmaciaController(MediLinkDbContext context, ILogger<FarmaciaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("recetas/{codigo}/validar")]
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

        if (receta.Estado == EstadoReceta.Activa && receta.FechaVencimiento < DateTime.Now)
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

    [HttpPost("recetas/{codigo}/dispensar")]
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

        if (receta.Estado == EstadoReceta.Vencida || receta.FechaVencimiento < DateTime.Now)
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
        receta.FechaDispensacion = DateTime.Now;

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