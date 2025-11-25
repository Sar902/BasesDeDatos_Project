namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class CompraDetalleItemViewModel
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }

        public int Cantidad { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal Subtotal { get; set; } // Cantidad * PrecioCompra
    }
}