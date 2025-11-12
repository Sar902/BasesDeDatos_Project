using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class SolicitudDevolucion
{
    public int IdSolicitudDevolucion { get; set; }

    public int IdInventario { get; set; }

    public string Estado { get; set; } = null!;

    public string? Observaciones { get; set; }

    public DateTime Fecha { get; set; }

    public virtual ICollection<DetalleSolicitudDevolucion> DetalleSolicitudDevolucion { get; set; } = new List<DetalleSolicitudDevolucion>();

    public virtual Inventario IdInventarioNavigation { get; set; } = null!;
}
