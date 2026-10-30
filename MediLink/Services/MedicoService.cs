
using MediLink.Data;
using MediLink.DTOs;
using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Services;

public class MedicoService
{
    private readonly MediLinkDbContext _context;

    public MedicoService(MediLinkDbContext context)
    {
        _context = context;
    }

    public async Task<(Medico? Medico, bool MatriculaDuplicada)>
        CrearAsync(MedicoDTO dto)
    {
        bool existe = await _context.Medicos
            .AnyAsync(m => m.Matricula == dto.Matricula);

        if (existe)
        {
            return (null, true);
        }

        var medico = new Medico
        {
            Nombre = dto.Nombre.Trim(),
            Matricula = dto.Matricula.Trim(),
            Especialidad = dto.Especialidad.Trim()
        };

        _context.Medicos.Add(medico);

        try
        {
            await _context.SaveChangesAsync();
            return (medico, false);
        }
        catch (DbUpdateException)
        {
            // El índice único protege ante duplicados simultáneos.
            return (null, true);
        }
    }

    public async Task<List<Medico>> ObtenerTodosAsync()
    {
        return await _context.Medicos
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Medico?> ObtenerPorIdAsync(int id)
    {
        return await _context.Medicos
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);
    }
}
