using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class DetallePerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public DetallePerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // ===============================
        //       Helper HTMX
        // ===============================
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        private async Task PopulateProductos(DetallePerdidum detalle = null)
        {
            var productos = await _context.Producto
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.IdProducto, p.Nombre })
                .ToListAsync();

            ViewBag.Productos = new SelectList(productos, "IdProducto", "Nombre", detalle?.IdProducto);
        }

        // ===============================
        //        Recalcular Stock
        // ===============================
        private async Task RecalculateProductoStock(int idProducto)
        {
            var producto = await _context.Producto.FindAsync(idProducto);
            if (producto == null) return;

            var totalPerdido = await _context.DetallePerdida
                .Where(d => d.IdProducto == idProducto)
                .SumAsync(d => d.CantidadPerdida);

            // Calculamos stock actual
            producto.Cantidad = Math.Max(producto.Cantidad - totalPerdido, 0);
            producto.Estado = (producto.Cantidad > 0) ? "Activo" : "Agotado";

            _context.Update(producto);
            await _context.SaveChangesAsync();
        }

        // ===============================
        //            INDEX
        // ===============================
        public async Task<IActionResult> Index()
        {
            return View(); // HTMX cargará la tabla con GetDetallePerdidaList
        }
[HttpGet]
public async Task<IActionResult> GetDetallePerdidaList()
{
    var detalles = await _context.DetallePerdida
        .Include(d => d.IdProductoNavigation)
        .Include(d => d.IdPerdidaNavigation)
        .OrderByDescending(d => d.IdDetallePerdida)
        .ToListAsync();

    return PartialView("_DetallePerdidaList", detalles);
}


        // ===============================
        //           DETAILS
        // ===============================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var detalle = await _context.DetallePerdida
                .Include(d => d.IdProductoNavigation)
                .Include(d => d.IdPerdidaNavigation)
                .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);

            if (detalle == null) return NotFound();

            return IsHtmxRequest()
                ? PartialView("Details", detalle)
                : View(detalle);
        }

        // ===============================
        //            CREATE
        // ===============================        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DetallePerdidum detalle, DateTime fecha)
        {
            await PopulateProductos(detalle);

            if (!ModelState.IsValid)
                return PartialView("Create", detalle);

            var producto = await _context.Producto.FindAsync(detalle.IdProducto);
            if (producto == null)
            {
                ModelState.AddModelError("", "Producto no encontrado.");
                return PartialView("Create", detalle);
            }

            if (detalle.CantidadPerdida > producto.Cantidad)
            {
                ModelState.AddModelError("", "La cantidad a perder no puede ser mayor que la disponible.");
                return PartialView("Create", detalle);
            }

            var perdida = new Perdidum { Fecha = fecha };
            _context.Perdida.Add(perdida);
            await _context.SaveChangesAsync();

            detalle.IdPerdida = perdida.IdPerdida;
            detalle.SubtotalPerdida = detalle.CantidadPerdida * detalle.PrecioCompraUnitario;

            _context.DetallePerdida.Add(detalle);
            await _context.SaveChangesAsync();

            producto.Cantidad -= detalle.CantidadPerdida;
            producto.Estado = producto.Cantidad > 0 ? "Activo" : "Agotado";
            _context.Update(producto);
            await _context.SaveChangesAsync();

            Response.Headers["HX-Trigger"] = "htmx:closeModal, refreshDetallePerdidaList";
            return Content("", "text/html");
        }


        //EDIT
     
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var detalle = await _context.DetallePerdida.FindAsync(id);
            if (detalle == null) return NotFound();

            await PopulateProductos(detalle);

            return IsHtmxRequest()
                ? PartialView("Edit", detalle)
                : View(detalle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DetallePerdidum detalle)
        {
            if (id != detalle.IdDetallePerdida) return NotFound();

            await PopulateProductos(detalle);

            if (!ModelState.IsValid)
                return PartialView("Edit", detalle);

            var detalleOriginal = await _context.DetallePerdida.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);

            if (detalleOriginal == null) return NotFound();

            // Validación: cantidad no mayor que disponible
            var producto = await _context.Producto.FindAsync(detalle.IdProducto);
            if (detalle.CantidadPerdida > producto.Cantidad + detalleOriginal.CantidadPerdida)
            {
                ModelState.AddModelError("", "La cantidad a perder no puede ser mayor que la disponible.");
                return PartialView("Edit", detalle);
            }

            detalle.SubtotalPerdida = detalle.CantidadPerdida * detalle.PrecioCompraUnitario;

            _context.Update(detalle);
            await _context.SaveChangesAsync();

            await RecalculateProductoStock(detalle.IdProducto);
         Response.Headers["HX-Trigger"] = "htmx:closeModal, refreshDetallePerdidaList";
            return Content("", "text/html");
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var detalle = await _context.DetallePerdida
                .Include(d => d.IdProductoNavigation)
                .Include(d => d.IdPerdidaNavigation)
                .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);

            if (detalle == null) return NotFound();

            return IsHtmxRequest()
                ? PartialView("Delete", detalle)
                : View(detalle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalle = await _context.DetallePerdida.FindAsync(id);
            if (detalle == null) return NotFound();

            _context.DetallePerdida.Remove(detalle);
            await _context.SaveChangesAsync();

            await RecalculateProductoStock(detalle.IdProducto);

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Trigger"] = "htmx:closeModal, refreshDetallePerdidaList";
                return Content("", "text/html");
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DetallePerdidaExists(int id)
        {
            return _context.DetallePerdida.Any(e => e.IdDetallePerdida == id);
        }
    }
}
