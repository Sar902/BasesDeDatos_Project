// Importamos los Modelos que vamos a usar
using ProyectoSistemaInventarioNuevo.Models;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class SoliDetailsViewModel
    {
        // Propiedad para la soli (usamos la vista VSoltum)
        public VSoltum? Soli { get; set; }

        // Propiedad para la lista de productos (usamos la vista VDetalleSoltum)
public List<SoliDetailItemViewModel> Detalles { get; set; } = new List<SoliDetailItemViewModel>();    }
}