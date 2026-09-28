namespace MediLink.Models;

public class PedidoReposicion
{
    public int Id { get; set; }
    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    public int Cantidad { get; set; }
    public DateTime FechaPedido { get; set; }
    public EstadoPedido Estado { get; set; }
    public string? CodigoEnvio { get; set; }
    public string? RespuestaFarmacia { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}