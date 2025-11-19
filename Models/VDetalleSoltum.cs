using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class VDetalleSoltum
{
    public int IdDetalleSolicitudDevolucion { get; set; }

    public int IdSolicitudDevolucion { get; set; }

    public int IdProducto { get; set; }

    public int CantidadSolicitada { get; set; }

    public decimal PrecioCompraUnitario { get; set; }

    public string EstadoItem { get; set; }
}
