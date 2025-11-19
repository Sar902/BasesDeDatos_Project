
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels; 

public class PerdidaCreateViewModel
{

    
    public int IdPerdida { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    [Required]
    public string Motivo { get; set; }

    public decimal Total { get; set; }
    public List<PerdidaDetalleViewModel> Items { get; set; } = new List<PerdidaDetalleViewModel>();
}

