using MediLink.Common;
using MediLink.DTOs;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

[ApiController]
[Route("api/insumos")]
[Produces("application/json")]
public class InsumosController : ControllerBase
{
    private readonly IInsumoService _service;

    public InsumosController(IInsumoService service)
    {
        _service = service;
    }

    /// <summary>Da de alta un insumo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InsumoResponseDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InsumoResponseDTO>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] InsumoDTO dto)
    {
        var result = await _service.CrearAsync(dto);
        if (result.StatusCode == StatusCodes.Status201Created)
            return CreatedAtAction(nameof(ObtenerPorId), new { id = result.Data!.Id },
                ApiResponse<InsumoResponseDTO>.Ok(result.Data, result.Message));

        return ToActionResult(result);
    }

    /// <summary>Lista todos los insumos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InsumoResponseDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerTodos() =>
        ToActionResult(await _service.ObtenerTodosAsync());

    /// <summary>Lista los insumos con stock actual menor o igual al mínimo.</summary>
    [HttpGet("stock-bajo")]
    [ProducesResponseType(typeof(ApiResponse<List<InsumoResponseDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerStockBajo() =>
        ToActionResult(await _service.ObtenerStockBajoAsync());

    /// <summary>Obtiene un insumo por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<InsumoResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InsumoResponseDTO>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id) =>
        ToActionResult(await _service.ObtenerPorIdAsync(id));

    /// <summary>Actualiza el stock actual de un insumo.</summary>
    [HttpPut("{id:int}/stock")]
    [ProducesResponseType(typeof(ApiResponse<InsumoResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InsumoResponseDTO>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarStock(int id, [FromBody] ActualizarStockDTO dto) =>
        ToActionResult(await _service.ActualizarStockAsync(id, dto));

    private ObjectResult ToActionResult<T>(ServiceResult<T> result)
    {
        var body = result.IsSuccess
            ? ApiResponse<T>.Ok(result.Data, result.Message)
            : ApiResponse<T>.Fail(result.Message, result.Data);
        return StatusCode(result.StatusCode, body);
    }
}
