
using MediLink.DTOs;
using MediLink.Models;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

[ApiController]
[Route("api/medicos")]
public class MedicosController : ControllerBase
{
    private readonly MedicoService _service;

    public MedicosController(MedicoService service)
    {
        _service = service;
    }

    [HttpPost]
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

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Medico>>>> ObtenerTodos()
    {
        var medicos = await _service.ObtenerTodosAsync();

        return Ok(new ApiResponse<List<Medico>>(
            true,
            "Médicos obtenidos correctamente.",
            medicos));
    }

    [HttpGet("{id:int}")]
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
