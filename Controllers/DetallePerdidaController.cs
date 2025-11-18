using System;
using System.Collections.Generic;
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
            return View(); 
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

            // SI ES HTMX (Modal), devolvemos Partial. SI NO, devolvemos View completa.
            return IsHtmxRequest()
                ? PartialView("Details", detalle)
                : View(detalle);
        }

        // ===============================
        //            CREATE
        // ===============================
        public async Task<IActionResult> Create()
        {
            await PopulateProductos();
            return PartialView("Create");
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PerdidaDetalleViewModel vm)
{
    if (!ModelState.IsValid)
        return PartialView("Create", vm);

    // --- Buscar inventario disponible ---
    var inventario = await _context.Inventario
        .FirstOrDefaultAsync(i => i.IdProducto == vm.IdProducto && i.CantidadDisponible >= vm.CantidadPerdida);

    if (inventario == null)
    {
        ModelState.AddModelError("", "No hay inventario suficiente para este producto.");
        return PartialView("Create", vm);
    }

    // --- Crear nueva Pérdida si no hay ninguna abierta ---
    var nuevaPerdida = new Perdidum
    {
        Fecha = DateTime.Now,
        Motivo = vm.Motivo
    };
    _context.Perdida.Add(nuevaPerdida);
    await _context.SaveChangesAsync(); // Necesitamos el IdPerdida

    // --- Crear detalle ---
    var detalle = new DetallePerdidum
    {
        IdPerdida = nuevaPerdida.IdPerdida,
        IdProducto = vm.IdProducto,
        CantidadPerdida = vm.CantidadPerdida,
        PrecioCompraUnitario = inventario.PrecioCompra
        // No tocar SubtotalPerdida
    };
    _context.DetallePerdida.Add(detalle);

    // --- Aplicar pérdida al inventario ---
    inventario.CantidadDisponible -= vm.CantidadPerdida;
    if (inventario.CantidadDisponible == 0)
        inventario.Estado = "Agotado";
    _context.Inventario.Update(inventario);

    await _context.SaveChangesAsync();

    // --- Recalcular total de la pérdida ---
    nuevaPerdida.Total = await _context.DetallePerdida
        .Where(d => d.IdPerdida == nuevaPerdida.IdPerdida)
        .SumAsync(d => d.SubtotalPerdida ?? 0);

    _context.Perdida.Update(nuevaPerdida);
    await _context.SaveChangesAsync();

    Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
    return Content("");
}
 
        // ===============================
        //             EDIT
        // ===============================
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
public async Task<IActionResult> Edit(int id, PerdidaDetalleViewModel vm)
{
    var detalle = await _context.DetallePerdida.FindAsync(id);
    if (detalle == null) return NotFound();

    // Buscar inventario actual
    var inventario = await _context.Inventario
        .FirstOrDefaultAsync(i => i.IdProducto == vm.IdProducto);

    if (inventario == null || inventario.CantidadDisponible + detalle.CantidadPerdida < vm.CantidadPerdida)
    {
        ModelState.AddModelError("", "No hay inventario suficiente para actualizar la pérdida.");
        return PartialView("Edit", vm);
    }

    // Restaurar stock antiguo
    inventario.CantidadDisponible += detalle.CantidadPerdida;

    // Aplicar nueva cantidad
    detalle.CantidadPerdida = vm.CantidadPerdida;
    detalle.IdProducto = vm.IdProducto;
    detalle.PrecioCompraUnitario = inventario.PrecioCompra;

    inventario.CantidadDisponible -= vm.CantidadPerdida;
    if (inventario.CantidadDisponible == 0)
        inventario.Estado = "Agotado";
    else
        inventario.Estado = "Activo";

    _context.DetallePerdida.Update(detalle);
    _context.Inventario.Update(inventario);

    // Recalcular total
    var perdida = await _context.Perdida.FindAsync(detalle.IdPerdida);
    perdida.Motivo = vm.Motivo;
    perdida.Total = await _context.DetallePerdida
        .Where(d => d.IdPerdida == detalle.IdPerdida)
        .SumAsync(d => d.SubtotalPerdida ?? 0);
    _context.Perdida.Update(perdida);

    await _context.SaveChangesAsync();

    Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
    return Content("");
}

        // ===============================
        //            DELETE
        // ===============================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var detalle = await _context.DetallePerdida
                .Include(d => d.IdProductoNavigation)
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

            int idProducto = detalle.IdProducto; // Guardar ID antes de borrar

            _context.DetallePerdida.Remove(detalle);
            await _context.SaveChangesAsync();

            await RecalculateProductoStock(idProducto);

            if (IsHtmxRequest())
            {
                // TRIGGER PARA CERRAR
                Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
                return Content("");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}