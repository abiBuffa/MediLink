
using MediLink.DTOs;
using MediLink.Models;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

/// <summary>
/// Alta y consulta de pacientes.
/// </summary>
[ApiController]
[Route("api/pacientes")]
[Produces("application/json")]
public class PacientesController : ControllerBase
{
    private readonly PacienteService _service;

    public PacientesController(PacienteService service)
    {
        _service = service;
    }

    /// <summary>
    /// Crea un nuevo paciente en el sistema.
    /// </summary>
    /// <remarks> El documento del paciente debe ser único. Si ya existe responde 409</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Paciente>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Paciente>>> Crear(
        [FromBody] PacienteDTO dto)
    {
        var resultado = await _service.CrearAsync(dto);

        if (resultado.DocumentoDuplicado)
        {
            return Conflict(new ApiResponse<object>(
                false,
                "Ya existe un paciente con ese documento.",
                null));
        }

        var paciente = resultado.Paciente!;

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = paciente.Id },
            new ApiResponse<Paciente>(
                true,
                "Paciente registrado correctamente.",
                paciente));
    }

    /// <summary>
    ///  Lista todos los pacientes
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<Paciente>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<Paciente>>>> ObtenerTodos()
    {
        var pacientes = await _service.ObtenerTodosAsync();

        return Ok(new ApiResponse<List<Paciente>>(
            true,
            "Pacientes obtenidos correctamente.",
            pacientes));
    }

    /// <summary>
    /// Obtiene un paciente por su ID.
    /// </summary>
    /// <param name="id">Identificador del paciente</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<Paciente>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Paciente>>> ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new ApiResponse<object>(
                false,
                "El ID debe ser mayor que cero.",
                null));
        }

        var paciente = await _service.ObtenerPorIdAsync(id);

        if (paciente is null)
        {
            return NotFound(new ApiResponse<object>(
                false,
                "No se encontró el paciente solicitado.",
                null));
        }

        return Ok(new ApiResponse<Paciente>(
            true,
            "Paciente obtenido correctamente.",
            paciente));
    }
}
