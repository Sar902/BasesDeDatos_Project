using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Categorium
{
    public int IdCategoria { get; set; }

[Required(ErrorMessage = "El nombre es obligatorio")]
public string Nombre { get; set; } = null!;

[Required(ErrorMessage = "El porcentaje de ganancia es obligatorio")]
[Display(Name = "Porcentaje de Ganancia")]
public decimal PorcentajeGanancia { get; set; }



  public string Estado { get; set; } = "Activo";



    public virtual ICollection<Producto> Producto { get; set; } = new List<Producto>();
}
