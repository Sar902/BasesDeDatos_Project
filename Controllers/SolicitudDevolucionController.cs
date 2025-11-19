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
    public class SolicitudDevolucionController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public SolicitudDevolucionController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        private bool IsHtmxRequest()
        {
            return Request.Headers["HX-Request"] == "true";
        }


        // GET: SolicitudDevolucion
        public async Task<IActionResult> Index()
        {
            var solicitudes = await _context.SolicitudDevolucion
                .OrderByDescending(s => s.Fecha)
                .ToListAsync();

            return View(solicitudes);
        }


        public async Task<IActionResult> GetSolicitudesList()
        {
            var lista = await _context.VSoltum.ToListAsync();
            return PartialView("_SolicitudList", lista);
        }

        // GET: Soli/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Buscamos la soli maestra (usando VSoltum)
            var soli = await _context.VSoltum
                .AsNoTracking() // AÑADIDO: AsNoTracking para vistas de solo lectura
                .FirstOrDefaultAsync(m => m.IdSolicitudDevolucion == id);

            if (soli == null)
            {
                return NotFound();
            }

            // 2. Buscamos los detalles (usando VDetalleSoltum)
            var detallesDb = await _context.VDetalleSoltum
                .Where(d => d.IdSolicitudDevolucion == id)
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
            var detallesSm = (from d in detallesDb
                            join p in productos on d.IdProducto equals p.IdProducto
                            select new SoliDetailItemViewModel // Creamos el nuevo ViewModel
                            {
                                NombreProducto = p.Nombre, // <-- ¡El nombre del producto!
                                CantidadSolicitada = d.CantidadSolicitada,
                                PrecioCompraUnitario = d.PrecioCompraUnitario,
                                EstadoItem = d.EstadoItem,
                            }).ToList();

            // 6. Creamos el ViewModel final para la vista
            var viewModel = new SoliDetailsViewModel
            {
                Soli = soli,
                Detalles = detallesSm // Asignamos nuestra lista "unida"
            };

            if (IsHtmxRequest())
            {
                // Si es HTMX, devolvemos el modal
                return PartialView("Details", viewModel);
            }

            return View(viewModel);
        }

        // GET: SolicitudDevolucion/Create
        public async Task<IActionResult> Create()
    {
        var model = new SolicitudDevolucionViewModel
        {
            Items = new List<DetalleSolicitudDevolucionViewModel>()
        };

        var productos = await _context.VProducto
            .Where(p => p.Cantidad > 0)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        ViewData["Productos"] = new SelectList(productos, "IdProducto", "Nombre");

        return View(model);
    }

        // POST: SolicitudDevolucion/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SolicitudDevolucionViewModel model)
    {
        if (model.Items == null || !model.Items.Any())
            ModelState.AddModelError("", "Debe agregar al menos un producto.");

        if (string.IsNullOrWhiteSpace(model.Observaciones))
            ModelState.AddModelError("Observaciones", "Debe ingresar una observación.");

        ViewData["Productos"] = new SelectList(_context.VProducto, "IdProducto", "Nombre");

        if (!ModelState.IsValid)
            return View(model);

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                int? inventarioBase = null; // Inventario único para toda la solicitud

                // ============================================================
                // VALIDAR INVENTARIO Y CALCULAR COSTOS
                // ============================================================
                foreach (var item in model.Items)
                {
                    var inventario = await _context.Inventario
                        .Where(i => i.IdProducto == item.IdProducto)
                        .OrderByDescending(i => i.FechaEntrada)
                        .FirstOrDefaultAsync();

                    if (inventario == null)
                    {
                        ModelState.AddModelError("", $"El producto {item.NombreProducto} no tiene inventarios.");
                        return View(model);
                    }

                    // --- Validar inventario único ---
                    if (inventarioBase == null)
                        inventarioBase = inventario.IdInventario;
                    else if (inventario.IdInventario != inventarioBase)
                    {
                        ModelState.AddModelError("", "Todos los productos deben pertenecer al mismo inventario más reciente.");
                        return View(model);
                    }

                    // --- Calcular precio compra unitario ---
                    if (inventario.Cantidad <= 0)
                    {
                        ModelState.AddModelError("", $"El inventario para {item.NombreProducto} tiene Cantidad = 0.");
                        return View(model);
                    }

                    decimal precioUnitario = inventario.PrecioCompra / (decimal)inventario.Cantidad;

                    item.IdInventario = inventario.IdInventario;
                    item.PrecioCompraUnitario = decimal.Round(precioUnitario, 4);
                }

                // ============================================================
                // CREAR SOLICITUD PRINCIPAL
                // ============================================================
                var solicitud = new SolicitudDevolucion
                {
                    Observaciones = model.Observaciones,
                    Estado = "Pendiente",
                    Fecha = DateTime.Now,
                    IdInventario = inventarioBase!.Value
                };

                _context.Add(solicitud);
                await _context.SaveChangesAsync();

                // ============================================================
                // GUARDAR DETALLES
                // ============================================================
                foreach (var item in model.Items)
                {
                    var detalle = new DetalleSolicitudDevolucion
                    {
                        IdSolicitudDevolucion = solicitud.IdSolicitudDevolucion,
                        IdProducto = item.IdProducto,
                        CantidadSolicitada = item.CantidadSolicitada,
                        PrecioCompraUnitario = item.PrecioCompraUnitario,
                        EstadoItem = "Pendiente",
                    };

                    _context.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", $"Error al crear la solicitud: {ex.Message}");
                return View(model);
            }
        }
    }

        // GET: SolicitudDevolucion/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var solicitudDevolucion = await _context.SolicitudDevolucion.FirstOrDefaultAsync(s => s.IdSolicitudDevolucion == id);

            if (solicitudDevolucion == null) return NotFound();

            var vm = new SoliEditViewModel
            {
                IdSolicitudDevolucion = solicitudDevolucion.IdSolicitudDevolucion,
                Observaciones = solicitudDevolucion.Observaciones,
                Fecha = solicitudDevolucion.Fecha,
                Estado = solicitudDevolucion.Estado
            };

            ViewData["Estados"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Rechazado", Text = "Rechazado" },
                new SelectListItem { Value = "Aceptado", Text = "Aceptado" }
            };

            return PartialView("Edit", vm);
        }

