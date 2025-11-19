using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;
using ProyectoSistemaInventarioNuevo.ViewModels;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class DetallePerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public DetallePerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // ===============================
        // Traer productos activos
        // ===============================
        private async Task PopulateProductos(int? idProducto = null)
        {
            var productos = await _context.Producto
                .Where(p => p.Estado == "Activo")
                .Join(_context.Inventario,
                      p => p.IdProducto,
                      i => i.IdProducto,
                      (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
                .Where(x => x.CantidadDisponible > 0)
                .OrderBy(x => x.Nombre)
                .ToListAsync();

            var productosUnicos = productos
                .GroupBy(x => x.IdProducto)
                .Select(g => new { g.Key, g.First().Nombre, CantidadDisponible = g.Sum(p => p.CantidadDisponible) })
                .ToList();

            ViewBag.Productos = new SelectList(productosUnicos, "Key", "Nombre", idProducto);
            ViewBag.ProductosConCantidad = productosUnicos.ToDictionary(x => x.Key, x => x.CantidadDisponible);
        }

        // ===============================
        // Ajustar inventario
        // ===============================
        private async Task AjustarInventario(int idProducto, int cantidadDelta)
        {
            var producto = await _context.Producto.FindAsync(idProducto);
            var inventario = await _context.Inventario.FirstOrDefaultAsync(i => i.IdProducto == idProducto);

            if (producto == null || inventario == null) return;

            producto.Cantidad -= cantidadDelta;
            inventario.CantidadDisponible -= cantidadDelta;

            producto.Estado = producto.Cantidad > 0 ? "Activo" : "Agotado";
            inventario.Estado = inventario.CantidadDisponible > 0 ? "Activo" : "Agotado";

            _context.Producto.Update(producto);
            _context.Inventario.Update(inventario);

            await _context.SaveChangesAsync();
        }

        // ===============================
        // Index
        // ===============================
        public IActionResult Index() => View();

        // ===============================
        // Create
        // ===============================
     // GET: DetallePerdida/Create
public async Task<IActionResult> Create(int idPerdida)
{
    // Cargamos productos activos con cantidad disponible
    var productos = await _context.Producto
        .Join(_context.Inventario,
              p => p.IdProducto,
              i => i.IdProducto,
              (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
        .Where(x => x.CantidadDisponible > 0)
        .OrderBy(x => x.Nombre)
        .ToListAsync();

    ViewBag.Productos = productos;
    ViewBag.IdPerdida = idPerdida;

    // Siempre enviamos un VM inicializado
    var vm = new PerdidaDetalleViewModel();
    return PartialView("Create", vm);
}

// POST: DetallePerdida/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(int idPerdida, PerdidaDetalleViewModel vm)
{
    if (!ModelState.IsValid)
    {
        // Re-cargar productos si hubo error
        var productos = await _context.Producto
            .Join(_context.Inventario,
                  p => p.IdProducto,
                  i => i.IdProducto,
                  (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
            .Where(x => x.CantidadDisponible > 0)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        ViewBag.Productos = productos;
        ViewBag.IdPerdida = idPerdida;
        return PartialView("Create", vm);
    }

    // Validación de inventario
    var inventario = await _context.Inventario.FirstOrDefaultAsync(i => i.IdProducto == vm.IdProducto);
    if (inventario == null || inventario.CantidadDisponible < vm.CantidadPerdida)
    {
        ModelState.AddModelError("", "No hay inventario suficiente para este producto.");
        var productos = await _context.Producto
            .Join(_context.Inventario,
                  p => p.IdProducto,
                  i => i.IdProducto,
                  (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
            .Where(x => x.CantidadDisponible > 0)
            .OrderBy(x => x.Nombre)
            .ToListAsync();
        ViewBag.Productos = productos;
        ViewBag.IdPerdida = idPerdida;
        return PartialView("Create", vm);
    }

    // Guardamos detalle
    var detalle = new DetallePerdidum
    {
        IdPerdida = idPerdida,
        IdProducto = vm.IdProducto,
        CantidadPerdida = vm.CantidadPerdida,
        PrecioCompraUnitario = inventario.PrecioCompra,
        SubtotalPerdida = vm.CantidadPerdida * inventario.PrecioCompra
    };
    _context.DetallePerdida.Add(detalle);

    // Ajustamos inventario
    inventario.CantidadDisponible -= vm.CantidadPerdida;
    _context.Inventario.Update(inventario);

    // Actualizamos total de la pérdida
    var perdida = await _context.Perdida.FindAsync(idPerdida);
    if (perdida != null)
    {
        perdida.Total = await _context.DetallePerdida
            .Where(d => d.IdPerdida == idPerdida)
            .SumAsync(d => d.SubtotalPerdida);
        _context.Perdida.Update(perdida);
    }

    await _context.SaveChangesAsync();

    // Trigger para HTMX: cerrar modal y refrescar lista
    Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
    return Content("");
}

        // ===============================
        // Edit
        // ===============================
        public async Task<IActionResult> Edit(int id)
        {
            var detalle = await _context.DetallePerdida.FindAsync(id);
            if (detalle == null) return NotFound();

            await PopulateProductos(detalle.IdProducto);

         var vm = new PerdidaDetalleViewModel
       {
          IdProducto = detalle.IdProducto,
          CantidadPerdida = detalle.CantidadPerdida
      };

         ViewBag.Motivo = detalle.IdPerdidaNavigation?.Motivo;


            ViewBag.IdDetalle = id;
            ViewBag.IdPerdida = detalle.IdPerdida;
            return PartialView("Edit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int idPerdida, PerdidaDetalleViewModel vm)
        {
            var detalle = await _context.DetallePerdida.FindAsync(id);
            if (detalle == null) return NotFound();

            var inventario = await _context.Inventario.FirstOrDefaultAsync(i => i.IdProducto == vm.IdProducto);
            if (inventario == null || inventario.CantidadDisponible + detalle.CantidadPerdida < vm.CantidadPerdida)
            {
                ModelState.AddModelError("", "No hay inventario suficiente para actualizar la pérdida.");
                await PopulateProductos(vm.IdProducto);
                return PartialView("Edit", vm);
            }

            int delta = vm.CantidadPerdida - detalle.CantidadPerdida;
            detalle.IdProducto = vm.IdProducto;
            detalle.CantidadPerdida = vm.CantidadPerdida;
            detalle.PrecioCompraUnitario = inventario.PrecioCompra;
            detalle.SubtotalPerdida = vm.CantidadPerdida * inventario.PrecioCompra;

            await AjustarInventario(vm.IdProducto, delta);

            _context.DetallePerdida.Update(detalle);

            var perdida = await _context.Perdida.FindAsync(idPerdida);
            if (perdida != null)
            {
                perdida.Total = await _context.DetallePerdida
                    .Where(d => d.IdPerdida == idPerdida)
                    .SumAsync(d => d.SubtotalPerdida);
                _context.Perdida.Update(perdida);
            }

            await _context.SaveChangesAsync();

            Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
            return Content("");
        }

        // ===============================
        // Delete
        // ===============================
        public async Task<IActionResult> Delete(int id)
        {
            var detalle = await _context.DetallePerdida
                .Include(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);
            if (detalle == null) return NotFound();

            ViewBag.IdPerdida = detalle.IdPerdida;
            return PartialView("Delete", detalle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalle = await _context.DetallePerdida.FindAsync(id);
            if (detalle == null) return NotFound();

            int idProducto = detalle.IdProducto;
            int idPerdida = detalle.IdPerdida;

            _context.DetallePerdida.Remove(detalle);
            await _context.SaveChangesAsync();

            await AjustarInventario(idProducto, -detalle.CantidadPerdida);

            var perdida = await _context.Perdida.FindAsync(idPerdida);
            if (perdida != null)
            {
                perdida.Total = await _context.DetallePerdida
                    .Where(d => d.IdPerdida == idPerdida)
                    .SumAsync(d => d.SubtotalPerdida);
                _context.Perdida.Update(perdida);
                await _context.SaveChangesAsync();
            }

            Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
            return Content("");
        }
    }
}
