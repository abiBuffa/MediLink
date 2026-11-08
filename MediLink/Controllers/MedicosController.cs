
using MediLink.DTOs;
using MediLink.Models;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

/// <summary>
/// Alta y consulta de médicos.
/// </summary>
[ApiController]
[Route("api/medicos")]
[Produces("application/json")]
public class MedicosController : ControllerBase
{
    private readonly MedicoService _service;

    public MedicosController(MedicoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registra un nuevo médico en el sistema.
    /// </summary>
    /// <remarks> La matrícula del médico debe ser única. Si ya existe responde 409</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Medico>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Medico>>> Crear(
        [FromBody] MedicoDTO dto)
    {
        var resultado = await _service.CrearAsync(dto);

        if (resultado.MatriculaDuplicada)
        {
            return Conflict(new ApiResponse<object>(
                false,
                "Ya existe un médico con esa matrícula.",
                null));
        }

        var medico = resultado.Medico!;

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = medico.Id },
            new ApiResponse<Medico>(
                true,
                "Médico registrado correctamente.",
                medico));
    }

    /// <summary>
    /// Lista todos los médicos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<Medico>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<Medico>>>> ObtenerTodos()
    {
        var medicos = await _service.ObtenerTodosAsync();

        return Ok(new ApiResponse<List<Medico>>(
            true,
            "Médicos obtenidos correctamente.",
            medicos));
    }

    /// <summary>
    /// Obtiene un médico por su ID.
    /// </summary>
    /// <param name="id">Identificador del médico</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<Medico>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Medico>>> ObtenerPorId(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new ApiResponse<object>(
                false,
                "El ID debe ser mayor que cero.",
                null));
        }

        var medico = await _service.ObtenerPorIdAsync(id);

        if (medico is null)
        {
            return NotFound(new ApiResponse<object>(
                false,
                "No se encontró el médico solicitado.",
                null));
        }

        return Ok(new ApiResponse<Medico>(
            true,
            "Médico obtenido correctamente.",
            medico));
    }
}
