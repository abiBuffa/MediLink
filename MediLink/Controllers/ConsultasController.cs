
using MediLink.DTOs;
using MediLink.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

/// <summary>
/// Alta y consulta de consultas médicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]   // /api/consultas
[Produces("application/json")]
public class ConsultasController : ControllerBase
{
    private readonly IConsultaService _servicio;

    public ConsultasController(IConsultaService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>
    /// Lista todas las consultas médicas registradas en el sistema.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConsultaReadDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConsultaReadDto>>>> GetTodas()
    {
        var consultas = await _servicio.GetTodasAsync();
        return Ok(ApiResponse<IEnumerable<ConsultaReadDto>>.Ok(consultas, "Consultas obtenidas correctamente."));
    }

    /// <summary>
    /// Obtiene una consulta médica por su ID.
    /// </summary>
    /// <param name="id">Identificador de la consulta</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ConsultaReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConsultaReadDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConsultaReadDto>>> GetPorId(int id)
    {
        var consulta = await _servicio.GetPorIdAsync(id);

        if (consulta is null)
            return NotFound(ApiResponse<ConsultaReadDto>.Fail($"No existe la consulta con id {id}."));

        return Ok(ApiResponse<ConsultaReadDto>.Ok(consulta, "Consulta obtenida correctamente."));
    }

    /// <summary>
    /// Registra una nueva consulta médica en el sistema.
    /// </summary>
    /// <remarks> El médico y el paciente deben existir previamente en el sistema. Si no se envia fecha se usa la actual.</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ConsultaReadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ConsultaReadDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ConsultaReadDto>>> Crear([FromBody] ConsultaCreateDto dto)
    {
        try
        {
            var creada = await _servicio.CrearAsync(dto);

            // 201 Created + header Location con la URI del recurso nuevo
            return CreatedAtAction(nameof(GetPorId), new { id = creada.Id },
                ApiResponse<ConsultaReadDto>.Ok(creada, "Consulta registrada correctamente."));
        }
        catch (ArgumentException ex)   // médico o paciente inexistente
        {
            return BadRequest(ApiResponse<ConsultaReadDto>.Fail(ex.Message));
        }
    }
}