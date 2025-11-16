using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    [Display(Name = "Categoría")]
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public int Cantidad { get; set; }

    public string Estado { get; set; } = null!;




    public virtual ICollection<DetallePerdidum> DetallePerdida { get; set; } = new List<DetallePerdidum>();

    public virtual ICollection<DetalleSolicitudDevolucion> DetalleSolicitudDevolucion { get; set; } = new List<DetalleSolicitudDevolucion>();

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public Categorium? IdCategoriaNavigation { get; set; }

    
    public virtual ICollection<Inventario> Inventario { get; set; } = new List<Inventario>();

     
}
