using MediLink.Data;
using MediLink.DTOs;
using MediLink.Interfaces;
using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Services;

public class RecetaService : IRecetaService
{
    private readonly MediLinkDbContext _db;
    private readonly IFarmaciaClient _farmacia;   // depende de la INTERFAZ, no de la clase concreta
    private readonly ILogger<RecetaService> _logger;

    public RecetaService(MediLinkDbContext db, IFarmaciaClient farmacia, ILogger<RecetaService> logger)
    {
        _db = db;
        _farmacia = farmacia;
        _logger = logger;
    }

    public async Task<RecetaEmitidaDto> EmitirAsync(int consultaId, RecetaCreateDto dto)
    {
        // 1) Criterio de aceptación: solo se emiten recetas desde consultas registradas.
        //    KeyNotFoundException = el recurso de la URL no existe (el controlador responde 404).
        var consulta = await _db.Consultas
            .Include(c => c.Medico)
            .Include(c => c.Paciente)
            .FirstOrDefaultAsync(c => c.Id == consultaId);

        if (consulta is null)
            throw new KeyNotFoundException(
                $"No existe la consulta {consultaId}. Solo se pueden emitir recetas desde consultas registradas.");

        // 2) Armamos la receta: código único, fecha de emisión, vencimiento y estado Activa.
        var ahora = DateTime.UtcNow;
        var receta = new Receta
        {
            Codigo = await GenerarCodigoUnicoAsync(),
            Consulta = consulta,
            FechaEmision = ahora,
            FechaVencimiento = ahora.AddDays(dto.DiasValidez),
            Estado = EstadoReceta.Activa,
            Medicamentos = dto.Medicamentos
        };

        _db.Recetas.Add(receta);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Receta {Codigo} emitida desde la consulta {ConsultaId}",
            receta.Codigo, consultaId);

        // 3) Avisamos a la farmacia. Si falla NO deshacemos la receta: ya existe y la
        //    farmacia puede validarla igual. Solo dejamos constancia en el log.
        bool notificada;
        try
        {
            notificada = await _farmacia.NotificarRecetaAsync(receta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la notificación de la receta {Codigo} a la farmacia", receta.Codigo);
            notificada = false;
        }

        if (!notificada)
            _logger.LogWarning("La farmacia no confirmó la receta {Codigo}", receta.Codigo);

        return new RecetaEmitidaDto { Receta = ToDto(receta), FarmaciaNotificada = notificada };
    }

    public async Task<RecetaReadDto?> GetPorCodigoAsync(string codigo)
    {
        var receta = await RecetasConDatos().FirstOrDefaultAsync(r => r.Codigo == codigo);

        if (receta is null)
            return null;   // el controlador lo traduce a 404

        // Estado "vencida": si sigue Activa pero ya pasó la fecha, la actualizamos al consultarla.
        if (receta.Estado == EstadoReceta.Activa && receta.FechaVencimiento < DateTime.UtcNow)
        {
            receta.Estado = EstadoReceta.Vencida;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Receta {Codigo} marcada como Vencida", codigo);
        }

        return ToDto(receta);
    }

    // Genera "RX-" + 8 caracteres de un Guid, y repite si por casualidad ya existe.
    private async Task<string> GenerarCodigoUnicoAsync()
    {
        string codigo;
        do
        {
            codigo = "RX-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
        while (await _db.Recetas.AnyAsync(r => r.Codigo == codigo));

        return codigo;
    }

    // Trae la receta junto con su consulta, médico y paciente (las navegaciones vienen vacías por defecto).
    private IQueryable<Receta> RecetasConDatos()
        => _db.Recetas
            .Include(r => r.Consulta).ThenInclude(c => c!.Medico)
            .Include(r => r.Consulta).ThenInclude(c => c!.Paciente);

    // Convierte la entidad en el DTO de salida.
    private static RecetaReadDto ToDto(Receta r)
    {
        var paciente = r.Consulta?.Paciente;
        var medico = r.Consulta?.Medico;

        return new RecetaReadDto
        {
            Codigo = r.Codigo,
            ConsultaId = r.ConsultaId,
            Paciente = paciente is null ? "" : $"{paciente.Nombre} {paciente.Apellido}",
            Medico = medico?.Nombre ?? "",
            Medicamentos = r.Medicamentos,
            FechaEmision = r.FechaEmision,
            FechaVencimiento = r.FechaVencimiento,
            Estado = r.Estado.ToString(),
            FechaDispensacion = r.FechaDispensacion
        };
    }
}