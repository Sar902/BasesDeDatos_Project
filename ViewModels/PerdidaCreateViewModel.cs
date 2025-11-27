using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoSistemaInventarioNuevo.ViewModels; 

public class PerdidaCreateViewModel
{
    public int IdPerdida { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "El motivo de la pérdida es obligatorio.")]
    [StringLength(250, ErrorMessage = "El motivo no puede exceder los 250 caracteres.")]
    public string Motivo { get; set; } = "";
    

    // ¡La clave!
    [Required(ErrorMessage = "Debe agregar al menos un producto perdido.")]
    [MinLength(1, ErrorMessage = "Debe agregar al menos un producto perdido.")]
    public List<PerdidaDetalleViewModel> Items { get; set; } = new List<PerdidaDetalleViewModel>();
      // Este campo se llena en el servidor antes de guardar
    public decimal Total 
    { 
        get
        {
            return Items?.Sum(item => item.SubtotalPerdida) ?? 0;
        }
    }
}