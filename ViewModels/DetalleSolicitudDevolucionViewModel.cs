namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class DetalleSolicitudDevolucionViewModel
    {
        public int IdProducto { get; set; }
        public int CantidadSolicitada { get; set; }
        public decimal PrecioCompraUnitario { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int IdInventario { get; set; } // Para validar que todos sean iguales
    }
}
