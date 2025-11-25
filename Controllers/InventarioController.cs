using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.ViewModels;
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
        public async Task<IActionResult> Index()
        {
            // 1. Traemos TODOS los datos crudos (incluyendo nombres de productos y proveedores)
            var inventarioCrudo = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .AsNoTracking()
                .OrderByDescending(i => i.FechaEntrada)
                .ToListAsync();

            // 2. HACEMOS LA MAGIA: Agrupar en memoria
            // La lógica es: Si tienen la misma Fecha y el mismo Proveedor, pertenecen a la misma "Compra"
            var comprasVirtuales = inventarioCrudo
                .GroupBy(x => new { x.FechaEntrada.Date, x.IdProveedor }) 
                .Select(grupo => new CompraAgrupadaViewModel
                {
                    Fecha = grupo.Key.Date,
                    // Si el proveedor es null, ponemos "Sin Proveedor"
                    NombreProveedor = grupo.First().IdProveedorNavigation?.Nombre ?? "Proveedor Desconocido",
                    
                    // Calculamos el total sumando (Cantidad * PrecioCompra) de cada item
                    TotalCompra = grupo.Sum(item => item.Cantidad * item.PrecioCompra),
                    
                    CantidadLotes = grupo.Count(),
                    
                    // Guardamos la lista de items por si queremos ver el detalle
                    Lotes = grupo.ToList()
                })
                .OrderByDescending(x => x.Fecha) // Las más recientes primero
                .ToList();

            return View(comprasVirtuales);
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
        // GET: Inventario/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CompraViewModel();

            ViewData["Productos"] = new SelectList(await _context.VProducto.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(), "IdProducto", "Nombre");
            ViewData["Proveedores"] = new SelectList(await _context.Proveedor.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(), "IdProveedor", "Nombre");

            // CAMBIO: Ya no devolvemos PartialView, siempre devolvemos View completa (con Layout)
            return View(viewModel);
        }

        // POST: Inventario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompraViewModel viewModel)
        {
            if (viewModel.Items == null || !viewModel.Items.Any())
            {
                ModelState.AddModelError("", "No se puede registrar una compra sin productos.");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in viewModel.Items)
                        {
                            var nuevoLote = new Inventario
                            {
                                IdProducto = item.IdProducto,
                                IdProveedor = item.IdProveedor,
                                Cantidad = item.Cantidad,
                                CantidadDisponible = item.Cantidad,
                                PrecioCompra = item.PrecioCompra,
                                FechaEntrada = viewModel.Fecha,
                                Estado = "EnExistencia"
                            };

                            _context.Add(nuevoLote);
                            await _context.SaveChangesAsync();
                            await RecalculateMasterStock(item.IdProducto);
                        }

                        await transaction.CommitAsync();

                        // CAMBIO: Redirección estándar a Index en lugar de cerrar modal
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Error: {ex.Message}");
                    }
                }
            }

            // Si falla, recargamos los selects
            ViewData["Productos"] = new SelectList(await _context.VProducto.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(), "IdProducto", "Nombre");
            ViewData["Proveedores"] = new SelectList(await _context.Proveedor.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(), "IdProveedor", "Nombre");

            return View(viewModel);
        }

        // =====================================================================
        // EDIT
        // =====================================================================

       // GET: Inventario/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Incluimos la navegación al Producto para obtener su nombre
            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .FirstOrDefaultAsync(i => i.IdInventario == id);

            if (inventario == null) return NotFound();

            // Pasamos el nombre a la vista para mostrarlo en el campo de solo lectura
            ViewBag.NombreProducto = inventario.IdProductoNavigation?.Nombre;

            await PopulateDropdowns(inventario);

            // Ahora siempre devolvemos la Vista normal (sin modal)
            return View(inventario);
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

        // GET: Inventario/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdInventario == id);

            if (inventario == null) return NotFound();

            // Devolvemos la vista completa estándar
            return View(inventario);
        }

        // POST: Inventario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return NotFound();

            // Validar si tiene devoluciones asociadas antes de borrar
            bool tieneSolicitudes = await _context.SolicitudDevolucion.AnyAsync(s => s.IdInventario == id);
            if (tieneSolicitudes)
            {
                ModelState.AddModelError("", "No se puede borrar: Este lote tiene devoluciones asociadas.");
                
                // Recargar relaciones para mostrar la vista de error correctamente
                await _context.Entry(inventario).Reference(i => i.IdProductoNavigation).LoadAsync();
                await _context.Entry(inventario).Reference(i => i.IdProveedorNavigation).LoadAsync();
                return View("Delete", inventario);
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int idProductoAfectado = inventario.IdProducto;

                    _context.Inventario.Remove(inventario);
                    await _context.SaveChangesAsync();

                    // IMPORTANTE: Recalcular el stock maestro del producto al borrar
                    await RecalculateMasterStock(idProductoAfectado);

                    await transaction.CommitAsync();
                    
                    // Redirigir al índice general
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al borrar: " + ex.Message);
                    
                    // Recargar relaciones para la vista de error
                    await _context.Entry(inventario).Reference(i => i.IdProductoNavigation).LoadAsync();
                    await _context.Entry(inventario).Reference(i => i.IdProveedorNavigation).LoadAsync();
                    return View("Delete", inventario);
                }
            }
        }

        private bool InventarioExists(int id)
        {
            return _context.Inventario.Any(e => e.IdInventario == id);
        }
    }
}
