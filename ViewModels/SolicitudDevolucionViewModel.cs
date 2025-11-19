using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class SolicitudDevolucionViewModel
    {
        public int IdSolicitudDevolucion { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Debe ingresar una observación.")]
        [StringLength(200)]
        public string Observaciones { get; set; } = string.Empty;

        public int IdInventario { get; set; } // Este lo asignaremos según los productos

        public List<DetalleSolicitudDevolucionViewModel> Items { get; set; } = new();
        

    }
}
