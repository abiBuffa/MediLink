using MediLink.Clients;
using MediLink.Common;
using MediLink.Data;
using MediLink.DTOs;
using MediLink.Interfaces;
using MediLink.Models;
using Microsoft.EntityFrameworkCore;

namespace MediLink.Services;

public interface IReposicionService
{
    Task<ServiceResult<PedidoReposicionResponseDTO>> CrearAsync(PedidoReposicionDTO dto, CancellationToken ct = default);
    Task<ServiceResult<List<PedidoReposicionResponseDTO>>> ObtenerTodosAsync();
    Task<ServiceResult<PedidoReposicionResponseDTO>> ObtenerPorIdAsync(int id);
}

public class ReposicionService : IReposicionService
{
    private readonly MediLinkDbContext _db;
    private readonly IFarmaciaClient _farmacia;
    private readonly ILogger<ReposicionService> _logger;

    public ReposicionService(MediLinkDbContext db, IFarmaciaClient farmacia, ILogger<ReposicionService> logger)
    {
        _db = db;
        _farmacia = farmacia;
        _logger = logger;
    }

    public async Task<ServiceResult<PedidoReposicionResponseDTO>> CrearAsync(PedidoReposicionDTO dto, CancellationToken ct = default)
    {
        // 1) Validar que el insumo exista
        var insumo = await _db.Insumos.FirstOrDefaultAsync(i => i.Id == dto.InsumoId, ct);
        if (insumo is null)
        {
            _logger.LogWarning("Pedido de reposición rechazado: insumo inexistente. InsumoId: {InsumoId}", dto.InsumoId);
            return ServiceResult<PedidoReposicionResponseDTO>.NotFound($"No existe el insumo con id {dto.InsumoId}.");
        }

        // 2) Guardar el pedido como Pendiente ANTES de llamar afuera,
        //    así queda registrado aunque PharmaLink falle.
        var pedido = new PedidoReposicion
        {
            InsumoId = insumo.Id,
            Insumo = insumo,
            Cantidad = dto.Cantidad!.Value,
            FechaPedido = DateTime.UtcNow,
            Estado = EstadoPedido.Pendiente
        };
        _db.PedidosReposicion.Add(pedido);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Pedido de reposición {PedidoId} creado (Pendiente). Insumo: {InsumoId} ({Nombre}), Cantidad: {Cantidad}",
            pedido.Id, insumo.Id, insumo.Nombre, pedido.Cantidad);

        // 3) Llamar a PharmaLink con manejo de fallas
        try
        {
            var respuesta = await _farmacia.SolicitarInsumosAsync(insumo.Nombre, pedido.Cantidad);

            pedido.FechaRespuesta = DateTime.UtcNow;
            pedido.RespuestaFarmacia = respuesta.Mensaje;

            if (respuesta.Confirmado)
            {
                pedido.Estado = EstadoPedido.Enviado;
                pedido.CodigoEnvio = respuesta.CodigoEnvio;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Pedido {PedidoId} aceptado por PharmaLink. Código de envío: {CodigoEnvio}",
                    pedido.Id, pedido.CodigoEnvio);

                return ServiceResult<PedidoReposicionResponseDTO>.Created(ToDto(pedido),
                    "Pedido de reposición enviado a la farmacia.");
            }

            pedido.Estado = EstadoPedido.Rechazado;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("Pedido {PedidoId} rechazado por PharmaLink. Motivo: {Motivo}", pedido.Id, respuesta.Mensaje);

            return ServiceResult<PedidoReposicionResponseDTO>.Created(ToDto(pedido),
                $"El pedido se registró pero la farmacia lo rechazó: {respuesta.Mensaje}");
        }
        catch (Exception ex) when (ex is FarmaciaNoDisponibleException
                                       or HttpRequestException
                                       or TaskCanceledException
                                       or TimeoutException)
        {
            // Resiliencia: PharmaLink caída / timeout -> no se rompe el sistema.
            _logger.LogError(ex, "PharmaLink no disponible al solicitar insumos. Pedido {PedidoId} marcado como Fallido.", pedido.Id);

            pedido.Estado = EstadoPedido.Fallido;
            pedido.FechaRespuesta = DateTime.UtcNow;
            pedido.RespuestaFarmacia = "Farmacia no disponible: " + ex.Message;
            await _db.SaveChangesAsync(CancellationToken.None);

            return ServiceResult<PedidoReposicionResponseDTO>.ServiceUnavailable(
                "La farmacia no está disponible en este momento. El pedido quedó registrado como Fallido.",
                ToDto(pedido));
        }
    }

    public async Task<ServiceResult<List<PedidoReposicionResponseDTO>>> ObtenerTodosAsync()
    {
        var pedidos = await _db.PedidosReposicion.AsNoTracking()
            .Include(p => p.Insumo)
            .OrderByDescending(p => p.FechaPedido)
            .ToListAsync();

        return ServiceResult<List<PedidoReposicionResponseDTO>>.Ok(pedidos.Select(ToDto).ToList(),
            $"Se encontraron {pedidos.Count} pedidos de reposición.");
    }

    public async Task<ServiceResult<PedidoReposicionResponseDTO>> ObtenerPorIdAsync(int id)
    {
        var pedido = await _db.PedidosReposicion.AsNoTracking()
            .Include(p => p.Insumo)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido is null)
            return ServiceResult<PedidoReposicionResponseDTO>.NotFound($"No existe el pedido de reposición con id {id}.");

        return ServiceResult<PedidoReposicionResponseDTO>.Ok(ToDto(pedido));
    }

    private static PedidoReposicionResponseDTO ToDto(PedidoReposicion p) => new()
    {
        Id = p.Id,
        InsumoId = p.InsumoId,
        NombreInsumo = p.Insumo?.Nombre ?? string.Empty,
        Cantidad = p.Cantidad,
        FechaPedido = p.FechaPedido,
        Estado = p.Estado.ToString(),
        CodigoEnvio = p.CodigoEnvio,
        RespuestaFarmacia = p.RespuestaFarmacia,
        FechaRespuesta = p.FechaRespuesta
    };
}
