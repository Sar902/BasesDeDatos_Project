using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class Ventum
{
    public int IdVenta { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();
}
