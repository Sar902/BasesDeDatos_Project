using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public int Cantidad { get; set; }

    public string Estado { get; set; } = null!;


    public virtual ICollection<DetallePerdidum> DetallePerdida { get; set; } = new List<DetallePerdidum>();

    public virtual ICollection<DetalleSolicitudDevolucion> DetalleSolicitudDevolucion { get; set; } = new List<DetalleSolicitudDevolucion>();

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Categorium IdCategoriaNavigation { get; set; } = null!;

    public virtual ICollection<Inventario> Inventario { get; set; } = new List<Inventario>();

     
}
