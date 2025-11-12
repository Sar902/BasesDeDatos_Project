using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class VDetalleVentum
{
    public int IdDetalleVenta { get; set; }

    public int IdVenta { get; set; }

    public int IdProducto { get; set; }

    public int CantidadVendida { get; set; }

    public decimal PrecioVentaUnitario { get; set; }

    public decimal PrecioCompraUnitario { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? GananciaSubtotal { get; set; }
}
