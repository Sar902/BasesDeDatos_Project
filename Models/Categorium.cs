using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Categorium
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal PorcentajeGanancia { get; set; }

    public string Estado { get; set; } = null!;


    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
