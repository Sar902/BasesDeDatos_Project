using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class VDetallePerdidum
{
    public int IdDetallePerdida { get; set; }

    public int IdPerdida { get; set; }

    public int IdProducto { get; set; }

    public int CantidadPerdida { get; set; }

    public decimal PrecioCompraUnitario { get; set; }

    public decimal? SubtotalPerdida { get; set; }
}
