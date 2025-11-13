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
    public class VentaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public VentaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: Venta
        public async Task<IActionResult> Index()
        {
            return View(await _context.Venta.ToListAsync());
        }

        // GET: Venta/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Creamos el ViewModel
            var viewModel = new VentaDetailsViewModel();

            // 2. Buscamos la venta maestra (usando la vista VVentum)
            viewModel.Venta = await _context.VVentum
                .FirstOrDefaultAsync(m => m.IdVenta == id);

            if (viewModel.Venta == null)
            {
                // Si no se encuentra la venta, retornamos NotFound
                return NotFound();
            }

            // 3. Buscamos los detalles de esa venta (usando la vista VDetalleVentum)
            viewModel.Detalles = await _context.VDetalleVentum
                .Where(d => d.IdVenta == id)
                .ToListAsync();

            // 4. Pasamos el ViewModel (que contiene Venta y Detalles) a la vista
            return View(viewModel);
        }

        // GET: Venta/Create
            public IActionResult Create()
        {
            // Creamos el ViewModel vacío
            var viewModel = new VentaViewModel();

            // También enviamos la lista de todos los productos a la vista
            // para que el usuario pueda elegirlos.
            // Usamos la vista 'VProducto' que ya tienes
            ViewData["Productos"] = new SelectList(_context.VProducto, "IdProducto", "Nombre");
            
            return View(viewModel); // Pasamos el ViewModel a la vista
        }

        // POST: Venta/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Venta/Create
        // POST: Venta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VentaViewModel viewModel)
        {
            // NOTA: Esta acción espera que la VISTA (que haremos después)
            // llene la lista 'viewModel.Items' usando JavaScript.
            // Por ahora, el formulario simple no funcionará.

            if (viewModel.Items == null || !viewModel.Items.Any())
            {
                ModelState.AddModelError("", "No se puede crear una venta sin productos.");
            }

            if (ModelState.IsValid)
            {
                // Usamos una transacción. Si algo falla (ej. no hay stock),
                // se revierte toda la operación.
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Crear la Venta "maestra"
                        var venta = new Ventum
                        {
                            Fecha = viewModel.Fecha,
                            Total = viewModel.Items!.Sum(item => item.Subtotal) // Calculamos el total
                        };
                        _context.Add(venta);
                        await _context.SaveChangesAsync(); // Guardamos para obtener el IdVenta

                        // 2. Recorrer los productos del "carrito"
                        foreach (var item in viewModel.Items!)
                        {
                            // 3. Buscar el producto en la base de datos
                            var producto = await _context.Producto.FindAsync(item.IdProducto);
                            if (producto == null)
                            {
                                throw new Exception($"Producto {item.NombreProducto} no encontrado.");
                            }

                            // 4. Verificar y descontar el stock
                            if (producto.Cantidad < item.Cantidad)
                            {
                                throw new Exception($"No hay suficiente stock para {producto.Nombre}. Stock actual: {producto.Cantidad}");
                            }
                            producto.Cantidad -= item.Cantidad; // Descontamos el stock
                            _context.Update(producto);

                            // OJO: Tu DetalleVentum pide un PrecioCompraUnitario.
                            // Debemos obtenerlo de alguna parte. Lo ideal es de la vista VProducto.
                            // 1. OBTENEMOS EL vProducto ANTES de crear el DetalleVentum
                            var vProducto = await _context.VProducto.FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto);

                            // 5. Crear el DetalleVentum
                            var detalleVenta = new DetalleVentum
                            {
                                IdVenta = venta.IdVenta, // Asignamos el ID de la venta maestra
                                IdProducto = item.IdProducto,
                                CantidadVendida = item.Cantidad,
                                PrecioVentaUnitario = item.PrecioVentaUnitario,
                                // El Subtotal se calculará por la base de datos (si tienes triggers) o lo asignamos
                                Subtotal = item.Subtotal,
                                
                                // AHORA sÍ podemos usar la variable vProducto
                                PrecioCompraUnitario = vProducto?.PrecioCompra ?? 0,
                            };
                            _context.Add(detalleVenta);
                        }

                        // 6. Guardar todos los cambios (Detalles y actualización de stock)
                        await _context.SaveChangesAsync();

                        // 7. Confirmar la transacción
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // Si algo falló, revertir todo
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Error al crear la venta: {ex.Message}");
                    }
                }
            }

            // Si el modelo falla, recargamos la lista de productos
            ViewData["Productos"] = new SelectList(_context.VProducto, "IdProducto", "Nombre");
            return View(viewModel);
        }

        // GET: Venta/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ventum = await _context.Venta.FindAsync(id);
            if (ventum == null)
            {
                return NotFound();
            }
            return View(ventum);
        }

        // POST: Venta/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdVenta,Fecha,Total")] Ventum ventum)
        {
            if (id != ventum.IdVenta)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ventum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VentumExists(ventum.IdVenta))
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
            return View(ventum);
        }

        // GET: Venta/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ventum = await _context.Venta
                .FirstOrDefaultAsync(m => m.IdVenta == id);
            if (ventum == null)
            {
                return NotFound();
            }

            return View(ventum);
        }

        // POST: Venta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ventum = await _context.Venta.FindAsync(id);
            if (ventum != null)
            {
                _context.Venta.Remove(ventum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VentumExists(int id)
        {
            return _context.Venta.Any(e => e.IdVenta == id);
        }

        [HttpGet] // Esto permite que JavaScript lo llame
        public async Task<IActionResult> GetProductoDetails(int id)
        {
            // Usamos la vista 'VProducto' que ya tiene los precios
            var producto = await _context.VProducto.FirstOrDefaultAsync(p => p.IdProducto == id);

            if (producto == null)
            {
                return NotFound();
            }

            // Devolvemos los datos en formato JSON
            return Json(new
            {
                idProducto = producto.IdProducto,
                nombre = producto.Nombre,
                precioVenta = producto.PrecioVenta, // Este es el precio que necesitamos
                stockActual = producto.Cantidad // (Opcional) podríamos usar 'producto.Stock' si VProducto lo tuviera
            });
        }
    }
}
