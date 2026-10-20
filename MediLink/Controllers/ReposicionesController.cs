using MediLink.Common;
using MediLink.DTOs;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

[ApiController]
[Route("api/reposiciones")]
[Produces("application/json")]
public class ReposicionesController : ControllerBase
{
    private readonly IReposicionService _service;

    public ReposicionesController(IReposicionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Crea un pedido de reposición y lo envía a PharmaLink.
    /// Si la farmacia no responde, el pedido queda "Fallido" y se devuelve 503.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PedidoReposicionResponseDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PedidoReposicionResponseDTO>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PedidoReposicionResponseDTO>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Crear([FromBody] PedidoReposicionDTO dto, CancellationToken ct)
    {
        var result = await _service.CrearAsync(dto, ct);
        if (result.StatusCode == StatusCodes.Status201Created)
            return CreatedAtAction(nameof(ObtenerPorId), new { id = result.Data!.Id },
                ApiResponse<PedidoReposicionResponseDTO>.Ok(result.Data, result.Message));

        return ToActionResult(result);
    }

    /// <summary>Lista todos los pedidos de reposición.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PedidoReposicionResponseDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerTodos() =>
        ToActionResult(await _service.ObtenerTodosAsync());

    /// <summary>Obtiene un pedido de reposición por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PedidoReposicionResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PedidoReposicionResponseDTO>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id) =>
        ToActionResult(await _service.ObtenerPorIdAsync(id));

    private ObjectResult ToActionResult<T>(ServiceResult<T> result)
    {
        var body = result.IsSuccess
            ? ApiResponse<T>.Ok(result.Data, result.Message)
            : ApiResponse<T>.Fail(result.Message, result.Data);
        return StatusCode(result.StatusCode, body);
    }
}
