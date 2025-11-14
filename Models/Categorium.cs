using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Categorium
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    [Display(Name = "Porcentaje de Ganancia")]
    public decimal PorcentajeGanancia { get; set; }

    public string Estado { get; set; } = null!;


    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
