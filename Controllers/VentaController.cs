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

        // Función helper para saber si es petición HTMX
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // GET: Venta
        public IActionResult Index()
        {
            return View();
        }

        // 1. Modificar GetVentaList para recibir filtros
        [HttpGet]
        public async Task<IActionResult> GetVentaList(DateTime? fechaInicio, DateTime? fechaFin, int? idVenta)
        {
            // Query base
            var query = _context.VVentum.AsNoTracking().AsQueryable();

            // Filtro por rango de fechas
            if (fechaInicio.HasValue)
            {
                query = query.Where(v => v.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                // Agregamos un día para incluir todo el día final hasta las 23:59:59
                query = query.Where(v => v.Fecha < fechaFin.Value.AddDays(1));
            }

            // Filtro por ID específico (Búsqueda exacta)
            if (idVenta.HasValue)
            {
                query = query.Where(v => v.IdVenta == idVenta.Value);
            }

            // Ordenar: Más recientes primero
            var ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

            return PartialView("_VentaList", ventas);
        }

// 2. RECOMENDACIÓN DE SEGURIDAD: 
// Comenta o elimina las acciones Edit y Delete (GET y POST).
// Una venta realizada es un documento legal/contable finalizado. 
// No se debe editar ni borrar. Si hubo un error, se debe hacer una "Devolución" (Nota de Crédito).

/*
public async Task<IActionResult> Edit(...) { ... }
public async Task<IActionResult> Delete(...) { ... }
*/
        // GET: Venta/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Buscamos la venta maestra (usando VVentum)
            var venta = await _context.VVentum
                .AsNoTracking() // AÑADIDO: AsNoTracking para vistas de solo lectura
                .FirstOrDefaultAsync(m => m.IdVenta == id);

            if (venta == null)
            {
                return NotFound();
            }

            // 2. Buscamos los detalles (usando VDetalleVentum)
            var detallesDb = await _context.VDetalleVentum
                .Where(d => d.IdVenta == id)
                .AsNoTracking() // AÑADIDO
                .ToListAsync();

            // 3. Obtenemos los IDs de los productos de esos detalles
            var productoIds = detallesDb.Select(d => d.IdProducto).Distinct().ToList();

            // 4. Buscamos TODOS los productos necesarios en UNA sola consulta
            var productos = await _context.Producto
                .Where(p => productoIds.Contains(p.IdProducto))
                .AsNoTracking() // AÑADIDO
                .ToListAsync(); 

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

            if (IsHtmxRequest())
            {
                // Si es HTMX, devolvemos el modal
                return PartialView("Details", viewModel);
            }

            return View(viewModel);
        }


        // GET: Venta/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new VentaViewModel();

            // REGLA: En el model crear de venta se debe ver solo los productos que tengan stock
            var productosConStock = await _context.VProducto
                                            .Where(p => p.Cantidad > 0)
                                            .OrderBy(p => p.Nombre)
                                            .ToListAsync();

            ViewData["Productos"] = new SelectList(productosConStock, "IdProducto", "Nombre");
            
            return View(viewModel);
        }

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
                            // A. Buscar Producto Maestro
                            var producto = await _context.Producto.FindAsync(item.IdProducto);
                            if (producto == null) throw new Exception($"Producto ID {item.IdProducto} no encontrado.");

                            // B. Validar Stock Total
                            if (producto.Cantidad < item.Cantidad)
                            {
                                throw new Exception($"Stock insuficiente para '{producto.Nombre}'. Solicitado: {item.Cantidad}, Disponible: {producto.Cantidad}");
                            }

                            // C. Descontar del Maestro
                            producto.Cantidad -= item.Cantidad;
                            if (producto.Cantidad == 0) producto.Estado = "Inactivo"; // Auto-inactivar si queda en 0
                            _context.Update(producto);

                            // D. LÓGICA FIFO Y CÁLCULO DE COSTO REAL (PUNTO 3)
                            int cantidadPendiente = item.Cantidad;
                            decimal costoTotalCompra = 0; // Acumulador para calcular el costo real de esta venta

                            // Traer lotes con stock, ordenados por fecha (FIFO: Primero en entrar, primero en salir)
                            var lotes = await _context.Inventario
                                .Where(i => i.IdProducto == item.IdProducto && i.CantidadDisponible > 0)
                                .OrderBy(i => i.FechaEntrada)
                                .ToListAsync();

                            foreach (var lote in lotes)
                            {
                                if (cantidadPendiente <= 0) break;

                                int cantidadATomar = Math.Min(cantidadPendiente, lote.CantidadDisponible);

                                // Descontar del lote
                                lote.CantidadDisponible -= cantidadATomar;
                                
                                // ACUMULAR COSTO: (Cantidad tomada de este lote * Precio que costó este lote)
                                costoTotalCompra += (cantidadATomar * lote.PrecioCompra);

                                // Actualizar estado del lote
                                if (lote.CantidadDisponible == 0)
                                {
                                    lote.Estado = "Agotado";
                                    lote.FechaSalida = DateTime.Now;
                                }
                                _context.Update(lote);

                                cantidadPendiente -= cantidadATomar;
                            }

                            // Verificación de integridad (no debería pasar si el stock maestro estaba bien)
                            if (cantidadPendiente > 0)
                            {
                                throw new Exception($"Error de integridad: El stock maestro decía que había {producto.Cantidad + item.Cantidad}, pero los lotes no sumaban suficiente.");
                            }

                            // E. Calcular Costo Unitario Promedio para este registro
                            // Esto asegura que la ganancia se calcule correctamente aunque se vendan productos de lotes con precios distintos.
                            decimal costoUnitarioPromedio = costoTotalCompra / item.Cantidad;

                            // F. Crear Detalle de Venta
                            var detalle = new DetalleVentum
                            {
                                IdVenta = venta.IdVenta,
                                IdProducto = item.IdProducto,
                                CantidadVendida = item.Cantidad,
                                PrecioVentaUnitario = item.PrecioVentaUnitario,
                                
                                // AQUÍ ESTÁ LA CORRECCIÓN CLAVE:
                                // Guardamos el costo real calculado, no el genérico de la tabla Producto.
                                PrecioCompraUnitario = costoUnitarioPromedio 
                            };
                            _context.Add(detalle);
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
        // MODIFICADO: Devuelve VVentum y es compatible con modal
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Usamos VVentum para mostrar la confirmación
            var ventum = await _context.VVentum
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdVenta == id);
                
            if (ventum == null)
            {
                return NotFound();
            }

            if (IsHtmxRequest())
            {
                return PartialView("Delete", ventum);
            }
            return View(ventum);
        }

        // POST: Venta/Delete/5
        // MODIFICADO: Añadida validación de dependencias y respuesta HTMX
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // MEJORA DE SEGURIDAD: Validar dependencias
            bool tieneDetalles = await _context.DetalleVenta.AnyAsync(d => d.IdVenta == id);
            if (tieneDetalles)
            {
                ModelState.AddModelError("", "No se puede eliminar una venta que tiene productos (detalles) asociados. Considere anular la venta.");
                
                var ventumParaError = await _context.VVentum
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.IdVenta == id);
                    
                // Devolvemos el modal con el error
                return PartialView("Delete", ventumParaError);
            }

            var ventum = await _context.Venta.FindAsync(id);
            if (ventum != null)
            {
                _context.Venta.Remove(ventum);
                await _context.SaveChangesAsync();
            }

            // Respuesta HTMX
            if (IsHtmxRequest())
            {
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshVentaList");
                return Content("", "text/html");
            }
            
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

                public async Task<IActionResult> ImprimirFactura(int? id)
                {
                    if (id == null) return NotFound();

                    // 1. Buscamos la venta maestra
                    var venta = await _context.VVentum
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.IdVenta == id);

                    if (venta == null) return NotFound();

                    // 2. Buscamos los detalles
                    var detallesDb = await _context.VDetalleVentum
                        .Where(d => d.IdVenta == id)
                        .AsNoTracking()
                        .ToListAsync();

                    // 3. Traemos nombres de productos
                    var productoIds = detallesDb.Select(d => d.IdProducto).Distinct().ToList();
                    var productos = await _context.Producto
                        .Where(p => productoIds.Contains(p.IdProducto))
                        .AsNoTracking()
                        .ToListAsync();

                    // 4. Armamos el ViewModel (Igual que en Details)
                    var detallesVm = (from d in detallesDb
                                    join p in productos on d.IdProducto equals p.IdProducto
                                    select new VentaDetailItemViewModel
                                    {
                                        NombreProducto = p.Nombre,
                                        CantidadVendida = d.CantidadVendida,
                                        PrecioVentaUnitario = d.PrecioVentaUnitario,
                                        Subtotal = d.Subtotal
                                    }).ToList();

                    var viewModel = new VentaDetailsViewModel
                    {
                        Venta = venta,
                        Detalles = detallesVm
                    };

                    return View(viewModel);
                }

    }
}