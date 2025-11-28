using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;
using System.Diagnostics;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SistemaInventarioFinalContext _context;

        public HomeController(ILogger<HomeController> logger, SistemaInventarioFinalContext context)
        {
            _logger = logger;
            _context = context;
        }

      public async Task<IActionResult> Index()
{
    var mesActual = DateTime.Now.Month;
    var añoActual = DateTime.Now.Year;

    // Productos
    var productos = await _context.Producto
        .Include(p => p.IdCategoriaNavigation)
        .Where(p => p.Estado == "Activo")
        .ToListAsync();

    // Productos con inventario bajo
    // Corrección: Consultar la tabla Producto directamente
    // Ya que 'RecalculateMasterStock' mantiene el campo 'Cantidad' actualizado con la suma total.
    var inventarioBajo = await _context.Producto
        .Include(p => p.IdCategoriaNavigation) // Opcional, si necesitas mostrar la categoría
        .Where(p => p.Cantidad < 10 
                && p.Estado == "Activo")
        .ToListAsync();

    // Ventas del mes
    var ventasMes = await _context.Venta
        .Where(v => v.Fecha.Month == mesActual && v.Fecha.Year == añoActual)
        .ToListAsync();

    DateTime inicioSemana = DateTime.Now.Date.AddDays(-7); 

    // 2. Calcular las Ventas de la Semana
    decimal totalVentasSemana = await _context.Venta
        .Where(v => v.Fecha >= inicioSemana) 
        .SumAsync(v => v.Total); 
     ViewBag.TotalVentasSemana = totalVentasSemana;

    // Detalle ventas
    var detalleVentas = await _context.DetalleVenta
        .Include(dv => dv.IdProductoNavigation)
        .Include(dv => dv.IdVentaNavigation)
        .ToListAsync();

    // Pérdidas del mes
    var perdidasMes = await _context.Perdida
        .Where(p => p.Fecha.Month == mesActual && p.Fecha.Year == añoActual)
        .ToListAsync();

    // Detalle pérdidas
    var detallePerdidas = await _context.DetallePerdida
        .Include(dp => dp.IdProductoNavigation)
        .Include(dp => dp.IdPerdidaNavigation)
        .ToListAsync();

    // Proveedores activos
    var proveedoresActivos = await _context.Proveedor
        .Where(p => p.Estado == "Activo")
        .ToListAsync();

    // Devoluciones
    var devoluciones = await _context.SolicitudDevolucion
        .Include(d => d.IdInventarioNavigation)
        .ToListAsync();

    // Detalle devoluciones
    var detalleDevoluciones = await _context.DetalleSolicitudDevolucion
        .Include(dd => dd.IdProductoNavigation)
        .Include(dd => dd.IdSolicitudNavigation)
        .ToListAsync();

    // Totales
    ViewBag.TotalProductos = productos.Count;
    ViewBag.TotalVentas = ventasMes.Sum(v => v.Total);
    ViewBag.PerdidasMes = perdidasMes.Count;
    ViewBag.ProveedoresActivosCount = proveedoresActivos.Count;

    // Datos para tablas
    ViewBag.Productos = productos;
    ViewBag.InventarioBajo = inventarioBajo;
    ViewBag.VentasMes = ventasMes;
    ViewBag.DetalleVentas = detalleVentas;
    ViewBag.Perdidas = perdidasMes;
    ViewBag.DetallePerdidas = detallePerdidas;
    ViewBag.ProveedoresActivos = proveedoresActivos;
    ViewBag.Devoluciones = devoluciones;
    ViewBag.DetalleDevoluciones = detalleDevoluciones;

    return View();
}

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
