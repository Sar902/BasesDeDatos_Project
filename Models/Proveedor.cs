using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class Proveedor
{
    public int IdProveedor { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Contacto { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();
}
