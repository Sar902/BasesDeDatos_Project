
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels; 
public class PerdidaDetalleViewModel
{
    public int IdDetallePerdida { get; set; }

    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = "";

    public int CantidadPerdida { get; set; }

    public decimal PrecioCompraUnitario { get; set; }
    public decimal SubtotalPerdida { get; set; }
}
