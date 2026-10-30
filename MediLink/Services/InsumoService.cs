using MediLink.Common;
using MediLink.Data;
using MediLink.DTOs;
using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Services;

public interface IInsumoService
{
    Task<ServiceResult<InsumoResponseDTO>> CrearAsync(InsumoDTO dto);
    Task<ServiceResult<List<InsumoResponseDTO>>> ObtenerTodosAsync();
    Task<ServiceResult<InsumoResponseDTO>> ObtenerPorIdAsync(int id);
    Task<ServiceResult<InsumoResponseDTO>> ActualizarStockAsync(int id, ActualizarStockDTO dto);
    Task<ServiceResult<List<InsumoResponseDTO>>> ObtenerStockBajoAsync();
}

public class InsumoService : IInsumoService
{
    private readonly MediLinkDbContext _db;
    private readonly ILogger<InsumoService> _logger;

    public InsumoService(MediLinkDbContext db, ILogger<InsumoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<InsumoResponseDTO>> CrearAsync(InsumoDTO dto)
    {
        var nombre = dto.Nombre.Trim();

        // SQLite compara con distinción de mayúsculas, por eso normalizamos.
        var existe = await _db.Insumos.AnyAsync(i => i.Nombre.ToLower() == nombre.ToLower());
        if (existe)
        {
            _logger.LogWarning("Intento de crear insumo duplicado: {Nombre}", nombre);
            return ServiceResult<InsumoResponseDTO>.Conflict($"Ya existe un insumo con el nombre '{nombre}'.");
        }

        var insumo = new Insumo
        {
            Nombre = nombre,
            StockActual = dto.StockActual,
            StockMinimo = dto.StockMinimo
        };

        _db.Insumos.Add(insumo);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Insumo creado. Id: {InsumoId}, Nombre: {Nombre}, Stock: {Stock}, Mínimo: {Minimo}",
            insumo.Id, insumo.Nombre, insumo.StockActual, insumo.StockMinimo);

        return ServiceResult<InsumoResponseDTO>.Created(ToDto(insumo), "Insumo creado correctamente.");
    }

    public async Task<ServiceResult<List<InsumoResponseDTO>>> ObtenerTodosAsync()
    {
        var insumos = await _db.Insumos.AsNoTracking().OrderBy(i => i.Nombre).ToListAsync();
        return ServiceResult<List<InsumoResponseDTO>>.Ok(insumos.Select(ToDto).ToList(),
            $"Se encontraron {insumos.Count} insumos.");
    }

    public async Task<ServiceResult<InsumoResponseDTO>> ObtenerPorIdAsync(int id)
    {
        var insumo = await _db.Insumos.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (insumo is null)
            return ServiceResult<InsumoResponseDTO>.NotFound($"No existe el insumo con id {id}.");

        return ServiceResult<InsumoResponseDTO>.Ok(ToDto(insumo));
    }

    public async Task<ServiceResult<InsumoResponseDTO>> ActualizarStockAsync(int id, ActualizarStockDTO dto)
    {
        var insumo = await _db.Insumos.FirstOrDefaultAsync(i => i.Id == id);
        if (insumo is null)
        {
            _logger.LogWarning("Intento de actualizar stock de insumo inexistente. Id: {InsumoId}", id);
            return ServiceResult<InsumoResponseDTO>.NotFound($"No existe el insumo con id {id}.");
        }

        var stockAnterior = insumo.StockActual;
        insumo.StockActual = dto.StockActual!.Value;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cambio de stock. Insumo: {InsumoId} ({Nombre}), Anterior: {Anterior}, Nuevo: {Nuevo}",
            insumo.Id, insumo.Nombre, stockAnterior, insumo.StockActual);

        if (insumo.StockActual <= insumo.StockMinimo)
        {
            _logger.LogWarning("Stock bajo. Insumo: {InsumoId} ({Nombre}), Stock: {Stock}, Mínimo: {Minimo}",
                insumo.Id, insumo.Nombre, insumo.StockActual, insumo.StockMinimo);
        }

        return ServiceResult<InsumoResponseDTO>.Ok(ToDto(insumo), "Stock actualizado correctamente.");
    }

    public async Task<ServiceResult<List<InsumoResponseDTO>>> ObtenerStockBajoAsync()
    {
        var insumos = await _db.Insumos.AsNoTracking()
            .Where(i => i.StockActual <= i.StockMinimo)
            .OrderBy(i => i.StockActual)
            .ToListAsync();

        _logger.LogInformation("Consulta de stock bajo: {Cantidad} insumos por debajo del mínimo.", insumos.Count);

        return ServiceResult<List<InsumoResponseDTO>>.Ok(insumos.Select(ToDto).ToList(),
            insumos.Count == 0 ? "No hay insumos con stock bajo." : $"{insumos.Count} insumos con stock bajo.");
    }

    private static InsumoResponseDTO ToDto(Insumo i) => new()
    {
        Id = i.Id,
        Nombre = i.Nombre,
        StockActual = i.StockActual,
        StockMinimo = i.StockMinimo,
        StockBajo = i.StockActual <= i.StockMinimo
    };
}
