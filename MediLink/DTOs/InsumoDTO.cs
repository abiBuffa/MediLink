using System.ComponentModel.DataAnnotations;

namespace MediLink.DTOs;

/// <summary>Datos para dar de alta un insumo (POST /api/insumos).</summary>
public class InsumoDTO
{
    [Required(ErrorMessage = "El nombre del insumo es obligatorio.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "El stock actual no puede ser negativo.")]
    public int StockActual { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public int StockMinimo { get; set; }
}

/// <summary>Datos para actualizar el stock (PUT /api/insumos/{id}/stock).</summary>
public class ActualizarStockDTO
{
    [Required(ErrorMessage = "El stock actual es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock actual no puede ser negativo.")]
    public int? StockActual { get; set; }
}

/// <summary>Lo que la API devuelve de un insumo.</summary>
public class InsumoResponseDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public bool StockBajo { get; set; }
}
