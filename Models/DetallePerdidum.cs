using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class DetallePerdidum
{
    public int IdDetallePerdida { get; set; }

    public int IdPerdida { get; set; }


   [Display(Name = "Producto")]
    public int IdProducto { get; set; }

   [Display(Name = "Cantidad Pérdida")]
    public int CantidadPerdida { get; set; }

   [Display(Name = "Precio Compra Unitario")]
    public decimal PrecioCompraUnitario { get; set; }

   [Display(Name = "Subtotal Pérdida")]
   public decimal SubtotalPerdida { get; set; } 

    public virtual Perdidum IdPerdidaNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
