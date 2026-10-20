using System.ComponentModel.DataAnnotations;

namespace MediLink.DTOs;

/// <summary>Datos para crear un pedido de reposición (POST /api/reposiciones).</summary>
public class PedidoReposicionDTO
{
    [Required(ErrorMessage = "El insumo es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "El id del insumo debe ser mayor a cero.")]
    public int? InsumoId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int? Cantidad { get; set; }
}

/// <summary>Lo que la API devuelve de un pedido de reposición.</summary>
public class PedidoReposicionResponseDTO
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public string NombreInsumo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaPedido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? CodigoEnvio { get; set; }
    public string? RespuestaFarmacia { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}
