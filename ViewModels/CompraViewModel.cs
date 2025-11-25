using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class CompraViewModel
    {
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Now;

        public decimal TotalCompra { get; set; } // Suma de subtotales

        // Lista de lotes a ingresar
        public List<CompraDetalleItemViewModel>? Items { get; set; } = new List<CompraDetalleItemViewModel>();
    }
}