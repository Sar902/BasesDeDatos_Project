using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Inventario
{
    public int IdInventario { get; set; }

    public int IdProducto { get; set; }

    public int? IdProveedor { get; set; }

    public int Cantidad { get; set; }

    public int CantidadDisponible { get; set; }

    public decimal PrecioCompra { get; set; }

    public DateTime FechaEntrada { get; set; }

    public DateTime? FechaSalida { get; set; }

    public string? Estado { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }

    public virtual Proveedor? IdProveedorNavigation { get; set; }

    public virtual ICollection<SolicitudDevolucion> SolicitudDevolucion { get; set; } = new List<SolicitudDevolucion>();
}
