using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DetallePerdidaViewModel
{
    public int IdDetallePerdida { get; set; }
    public int IdProducto { get; set; }

    public string NombreProducto { get; set; }

    [Display(Name = "Cantidad Pérdida")]
    public int CantidadPerdida { get; set; }

    public decimal PrecioCompraUnitario { get; set; }
    public decimal SubtotalPerdida => CantidadPerdida * PrecioCompraUnitario;
}
