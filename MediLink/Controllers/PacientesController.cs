
using MediLink.DTOs;
using MediLink.Models;
using MediLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediLink.Controllers;

[ApiController]
[Route("api/pacientes")]
public class PacientesController : ControllerBase
{
    private readonly PacienteService _service;

    public PacientesController(PacienteService service)
    {
        _service = service;
    }

    [HttpPost]
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

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Paciente>>>> ObtenerTodos()
    {
        var pacientes = await _service.ObtenerTodosAsync();

        return Ok(new ApiResponse<List<Paciente>>(
            true,
            "Pacientes obtenidos correctamente.",
            pacientes));
    }

    [HttpGet("{id:int}")]
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