// POST: Perdida/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, SoliEditViewModel model)
{
    if (id != model.IdSolicitudDevolucion) return BadRequest();
    var solicitudDevolucion = await _context.SolicitudDevolucion.FirstOrDefaultAsync(s => s.IdSolicitudDevolucion == id);

    if (solicitudDevolucion == null) return NotFound();

    solicitudDevolucion.Observaciones = model.Observaciones;
    solicitudDevolucion.Fecha = model.Fecha;
    solicitudDevolucion.Estado = model.Estado;

    await _context.SaveChangesAsync();

    Response.Headers["HX-Trigger"] = "refreshSolicitudesList, htmx:closeModal";
    return Content("");
}


        // GET: SolicitudDevolucion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var solicitud = await _context.SolicitudDevolucion
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSolicitudDevolucion == id);

            if (solicitud == null) return NotFound();

            if (Request.Headers.ContainsKey("HX-Request"))
                return PartialView("Delete", solicitud);

            return View(solicitud);
        }

        // POST: SolicitudDevolucion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var solicitud = await _context.SolicitudDevolucion.FindAsync(id);
            if (solicitud == null) return NotFound();

            // Eliminar detalles
            var detalles = await _context.DetalleSolicitudDevolucion
                .Where(d => d.IdSolicitudDevolucion == id)
                .ToListAsync();
            if (detalles.Any())
                _context.DetalleSolicitudDevolucion.RemoveRange(detalles);

                _context.SolicitudDevolucion.Remove(solicitud);
                await _context.SaveChangesAsync();

            if (Request.Headers.ContainsKey("HX-Request"))
                {
                    Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshSolicitudesList");
                    return Content("", "text/html");
                }

            return RedirectToAction(nameof(Index));
        }





        private bool SolicitudDevolucionExists(int id)
        {
            return _context.SolicitudDevolucion.Any(e => e.IdSolicitudDevolucion == id);
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
                precioCompra = producto.PrecioCompra, // Este es el precio que necesitamos
                stockActual = producto.Cantidad // (Opcional) podríamos usar 'producto.Stock' si VProducto lo tuviera
            });
        }
    }
}
