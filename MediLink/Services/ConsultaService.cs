using MediLink.Data;
using MediLink.DTOs;
using MediLink.Interfaces;
using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Services;

public class ConsultaService : IConsultaService
{
    private readonly MediLinkDbContext _db;
    private readonly ILogger<ConsultaService> _logger;

    // El DbContext y el logger llegan por inyección de dependencias.
    public ConsultaService(MediLinkDbContext db, ILogger<ConsultaService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ConsultaReadDto>> GetTodasAsync()
        => await ProyectarConsultas().ToListAsync();

    public async Task<ConsultaReadDto?> GetPorIdAsync(int id)
        => await ProyectarConsultas().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<ConsultaReadDto> CrearAsync(ConsultaCreateDto dto)
    {
        // Regla de negocio: la consulta necesita un médico y un paciente que existan.
        // ArgumentException = el body referencia algo inválido (el controlador responde 400).
        if (!await _db.Medicos.AnyAsync(m => m.Id == dto.MedicoId))
            throw new ArgumentException($"No existe el médico con id {dto.MedicoId}.");

        if (!await _db.Pacientes.AnyAsync(p => p.Id == dto.PacienteId))
            throw new ArgumentException($"No existe el paciente con id {dto.PacienteId}.");

        var consulta = new Consulta
        {
            MedicoId = dto.MedicoId,
            PacienteId = dto.PacienteId,
            Fecha = dto.Fecha ?? DateTime.UtcNow,
            Diagnostico = dto.Diagnostico,
            Observaciones = dto.Observaciones
        };

        _db.Consultas.Add(consulta);        // solo marca el cambio en memoria
        await _db.SaveChangesAsync();       // recién acá se escribe en la base

        // Logging estructurado: el id va como parámetro, no pegado al texto.
        // No registramos el diagnóstico (dato sensible).
        _logger.LogInformation("Consulta {ConsultaId} registrada (médico {MedicoId}, paciente {PacienteId})",
            consulta.Id, consulta.MedicoId, consulta.PacienteId);

        var creada = await GetPorIdAsync(consulta.Id);
        return creada!;
    }

    // Select proyecta cada fila directo al DTO: EF trae solo esas columnas
    // y no hace falta Include.
    private IQueryable<ConsultaReadDto> ProyectarConsultas()
        => _db.Consultas.Select(c => new ConsultaReadDto
        {
            Id = c.Id,
            MedicoId = c.MedicoId,
            Medico = c.Medico!.Nombre,
            PacienteId = c.PacienteId,
            Paciente = c.Paciente!.Nombre + " " + c.Paciente.Apellido,
            Fecha = c.Fecha,
            Diagnostico = c.Diagnostico,
            Observaciones = c.Observaciones
        });
}