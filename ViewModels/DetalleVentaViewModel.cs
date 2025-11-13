namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class DetalleVentaViewModel
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;        
        public int Cantidad { get; set; }
        public decimal PrecioVentaUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}