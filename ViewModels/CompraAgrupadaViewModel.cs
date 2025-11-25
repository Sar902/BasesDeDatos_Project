using System;
using System.Collections.Generic;
using ProyectoSistemaInventarioNuevo.Models;

namespace ProyectoSistemaInventarioNuevo.ViewModels
{
    public class CompraAgrupadaViewModel
    {
        public DateTime Fecha { get; set; }
        public string NombreProveedor { get; set; }
        public decimal TotalCompra { get; set; } // La suma de todos los lotes
        public int CantidadLotes { get; set; } // Cuántos productos distintos vinieron
        
        // Aquí guardamos los detalles para poder desplegarlos si queremos
        public List<Inventario> Lotes { get; set; } = new List<Inventario>();
    }
}