using Microsoft.AspNetCore.Mvc;
using MediLink.Services;
using MediLink.DTOs;

namespace MediLink.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IConsultaService _consultaService;

        public ConsultasController(IConsultaService consultaService)
        {
            _consultaService = consultaService;
        }

        // POST /api/consultas
        [HttpPost]
        public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaDto dto)
        {
            var consulta = await _consultaService.CrearConsultaAsync(dto);
            if (consulta == null) return BadRequest("No se pudo crear la consulta.");

            return CreatedAtAction(nameof(ObtenerPorId), new { id = consulta.Id }, consulta);
        }

        // GET /api/consultas/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var consulta = await _consultaService.ObtenerPorIdAsync(id);
            if (consulta == null) return NotFound(new { mensaje = "Consulta no encontrada." });

            return Ok(consulta);
        }
    }
}