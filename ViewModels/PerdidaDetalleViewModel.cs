using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class PerdidaDetalleViewModel
    {
        // Para enviar al controlador
        [Required(ErrorMessage = "El producto es obligatorio.")]
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "La cantidad perdida es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int CantidadPerdida { get; set; }

        // Propiedades calculadas y opcionales (solo lectura/vista)
        public string? NombreProducto { get; set; }
        public decimal PrecioCompraUnitario { get; set; }
        public decimal SubtotalPerdida => CantidadPerdida * PrecioCompraUnitario;

        // ⚠ Propiedades necesarias para la tabla y los botones de Edit/Delete
        public int IdPerdida { get; set; }           // Para identificar la pérdida completa
        public int IdDetallePerdida { get; set; }    // Para editar o eliminar este detalle
    }
}
