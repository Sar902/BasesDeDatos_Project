using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class VentaViewModel
    {
        public VentaViewModel()
        {
            // Inicializamos la lista para que no esté nula
            Items = new List<DetalleVentaViewModel>();
        }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now; // Poner la fecha actual por defecto

        public decimal Total { get; set; }

        // La lista de productos que se están vendiendo
        public List<DetalleVentaViewModel> Items { get; set; }
    }
}