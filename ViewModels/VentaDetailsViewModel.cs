// Importamos los Modelos que vamos a usar
using ProyectoSistemaInventarioNuevo.Models;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class VentaDetailsViewModel
    {
        // Propiedad para la Venta (usamos la vista VVentum)
        public VVentum? Venta { get; set; }

        // Propiedad para la lista de productos (usamos la vista VDetalleVentum)
public List<VentaDetailItemViewModel> Detalles { get; set; } = new List<VentaDetailItemViewModel>();    }
}