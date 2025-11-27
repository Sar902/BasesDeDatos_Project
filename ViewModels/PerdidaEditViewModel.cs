using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class PerdidaEditViewModel
    {
        public int IdPerdida { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(200)]
        public string Motivo { get; set; } = string.Empty;

        // Propiedad agregada:
        public decimal Total { get; set; } 
    }
}