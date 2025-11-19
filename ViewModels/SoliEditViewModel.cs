using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class SoliEditViewModel
    {
        public int IdSolicitudDevolucion { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(200)]
        public string Observaciones { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Estado { get; set; } = string.Empty;
    }
}
