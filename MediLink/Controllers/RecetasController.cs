using Microsoft.AspNetCore.Mvc;
using MediLink.Services;
using MediLink.DTOs;

namespace MediLink.Controllers
{
    [ApiController]
    public class RecetasController : ControllerBase
    {
        private readonly IRecetaService _recetaService;
        private const string API_KEY_VALIDA = "MediLink-Secret-Key-2026"; // Clave de prueba

        public RecetasController(IRecetaService recetaService)
        {
            _recetaService = recetaService;
        }

        // POST /api/consultas/{id}/recetas (Protegido con API Key)
        [HttpPost("api/consultas/{id}/recetas")]
        public async Task<IActionResult> EmitirReceta(int id, [FromBody] CrearRecetaDto dto)
        {
            // Validación de API Key desde los Headers
            if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey) || apiKey != API_KEY_VALIDA)
            {
                return Unauthorized(new { mensaje = "API Key inválida o no provista." });
            }

            var receta = await _recetaService.EmitirRecetaAsync(id, dto);
            if (receta == null) return NotFound(new { mensaje = "La consulta especificada no existe." });

            return Ok(receta);
        }

        // GET /api/recetas/{codigo}
        [HttpGet("api/recetas/{codigo}")]
        public async Task<IActionResult> ObtenerEstadoReceta(string codigo)
        {
            var receta = await _recetaService.ObtenerEstadoAsync(codigo);
            if (receta == null) return NotFound(new { mensaje = "Receta no encontrada." });

            return Ok(receta);
        }
    }
}