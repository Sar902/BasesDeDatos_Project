using System;
using System;
using System.Collections.Generic;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class VSoltum
{
    public int IdSolicitudDevolucion { get; set; }
    
    public string Proveedor { get; set; }

     public string? Observaciones { get; set; }

    public DateTime Fecha { get; set; }

     public string Estado { get; set; }
}
