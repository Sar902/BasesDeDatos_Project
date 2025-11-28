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
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, int? idProveedor, int? idProducto)
        {
            // 1. Cargar listas para los filtros (Proveedores y Productos)
            ViewData["Proveedores"] = new SelectList(await _context.Proveedor.OrderBy(p => p.Nombre).AsNoTracking().ToListAsync(), "IdProveedor", "Nombre", idProveedor);
            
            // Cargamos productos ordenados alfabéticamente para facilitar la búsqueda
            ViewData["Productos"] = new SelectList(await _context.Producto.OrderBy(p => p.Nombre).AsNoTracking().ToListAsync(), "IdProducto", "Nombre", idProducto);

            // 2. Query Base
            var query = _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .AsNoTracking()
                .AsQueryable();

            // 3. Aplicar Filtros
            if (fechaInicio.HasValue) query = query.Where(i => i.FechaEntrada >= fechaInicio.Value);
            if (fechaFin.HasValue) query = query.Where(i => i.FechaEntrada < fechaFin.Value.AddDays(1));
            if (idProveedor.HasValue) query = query.Where(i => i.IdProveedor == idProveedor);
            
            // --- NUEVO FILTRO DE PRODUCTO ---
            if (idProducto.HasValue)
            {
                query = query.Where(i => i.IdProducto == idProducto);
            }

            // 4. Ejecutar y Agrupar
            // Nota: Al filtrar por producto, los grupos (compras) solo mostrarán 
            // las líneas que coincidan con ese producto, lo cual es perfecto para el análisis.
            var inventarioCrudo = await query.OrderByDescending(i => i.FechaEntrada).ToListAsync();

            var comprasVirtuales = inventarioCrudo
                .GroupBy(x => new { x.FechaEntrada.Date, x.IdProveedor }) 
                .Select(grupo => new CompraAgrupadaViewModel
                {
                    Fecha = grupo.Key.Date,
                    NombreProveedor = grupo.First().IdProveedorNavigation?.Nombre ?? "Proveedor Desconocido",
                    TotalCompra = grupo.Sum(item => item.Cantidad * item.PrecioCompra),
                    CantidadLotes = grupo.Count(),
                    Lotes = grupo.ToList()
                })
                .OrderByDescending(x => x.Fecha)
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

        // POST: Inventario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdInventario,IdProducto,IdProveedor,Cantidad,PrecioCompra,FechaEntrada,Estado")] Inventario inventario)
        {
            // NOTA: Quité 'CantidadDisponible' del Bind de arriba para protegerlo.

            if (id != inventario.IdInventario) return NotFound();

            // Validamos manualmente porque quitamos campos del Bind
            if (inventario.PrecioCompra < 0) ModelState.AddModelError("PrecioCompra", "El precio no puede ser negativo");
            if (inventario.Cantidad < 1) ModelState.AddModelError("Cantidad", "La cantidad debe ser mayor a 0");

            if (ModelState.IsValid)
            {
                // Traemos el original "AsNoTracking" false para poder rastrear cambios o solo lectura
                var inventarioOriginal = await _context.Inventario.AsNoTracking().FirstOrDefaultAsync(i => i.IdInventario == id);

                if (inventarioOriginal == null) return NotFound();

                // 1. CÁLCULO DE INTEGRIDAD
                // Calculamos cuántos items se han gastado de este lote hasta hoy
                int itemsGastados = inventarioOriginal.Cantidad - inventarioOriginal.CantidadDisponible;

                // Si el usuario reduce la cantidad total a menos de lo que ya se gastó, es un error.
                if (inventario.Cantidad < itemsGastados)
                {
                    ModelState.AddModelError("Cantidad", $"No puedes reducir la cantidad a {inventario.Cantidad} porque ya se han vendido {itemsGastados} unidades de este lote. El mínimo permitido es {itemsGastados}.");
                    await PopulateDropdowns(inventario);
                    return View(inventario);
                }

                // 2. ACTUALIZACIÓN AUTOMÁTICA
                // La nueva disponibilidad es la Nueva Cantidad Total - Lo que ya se gastó
                inventario.CantidadDisponible = inventario.Cantidad - itemsGastados;

                // Mantener campos que no deberían cambiar o que no vienen en el form
                // (Opcional: Si quieres bloquear cambio de producto, descomenta la siguiente linea)
                // inventario.IdProducto = inventarioOriginal.IdProducto; 

                try
                {
                    _context.Update(inventario);
                    await _context.SaveChangesAsync();

                    await RecalculateMasterStock(inventario.IdProducto);
                    
                    // Si cambió de producto (raro pero posible), recalculamos el stock del producto viejo también
                    if (inventarioOriginal.IdProducto != inventario.IdProducto)
                    {
                        await RecalculateMasterStock(inventarioOriginal.IdProducto);
                    }
                    
                    // Retorno directo a Index
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar: " + ex.Message);
                }
            }

            await PopulateDropdowns(inventario);
            return View(inventario);
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
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null) return NotFound();

            // 1. VALIDACIÓN CRÍTICA: ¿El lote está intacto?
            // Si la cantidad inicial es distinta a la disponible, significa que ya se vendió o movió algo.
            if (inventario.Cantidad != inventario.CantidadDisponible)
            {
                ModelState.AddModelError("", "No se puede eliminar esta compra porque ya se han vendido o utilizado productos de este lote. Realice un ajuste de inventario o una devolución en su lugar.");
                
                // Recargamos datos para volver a mostrar la vista con el error
                await _context.Entry(inventario).Reference(i => i.IdProductoNavigation).LoadAsync();
                await _context.Entry(inventario).Reference(i => i.IdProveedorNavigation).LoadAsync();
                return View("Delete", inventario);
            }

            // 2. Validación de Devoluciones (que ya tenías)
            bool tieneSolicitudes = await _context.SolicitudDevolucion.AnyAsync(s => s.IdInventario == id);
            if (tieneSolicitudes)
            {
                ModelState.AddModelError("", "No se puede borrar: Este lote tiene devoluciones asociadas.");
                await _context.Entry(inventario).Reference(i => i.IdProductoNavigation).LoadAsync();
                await _context.Entry(inventario).Reference(i => i.IdProveedorNavigation).LoadAsync();
                return View("Delete", inventario);
            }

            // 3. Proceder a borrar
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    int idProductoAfectado = inventario.IdProducto;
                    _context.Inventario.Remove(inventario);
                    await _context.SaveChangesAsync();

                    await RecalculateMasterStock(idProductoAfectado);
                    await transaction.CommitAsync();
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Error al borrar: " + ex.Message);
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
