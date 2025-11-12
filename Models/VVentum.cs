using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class VVentum
{
    public int IdVenta { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public decimal GananciaTotal { get; set; }
}
