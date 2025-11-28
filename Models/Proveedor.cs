using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Necesario para las validaciones

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class Proveedor
{
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "El nombre de la empresa o proveedor es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    [Display(Name = "Nombre del Proveedor")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El contacto es obligatorio.")]
    [StringLength(50, ErrorMessage = "El contacto no puede exceder los 50 caracteres.")]
    [Display(Name = "Teléfono / Contacto")]
    public string? Contacto { get; set; }

    // El estado no requiere validación del usuario porque lo manejamos nosotros internamente
    public string? Estado { get; set; }

    public virtual ICollection<Inventario> Inventario { get; set; } = new List<Inventario>();
} 