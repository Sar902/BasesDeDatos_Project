using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Proveedor
{
    public int IdProveedor { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Contacto { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<Inventario> Inventario { get; set; } = new List<Inventario>();
    
}
