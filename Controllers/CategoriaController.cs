using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public CategoriaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // Helper: Detecta si la petición fue enviada por HTMX
        // HTMX envía automáticamente la cabecera "HX-Request"
        // ---------------------------------------------------------
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // ---------------------------------------------------------
        // GET: Categoria/Index
        // Vista principal SIN datos.
        // La tabla será cargada dinámicamente usando HTMX.
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            return View();
        }

        // ---------------------------------------------------------
        // GET: Categoria/GetCategoriaList
        // Devuelve la tabla de categorías (vista parcial).
        // HTMX recarga esta sección cada vez que ocurre un cambio.
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetCategoriaList()
        {
            var categorias = await _context.Categoria
                                           .OrderBy(c => c.Nombre)
                                           .AsNoTracking()
                                           .ToListAsync();

            return PartialView("_CategoriaList", categorias);
        }

        // ---------------------------------------------------------
        // GET: Categoria/Details/5
        // Muestra los detalles de una categoría.
        // Si es HTMX, se envía al modal como parcial.
        // ---------------------------------------------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categoria
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null) return NotFound();

            // Si viene desde HTMX, lo cargamos dentro del modal
            if (IsHtmxRequest())
            {
                return PartialView("Details", categoria);
            }

            return View(categoria);
        }

        // ---------------------------------------------------------
        // GET: Categoria/Create
        // Devuelve el formulario de creación.
        // Si es HTMX, se carga en un modal.
        // ---------------------------------------------------------
        public IActionResult Create()
        {
            var categoria = new Categorium();

            if (IsHtmxRequest())
            {
                return PartialView("Create", categoria);
            }

            return View(categoria);
        }

        // ---------------------------------------------------------
        // POST: Categoria/Create
        // Crea una nueva categoría.
        // HTMX usa la validación y el cierre del modal automáticamente.
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,PorcentajeGanancia")] Categorium categoria)
        {
            // Validación del modelo
            if (!ModelState.IsValid)
            {
                // Devuelve el formulario con errores al modal
                return PartialView("Create", categoria);
            }

            // Verificar duplicados por nombre
            bool existe = await _context.Categoria.AnyAsync(c => c.Nombre == categoria.Nombre);
            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe una categoría con este nombre.");
                return PartialView("Create", categoria);
            }

            categoria.Estado = "Activo";
            _context.Add(categoria);
            await _context.SaveChangesAsync();

            // Dispara eventos para:
            // * Cerrar el modal
            // * Refrescar la tabla
            Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshCategoriaList");

            // Respuesta vacía requerida por HTMX
            return Content("", "text/html");
        }

        // ---------------------------------------------------------
        // GET: Categoria/Edit/5
        // Devuelve el formulario de edición dentro del modal.
        // ---------------------------------------------------------
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return NotFound();

            if (IsHtmxRequest())
            {
                return PartialView("Edit", categoria);
            }

            return View(categoria);
        }

        // ---------------------------------------------------------
        // POST: Categoria/Edit/5
        // Guarda cambios de una categoría.
        // Maneja validación, duplicados y HTMX.
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCategoria,Nombre,PorcentajeGanancia,Estado")] Categorium categoria)
        {
            if (id != categoria.IdCategoria) return NotFound();

            if (!ModelState.IsValid)
            {
                return PartialView("Edit", categoria);
            }

            // Verifica duplicado excluyendo el actual
            bool existe = await _context.Categoria
                                        .AnyAsync(c => c.Nombre == categoria.Nombre && c.IdCategoria != id);

            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe otra categoría con este nombre.");
                return PartialView("Edit", categoria);
            }

            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Si se borró durante edición
                if (!CategoriaExists(categoria.IdCategoria)) return NotFound();
                else throw;
            }

            // Enviar señal de cierre + refrescar tabla
            Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshCategoriaList");

            return Content("", "text/html");
        }

        // ---------------------------------------------------------
        // GET: Categoria/Delete/5
        // Devuelve el formulario de confirmación.
        // HTMX lo carga en un modal.
        // ---------------------------------------------------------
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categoria
                                          .AsNoTracking()
                                          .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null) return NotFound();

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("Delete", categoria);
            }

            return View(categoria);
        }

        // ---------------------------------------------------------
        // POST: Categoria/Delete/5
        // Elimina una categoría SI NO tiene productos asociados.
        // La validación se muestra dentro del modal usando HTMX.
        // ---------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return NotFound();

            // Validar si tiene productos asociados
            bool tieneProductos = await _context.Producto
                                                .AnyAsync(p => p.IdCategoria == id);

            if (tieneProductos)
            {
                // Muestra error dentro del modal
                ModelState.AddModelError("", "No se puede eliminar esta categoría porque tiene productos relacionados.");
                return PartialView("Delete", categoria);
            }

            _context.Categoria.Remove(categoria);
            await _context.SaveChangesAsync();

            // Cerrar modal y refrescar tabla
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshCategoriaList");
                return Content("", "text/html");
            }

            return RedirectToAction(nameof(Index));
        }

        // ---------------------------------------------------------
        // Verifica si existe una categoría por ID
        // ---------------------------------------------------------
        private bool CategoriaExists(int id)
        {
            return _context.Categoria.Any(c => c.IdCategoria == id);
        }
    }
}
