using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public int Cantidad { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<DetallePerdidum> DetallePerdida { get; set; } = new List<DetallePerdidum>();

    public virtual ICollection<DetalleSolicitudDevolucion> DetalleSolicitudDevolucions { get; set; } = new List<DetalleSolicitudDevolucion>();

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Categorium IdCategoriaNavigation { get; set; } = null!;

    public virtual ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();
}
