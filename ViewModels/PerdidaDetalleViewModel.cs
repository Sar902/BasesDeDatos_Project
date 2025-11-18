using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels; // Asegúrate de crear esta carpeta

public class PerdidaDetalleViewModel
{
    // --- Campos de DetallePerdidum ---
    
    [Required(ErrorMessage = "Debe seleccionar un producto.")]
    [Display(Name = "Producto")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "Ingrese la cantidad perdida.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    [Display(Name = "Cantidad Pérdida")]
    public int CantidadPerdida { get; set; }

    // El PrecioCompraUnitario lo obtendremos del Inventario, no se pide al usuario.

    // --- Campos de Perdidum (Encabezado) ---
    
    [Display(Name = "Motivo de la Pérdida")]
    [Required(ErrorMessage = "Debe especificar el motivo de la pérdida.")]
    [StringLength(255, ErrorMessage = "El motivo no puede exceder los 255 caracteres.")]
    public string? Motivo { get; set; } 
    
    // Total y Fecha se calculan en el servidor.
}