
using MediLink.DTOs;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MediLink.Controllers;

[ApiController]
[Route("api/vista")]
[Produces("application/json")]
public class VistaPedidosController : ControllerBase
{
    private readonly IReposicionService _reposicionService;
    private readonly ILogger<VistaPedidosController> _logger;

    public VistaPedidosController(
        IReposicionService reposicionService,
        ILogger<VistaPedidosController> logger)
    {
        _reposicionService = reposicionService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los pedidos de reposición
    /// con sus insumos, estados y respuestas de farmacia.
    /// </summary>
    [HttpGet("pedidos")]
    [ProducesResponseType(
        typeof(ApiResponse<List<PedidoReposicionResponseDTO>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerPedidos()
    {
        _logger.LogInformation(
            "Iniciando consulta de la vista consolidada de pedidos.");

        var resultado =
            await _reposicionService.ObtenerTodosAsync();

        if (!resultado.IsSuccess)
        {
            _logger.LogWarning(
                "No se pudo obtener la vista consolidada. Código: {StatusCode}. Mensaje: {Message}",
                resultado.StatusCode,
                resultado.Message);

            var error = ApiResponse<List<PedidoReposicionResponseDTO>>.Fail(
                resultado.Message);

            return StatusCode(resultado.StatusCode, error);
        }

        var pedidos = resultado.Data
            ?? new List<PedidoReposicionResponseDTO>();

        _logger.LogInformation(
            "Consulta consolidada completada. Cantidad de pedidos: {Cantidad}",
            pedidos.Count);

        var respuesta =
            ApiResponse<List<PedidoReposicionResponseDTO>>.Ok(
                pedidos,
                resultado.Message);

        return Ok(respuesta);
    }
}
