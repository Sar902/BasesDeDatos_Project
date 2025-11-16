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
    public class ProveedorController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public ProveedorController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------
        // HELPER: Detecta si la petición proviene desde HTMX
        // HTMX envía el header "HX-Request" en cada llamada AJAX parcial
        // -------------------------------------------------------------
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // -------------------------------------------------------------
        // GET: Proveedor
        // Devuelve solo la vista principal (carcasa), el contenido
        // dinámico se carga con HTMX
        // -------------------------------------------------------------
        public IActionResult Index()
        {
            return View(); // La tabla y formularios se cargarán con HTMX
        }

        // -------------------------------------------------------------
        // GET: Lista de proveedores en vista parcial (para HTMX)
        // Devuelve únicamente el fragmento HTML de la tabla
        // -------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetProveedorList()
        {
            var proveedores = await _context.Proveedor
                                            .AsNoTracking()         // Optimiza consulta cuando no se modificará la entidad
                                            .OrderBy(p => p.Nombre) // Orden alfabético
                                            .ToListAsync();

            return PartialView("_ProveedorList", proveedores);
        }

        // -------------------------------------------------------------
        // GET: Devuelve el conteo de proveedores activos
        // Usado en dashboards, tarjetas o contadores dinámicos con HTMX
        // -------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetProveedorCount()
        {
            var count = await _context.Proveedor.CountAsync(p => p.Estado == "Activo");
            return Content(count.ToString(), "text/plain"); // Respuesta simple sin vista
        }

        // -------------------------------------------------------------
        // GET: Proveedor/Details/5
        // Devuelve detalles del proveedor
        // Si es HTMX devuelve un modal parcial, si no la vista completa
        // -------------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedor
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdProveedor == id);

            if (proveedor == null) return NotFound();

            // Si la petición es HTMX devolvemos solo el fragmento HTML
            if (IsHtmxRequest())
            {
                return PartialView("Details", proveedor);
            }

            // Vista completa (no modal)
            return View(proveedor);
        }

        // -------------------------------------------------------------
        // GET: Proveedor/Create
        // Devuelve el formulario de creación
        // Si es HTMX lo enviamos como modal
        // -------------------------------------------------------------
        public IActionResult Create()
        {
            if (IsHtmxRequest())
            {
                return PartialView("Create", new Proveedor());
            }
            return View();
        }

        // -------------------------------------------------------------
        // POST: Proveedor/Create
        // Validación, creación y triggers HTMX
        // -------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Contacto,Estado")] Proveedor proveedor)
        {
            // ✔ Validación: evitar duplicados por nombre
            bool existe = await _context.Proveedor.AnyAsync(p => p.Nombre == proveedor.Nombre);
            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe un proveedor con este nombre.");
            }

            // Si pasa las validaciones
            if (ModelState.IsValid)
            {
                _context.Add(proveedor);
                await _context.SaveChangesAsync();

                // Trigger HTMX para cerrar modal y refrescar tabla
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshProveedorList");

                // Devolvemos respuesta vacía (HTMX no necesita más)
                return Content("", "text/html");
            }

            // Si falló validación, devolvemos el formulario con errores
            return PartialView("Create", proveedor);
        }

        // -------------------------------------------------------------
        // GET: Proveedor/Edit/5
        // Devuelve el formulario de edición (parcial o vista completa)
        // -------------------------------------------------------------
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedor.FindAsync(id);
            if (proveedor == null) return NotFound();

            if (IsHtmxRequest())
            {
                return PartialView("Edit", proveedor);
            }
            return View(proveedor);
        }

        // -------------------------------------------------------------
        // POST: Proveedor/Edit/5
        // Actualiza proveedor y dispara eventos HTMX
        // -------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProveedor,Nombre,Contacto,Estado")] Proveedor proveedor)
        {
            if (id != proveedor.IdProveedor) return NotFound();

            // ✔ Validación: no permitir nombres duplicados
            bool existe = await _context.Proveedor.AnyAsync(
                p => p.Nombre == proveedor.Nombre && p.IdProveedor != id
            );

            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe otro proveedor con este nombre.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Si no existe ya, devolvemos NotFound
                    if (!ProveedorExists(proveedor.IdProveedor)) return NotFound();
                    else throw; // Otro error de concurrencia
                }

                // HTMX: cerrar modal y recargar tabla
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshProveedorList");
                return Content("", "text/html");
            }

            // Retornar modal con errores de validación
            return PartialView("Edit", proveedor);
        }

        // -------------------------------------------------------------
        // GET: Proveedor/Delete/5
        // Muestra confirmación de eliminación (vista o modal HTMX)
        // -------------------------------------------------------------
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedor
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdProveedor == id);

            if (proveedor == null) return NotFound();

            if (IsHtmxRequest())
            {
                return PartialView("Delete", proveedor);
            }

            return View(proveedor);
        }

        // -------------------------------------------------------------
        // POST: Proveedor/Delete/5
        // Eliminación con validación de dependencias
        // -------------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedor.FindAsync(id);
            if (proveedor == null) return NotFound();

            // ✔ Validación: No permitir eliminar si tiene inventario asociado
            bool tieneInventario = await _context.Inventario.AnyAsync(i => i.IdProveedor == id);

            if (tieneInventario)
            {
                ModelState.AddModelError("", "No se puede eliminar. Este proveedor tiene registros de inventario asociados.");
                return PartialView("Delete", proveedor); // Regresar modal con error
            }

            _context.Proveedor.Remove(proveedor);
            await _context.SaveChangesAsync();

            // ✔ Respuesta para HTMX
            if (IsHtmxRequest())
            {
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshProveedorList");
                return Content("", "text/html");
            }

            // Vista normal
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------------
        // Utilidad: Comprueba si existe proveedor
        // -------------------------------------------------------------
        private bool ProveedorExists(int id)
        {
            return _context.Proveedor.Any(e => e.IdProveedor == id);
        }
    }
}
