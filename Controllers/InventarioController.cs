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
    public class InventarioController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public InventarioController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // =====================================================================
        //                         SECCIÓN HTMX
        // =====================================================================

        // Helper para detectar si la petición viene desde HTMX
        // Esto permite devolver vistas parciales en lugar del layout completo.
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // Load automático de dropdowns (Producto y Proveedor).
        // Se usa en Create y Edit.
        private async Task PopulateDropdowns(Inventario inventario = null)
        {
            if (inventario == null)
            {
                // Para formularios nuevos
                ViewData["IdProducto"] = new SelectList(
                    await _context.VProducto.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(),
                    "IdProducto", "Nombre"
                );
                ViewData["IdProveedor"] = new SelectList(
                    await _context.Proveedor.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(),
                    "IdProveedor", "Nombre"
                );
            }
            else
            {
                // Para edición (con valores seleccionados)
                ViewData["IdProducto"] = new SelectList(
                    await _context.VProducto.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(),
                    "IdProducto", "Nombre", inventario.IdProducto
                );
                ViewData["IdProveedor"] = new SelectList(
                    await _context.Proveedor.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(),
                    "IdProveedor", "Nombre", inventario.IdProveedor
                );
            }
        }

        // =====================================================================
        // RE-CALCULAR STOCK MAESTRO DEL PRODUCTO
        // =====================================================================

        // Esta función recalcula el stock total del producto tomando
        // la suma de la CantidadDisponible de todos sus lotes.
        private async Task RecalculateMasterStock(int idProducto)
        {
            var producto = await _context.Producto.FindAsync(idProducto);
            if (producto == null) return;

            // Suma total del inventario disponible en los lotes del producto
            var nuevoStockMaestro = await _context.Inventario
                .Where(i => i.IdProducto == idProducto)
                .SumAsync(i => i.CantidadDisponible);

            // Actualiza la entidad Producto
            producto.Cantidad = nuevoStockMaestro;
            producto.Estado = (producto.Cantidad > 0) ? "Activo" : "Inactivo";

            _context.Update(producto);
            await _context.SaveChangesAsync(); // Guardamos los cambios
        }

        // =====================================================================
        // INDEX GENERAL
        // =====================================================================

        // Página principal: solo carga la carcasa del frontend.
        public IActionResult Index()
        {
            return View();
        }

        // Endpoint que HTMX usa para cargar la tabla del inventario.
        [HttpGet]
        public async Task<IActionResult> GetInventarioList()
        {
            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .AsNoTracking()
                .OrderByDescending(i => i.FechaEntrada)
                .ToListAsync();

            return PartialView("_InventarioList", inventario);
        }

        // =====================================================================
        // DETALLES
        // =====================================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdInventario == id);

            if (inventario == null) return NotFound();

            return IsHtmxRequest()
                ? PartialView("Details", inventario)
                : View(inventario);
        }

        // =====================================================================
        // CREATE
        // =====================================================================

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns(); // Cargar selects

            if (IsHtmxRequest())
            {
                return PartialView("Create", new Inventario { FechaEntrada = DateTime.Today });
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProducto,IdProveedor,Cantidad,PrecioCompra,FechaEntrada")] Inventario inventario)
        {
            if (ModelState.IsValid)
            {
                // Transacción para evitar inconsistencias
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Definir datos del nuevo lote
                        inventario.CantidadDisponible = inventario.Cantidad;
                        inventario.Estado = "EnExistencia";
                        inventario.FechaSalida = null;

                        _context.Add(inventario);
                        await _context.SaveChangesAsync(); // Guardamos el lote

                        // 2. Actualizar stock maestro del producto
                        await RecalculateMasterStock(inventario.IdProducto);

                        await transaction.CommitAsync();

                        // Cierra modal y recarga la tabla
                        Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshInventarioList");
                        return Content("", "text/html");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                    }
                }
            }

            // Si hay errores, recargar dropdowns y devolver formulario
            await PopulateDropdowns(inventario);
            return PartialView("Create", inventario);
        }

        // =====================================================================
        // EDIT
        // =====================================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return NotFound();

            await PopulateDropdowns(inventario);

            return IsHtmxRequest()
                ? PartialView("Edit", inventario)
                : View(inventario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdInventario,IdProducto,IdProveedor,Cantidad,CantidadDisponible,PrecioCompra,FechaEntrada,FechaSalida,Estado")] Inventario inventario)
        {
            if (id != inventario.IdInventario) return NotFound();

            if (ModelState.IsValid)
            {
                // Obtenemos el lote original antes de actualizar
                var inventarioOriginal = await _context.Inventario
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.IdInventario == id);

                if (inventarioOriginal == null) return NotFound();

                // Validación: no permitir disponible > cantidad total
                if (inventario.CantidadDisponible > inventario.Cantidad)
                {
                    ModelState.AddModelError("CantidadDisponible",
                        "La cantidad disponible no puede ser mayor que la cantidad total del lote.");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns(inventario);
                    return PartialView("Edit", inventario);
                }

                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _context.Update(inventario);
                        await _context.SaveChangesAsync();

                        // Recalcular stock maestro del nuevo producto
                        await RecalculateMasterStock(inventario.IdProducto);

                        // Si cambió el producto, recalcular también el anterior
                        if (inventarioOriginal.IdProducto != inventario.IdProducto)
                        {
                            await RecalculateMasterStock(inventarioOriginal.IdProducto);
                        }

                        await transaction.CommitAsync();

                        Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshInventarioList");
                        return Content("", "text/html");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                    }
                }
            }

            await PopulateDropdowns(inventario);
            return PartialView("Edit", inventario);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.IdInventario == id);

            if (inventario == null) return NotFound();

            return IsHtmxRequest()
                ? PartialView("Delete", inventario)
                : View(inventario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return NotFound();

            // Validación: no borrar si tiene solicitudes de devolución
            bool tieneSolicitudes = await _context.SolicitudDevolucion
                .AnyAsync(s => s.IdInventario == id);

            if (tieneSolicitudes)
            {
                ModelState.AddModelError("", "No se puede borrar. Este lote tiene solicitudes de devolución asociadas.");
                await _context.Entry(inventario).Reference(i => i.IdProductoNavigation).LoadAsync();
                await _context.Entry(inventario).Reference(i => i.IdProveedorNavigation).LoadAsync();
                return PartialView("Delete", inventario);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int idProductoAfectado = inventario.IdProducto;

                    _context.Inventario.Remove(inventario);
                    await _context.SaveChangesAsync();  // El lote ya fue eliminado

                    // Ahora recalculamos el stock maestro
                    await RecalculateMasterStock(idProductoAfectado);

                    await transaction.CommitAsync();

                    if (IsHtmxRequest())
                    {
                        Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshInventarioList");
                        return Content("", "text/html");
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al borrar: " + ex.Message);

                    await _context.Entry(inventario).Reference(i => i.IdProductoNavigation).LoadAsync();
                    await _context.Entry(inventario).Reference(i => i.IdProveedorNavigation).LoadAsync();

                    return PartialView("Delete", inventario);
                }
            }
        }

        private bool InventarioExists(int id)
        {
            return _context.Inventario.Any(e => e.IdInventario == id);
        }
    }
}
