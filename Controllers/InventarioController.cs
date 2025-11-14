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

        // GET: Inventario
        public async Task<IActionResult> Index()
        {
            // Incluimos los nombres para mostrar en la lista
            var sistemaInventarioFinalContext = _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation);
            return View(await sistemaInventarioFinalContext.ToListAsync());
        }

        // GET: Inventario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .FirstOrDefaultAsync(m => m.IdInventario == id);
            if (inventario == null)
            {
                return NotFound();
            }

            return View(inventario);
        }

        // GET: Inventario/Create
        // --- MÉTODO MODIFICADO ---
        public IActionResult Create()
        {
            // Cargar listas para los dropdowns
            // Usamos 'VProducto' que es más ligero si solo queremos Id y Nombre
            ViewData["IdProducto"] = new SelectList(_context.VProducto, "IdProducto", "Nombre");
            ViewData["IdProveedor"] = new SelectList(_context.Proveedor, "IdProveedor", "Nombre"); // Asumo que Proveedor tiene "Nombre"
            return View();
        }

        // POST: Inventario/Create
        // --- MÉTODO REEMPLAZADO CON LÓGICA DE NEGOCIO ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProducto,IdProveedor,Cantidad,PrecioCompra,FechaEntrada")] Inventario inventario)
        {
            // Quitamos 'CantidadDisponible', 'FechaSalida', 'Estado' del Bind
            
            if (ModelState.IsValid)
            {
                // Usamos una transacción
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Configurar el nuevo lote
                        inventario.CantidadDisponible = inventario.Cantidad; // Lógica clave
                        inventario.Estado = "EnExistencia"; // Estado por defecto
                        inventario.FechaSalida = null; // Aún no ha salido

                        _context.Add(inventario);
                        await _context.SaveChangesAsync(); // Guardar el lote

                        // 2. Buscar el producto maestro
                        var producto = await _context.Producto.FindAsync(inventario.IdProducto);
                        if (producto == null)
                        {
                            throw new Exception("El producto seleccionado no existe.");
                        }

                       // 3. Sumar al stock maestro
                        producto.Cantidad += inventario.Cantidad;

                        // === INICIO DE LA MEJORA ===
                        // Si el producto estaba 'Inactivo' (stock 0) y le metimos stock,
                        // lo volvemos a poner 'Activo'.
                        if (producto.Cantidad > 0 && producto.Estado == "Inactivo")
                        {
                            producto.Estado = "Activo";
                        }
                        // === FIN DE LA MEJORA ===

                        _context.Update(producto);
                        await _context.SaveChangesAsync(); // Guardar el producto
                        
                        // 4. Confirmar transacción
                        await transaction.CommitAsync();
                        
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // 5. Revertir si algo falla
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                    }
                }
            }
            
            // Si el modelo no es válido, recargar dropdowns
            ViewData["IdProducto"] = new SelectList(_context.VProducto, "IdProducto", "Nombre", inventario.IdProducto);
            ViewData["IdProveedor"] = new SelectList(_context.Proveedor, "IdProveedor", "Nombre", inventario.IdProveedor);
            return View(inventario);
        }

        // GET: Inventario/Edit/5
        // (Dejamos Edit y Delete como estaban por ahora, aunque 'Edit' necesitaría 
        // una lógica más compleja para recalcular el stock maestro si se cambia la cantidad)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario == null)
            {
                return NotFound();
            }
            ViewData["IdProducto"] = new SelectList(_context.Producto, "IdProducto", "Nombre", inventario.IdProducto);
            ViewData["IdProveedor"] = new SelectList(_context.Proveedor, "IdProveedor", "Nombre", inventario.IdProveedor);
            return View(inventario);
        }

        // POST: Inventario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdInventario,IdProducto,IdProveedor,Cantidad,CantidadDisponible,PrecioCompra,FechaEntrada,FechaSalida,Estado")] Inventario inventario)
        {
            if (id != inventario.IdInventario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventarioExists(inventario.IdInventario))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdProducto"] = new SelectList(_context.Producto, "IdProducto", "Nombre", inventario.IdProducto);
            ViewData["IdProveedor"] = new SelectList(_context.Proveedor, "IdProveedor", "Nombre", inventario.IdProveedor);
            return View(inventario);
        }

        // GET: Inventario/Delete/5
        // (Borrar un lote de inventario también debería restar del stock maestro,
        // pero lo dejaremos así por simplicidad por ahora)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventario = await _context.Inventario
                .Include(i => i.IdProductoNavigation)
                .Include(i => i.IdProveedorNavigation)
                .FirstOrDefaultAsync(m => m.IdInventario == id);
            if (inventario == null)
            {
                return NotFound();
            }

            return View(inventario);
        }

        // POST: Inventario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventario = await _context.Inventario.FindAsync(id);
            if (inventario != null)
            {
                _context.Inventario.Remove(inventario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InventarioExists(int id)
        {
            return _context.Inventario.Any(e => e.IdInventario == id);
        }
    }
}