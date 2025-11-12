using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class VProducto
{
    public int IdProducto { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdCategoria { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal? PrecioVenta { get; set; }

    public int Cantidad { get; set; }

    public string Estado { get; set; } = null!;
}
