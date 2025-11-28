using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class PerdidaRowViewModel
    {
        public int IdPerdida { get; set; }
        public DateTime Fecha { get; set; }
        public string Motivo { get; set; }
        public decimal TotalPerdida { get; set; }
    }
}