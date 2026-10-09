using MediLink.Common;
using MediLink.DTOs;
using MediLink.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

[ApiController]
[Route("api/[controller]")]   // /api/consultas
public class ConsultasController : ControllerBase
{
    private readonly IConsultaService _servicio;

    public ConsultasController(IConsultaService servicio)
    {
        _servicio = servicio;
    }

    // GET /api/consultas
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConsultaReadDto>>>> GetTodas()
    {
        var consultas = await _servicio.GetTodasAsync();
        return Ok(ApiResponse<IEnumerable<ConsultaReadDto>>.Ok(consultas, "Consultas obtenidas correctamente."));
    }

    // GET /api/consultas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ConsultaReadDto>>> GetPorId(int id)
    {
        var consulta = await _servicio.GetPorIdAsync(id);

        if (consulta is null)
            return NotFound(ApiResponse<ConsultaReadDto>.Fail($"No existe la consulta con id {id}."));

        return Ok(ApiResponse<ConsultaReadDto>.Ok(consulta, "Consulta obtenida correctamente."));
    }

    // POST /api/consultas
    [HttpPost]
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