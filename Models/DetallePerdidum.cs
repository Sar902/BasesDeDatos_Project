using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class DetallePerdidum
{
    public int IdDetallePerdida { get; set; }

    public int IdPerdida { get; set; }

    public int IdProducto { get; set; }

    public int CantidadPerdida { get; set; }

    public decimal PrecioCompraUnitario { get; set; }

    public virtual Perdidum IdPerdidaNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
