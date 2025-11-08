using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class DetalleSolicitudDevolucion
{
    public int IdDetalleSolicitudDevolucion { get; set; }

    public int IdSolicitudDevolucion { get; set; }

    public int IdProducto { get; set; }

    public string? MotivoRechazo { get; set; }

    public int CantidadSolicitada { get; set; }

    public int? CantidadAceptada { get; set; }

    public decimal PrecioCompraUnitario { get; set; }

    public string EstadoItem { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual SolicitudDevolucion IdSolicitudDevolucionNavigation { get; set; } = null!;
}
