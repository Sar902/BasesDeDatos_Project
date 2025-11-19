using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Perdidum
{
    public int IdPerdida { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public string? Motivo { get; set; }


    public virtual ICollection<DetallePerdidum> DetallePerdida { get; set; } = new List<DetallePerdidum>();
}