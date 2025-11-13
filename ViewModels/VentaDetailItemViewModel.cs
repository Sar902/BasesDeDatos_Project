namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    // Esta clase guardará 1 línea del detalle
    public class VentaDetailItemViewModel
    {
        // Propiedades que vienen de VDetalleVentum
        public int CantidadVendida { get; set; }
        public decimal PrecioVentaUnitario { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? GananciaSubtotal { get; set; }
        
        // La propiedad que nos faltaba
        public string NombreProducto { get; set; } = string.Empty;
    }
}