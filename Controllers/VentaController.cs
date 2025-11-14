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

            // 1. Buscamos la venta maestra (usando VVentum)
            var venta = await _context.VVentum
                .FirstOrDefaultAsync(m => m.IdVenta == id);

            if (venta == null)
            {
                return NotFound();
            }

            // 2. Buscamos los detalles (usando VDetalleVentum)
            var detallesDb = await _context.VDetalleVentum
                .Where(d => d.IdVenta == id)
                .ToListAsync();

            // 3. Obtenemos los IDs de los productos de esos detalles
            var productoIds = detallesDb.Select(d => d.IdProducto).Distinct().ToList();

            // 4. Buscamos TODOS los productos necesarios en UNA sola consulta
            var productos = await _context.Producto
                .Where(p => productoIds.Contains(p.IdProducto))
                .ToListAsync(); //

            // 5. Unimos las dos listas (detalles + productos) usando LINQ en C#
            var detallesVm = (from d in detallesDb
                            join p in productos on d.IdProducto equals p.IdProducto
                            select new VentaDetailItemViewModel // Creamos el nuevo ViewModel
                            {
                                NombreProducto = p.Nombre, // <-- ¡El nombre del producto!
                                CantidadVendida = d.CantidadVendida,
                                PrecioVentaUnitario = d.PrecioVentaUnitario,
                                Subtotal = d.Subtotal,
                                GananciaSubtotal = d.GananciaSubtotal
                            }).ToList();

            // 6. Creamos el ViewModel final para la vista
            var viewModel = new VentaDetailsViewModel
            {
                Venta = venta,
                Detalles = detallesVm // Asignamos nuestra lista "unida"
            };

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
        // POST: Venta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VentaViewModel viewModel)
        {
            if (viewModel.Items == null || !viewModel.Items.Any())
            {
                ModelState.AddModelError("", "No se puede crear una venta sin productos.");
            }

            // Recargamos los productos para el dropdown en caso de error
            ViewData["Productos"] = new SelectList(_context.VProducto!, "IdProducto", "Nombre");

            if (ModelState.IsValid)
            {
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
                            // 3. Buscar el producto MAESTRO
                            var producto = await _context.Producto.FindAsync(item.IdProducto);
                            if (producto == null)
                            {
                                throw new Exception($"Producto {item.NombreProducto} no encontrado.");
                            }

                            // 4. Verificar y descontar el stock MAESTRO
                            if (producto.Cantidad < item.Cantidad)
                            {
                                throw new Exception($"No hay suficiente stock total para {producto.Nombre}. Stock actual: {producto.Cantidad}");
                            }
                            producto.Cantidad -= item.Cantidad; // Descontamos el stock MAESTRO

                            // === INICIO DE LA MEJORA ===
                            // ¡Mae, aquí está su lógica!
                            // Si el stock maestro llega a 0, lo inactivamos.
                            if (producto.Cantidad == 0)
                            {
                                producto.Estado = "Inactivo";
                            }
                            // === FIN DE LA MEJORA ===

                            _context.Update(producto);

                            // --- INICIO DE LA CIRUGÍA (Paso 5: Lógica FIFO) ---

                            int cantidadAVender = item.Cantidad; // Cantidad que necesitamos despachar

                            // Buscamos los lotes de este producto, del más antiguo al más nuevo,
                            // que todavía tengan stock disponible.
                            var lotesDisponibles = await _context.Inventario
                                .Where(lote => lote.IdProducto == item.IdProducto && lote.CantidadDisponible > 0)
                                .OrderBy(lote => lote.FechaEntrada)
                                .ToListAsync(); //

                            foreach (var lote in lotesDisponibles)
                            {
                                if (cantidadAVender <= 0) 
                                {
                                    break; // Ya completamos la cantidad de esta venta
                                }

                                if (lote.CantidadDisponible >= cantidadAVender)
                                {
                                    // Este lote tiene suficiente para cubrir lo que falta
                                    lote.CantidadDisponible -= cantidadAVender;
                                    cantidadAVender = 0; // Venta completada
                                }
                                else
                                {
                                    // Este lote se vacía y seguimos al siguiente
                                    cantidadAVender -= lote.CantidadDisponible;
                                    lote.CantidadDisponible = 0;
                                }

                                // (Opcional) Actualizar estado si se vació
                                if (lote.CantidadDisponible == 0)
                                {
                                    lote.Estado = "Agotado";
                                    lote.FechaSalida = DateTime.Now;
                                }
                                _context.Update(lote);
                            }

                            // Si después de recorrer todos los lotes aún falta cantidad,
                            // es un error de integridad de datos (Total no calza con Lotes).
                            if (cantidadAVender > 0)
                            {
                                throw new Exception($"Inconsistencia de datos. El stock total de {producto.Nombre} es {producto.Cantidad}, pero los lotes de inventario no suman esa cantidad.");
                            }
                            
                            // --- FIN DE LA CIRUGÍA (Paso 5) ---


                            // 6. Crear el DetalleVentum
                            var vProducto = await _context.VProducto.FirstOrDefaultAsync(p => p.IdProducto == item.IdProducto);
                            var detalleVenta = new DetalleVentum
                            {
                                IdVenta = venta.IdVenta,
                                IdProducto = item.IdProducto,
                                CantidadVendida = item.Cantidad,
                                PrecioVentaUnitario = item.PrecioVentaUnitario,
                                // Subtotal fue eliminado (es calculado por SQL)
                                PrecioCompraUnitario = vProducto?.PrecioCompra ?? 0,
                            };
                            _context.Add(detalleVenta);
                        }

                        // 7. Guardar todos los cambios (Stock Maestro, Lotes, Detalles)
                        await _context.SaveChangesAsync();

                        // 8. Confirmar la transacción
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
            
            // Si el modelo falla, se devuelve a la vista
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
