using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels;

public class PerdidaDetalleViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un producto.")]
    [Display(Name = "Producto")]
    public int IdProducto { get; set; }

    public string NombreProducto { get; set; } = ""; // <--- agregar esto

    [Required(ErrorMessage = "Ingrese la cantidad perdida.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    [Display(Name = "Cantidad perdida")]
    public int CantidadPerdida { get; set; }

    public decimal PrecioCompraUnitario { get; set; } 
    public decimal SubtotalPerdida { get; set; }    
    public int IdDetallePerdida { get; set; } 
}

