using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class DetalleVentum
{
    public int IdDetalleVenta { get; set; }

    public int IdVenta { get; set; }

    public int IdProducto { get; set; }

    public int CantidadVendida { get; set; }

    public decimal PrecioVentaUnitario { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Ventum IdVentaNavigation { get; set; } = null!;
}
