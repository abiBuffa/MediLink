
using MediLink.Data;
using MediLink.DTOs;
using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Services;

public class PacienteService
{
    private readonly MediLinkDbContext _context;

    public PacienteService(MediLinkDbContext context)
    {
        _context = context;
    }

    public async Task<(Paciente? Paciente, bool DocumentoDuplicado)>
        CrearAsync(PacienteDTO dto)
    {
        bool existe = await _context.Pacientes
            .AnyAsync(p => p.Documento == dto.Documento);

        if (existe)
        {
            return (null, true);
        }

        var paciente = new Paciente
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Documento = dto.Documento.Trim(),
            FechaNacimiento = dto.FechaNacimiento!.Value,
            Correo = dto.Correo.Trim()
        };

        _context.Pacientes.Add(paciente);

        try
        {
            await _context.SaveChangesAsync();
            return (paciente, false);
        }
        catch (DbUpdateException)
        {
            return (null, true);
        }
    }

    public async Task<List<Paciente>> ObtenerTodosAsync()
    {
        return await _context.Pacientes
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Paciente?> ObtenerPorIdAsync(int id)
    {
        return await _context.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
