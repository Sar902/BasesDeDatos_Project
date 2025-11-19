namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    // Esta clase guardará 1 línea del detalle
    public class SoliDetailItemViewModel
    {
        // Propiedades que vienen de VDetalleSoltum
        public int CantidadSolicitada { get; set; }
        public decimal PrecioCompraUnitario { get; set; }
        public string EstadoItem { get; set; }
        
        // La propiedad que nos faltaba
        public string NombreProducto { get; set; } = string.Empty;
    }
}