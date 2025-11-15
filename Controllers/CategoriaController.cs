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

        // GET: Categoria
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categoria
                                           .OrderBy(c => c.Nombre)
                                           .AsNoTracking()
                                           .ToListAsync();
            return View(categorias);
        }

        // GET: Categoria/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // GET: Categoria/Create
        public IActionResult Create()
        {
            return View(new Categorium());
        }

        // POST: Categoria/Create
      [HttpPost]
[ValidateAntiForgeryToken]

public async Task<IActionResult> Create([Bind("Nombre,PorcentajeGanancia")] Categorium categoria)
{
    if (!ModelState.IsValid) return View(categoria);

    // Validar duplicados
    bool existe = await _context.Categoria.AnyAsync(c => c.Nombre == categoria.Nombre);
    if (existe)
    {
        ModelState.AddModelError("Nombre", "Ya existe una categoría con este nombre.");
        return View(categoria);
    }

    categoria.Estado = "Activo"; // Valor por defecto
    _context.Add(categoria);      // EF Core generará IdCategoria automáticamente
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}



        // GET: Categoria/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // POST: Categoria/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCategoria,Nombre,PorcentajeGanancia,Estado")] Categorium categoria)
        {
            if (id != categoria.IdCategoria) return NotFound();

            if (!ModelState.IsValid) return View(categoria);

            // Validar duplicados (excepto esta categoría)
            bool existe = await _context.Categoria
                                        .AnyAsync(c => c.Nombre == categoria.Nombre && c.IdCategoria != id);
            if (existe)
            {
                ModelState.AddModelError("Nombre", "Ya existe otra categoría con este nombre.");
                return View(categoria);
            }

            try
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoriaExists(categoria.IdCategoria)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Categoria/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categoria
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategoria == id);

            if (categoria == null) return NotFound();

            return View(categoria);
        }

        // POST: Categoria/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return NotFound();

            // Validación: no eliminar si tiene productos relacionados
            bool tieneProductos = await _context.Producto
                                                .AnyAsync(p => p.IdCategoria == id);
            if (tieneProductos)
            {
                ModelState.AddModelError("", "No se puede eliminar esta categoría porque tiene productos relacionados.");
                return View(categoria);
            }

            _context.Categoria.Remove(categoria);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoriaExists(int id)
        {
            return _context.Categoria.Any(c => c.IdCategoria == id);
        }
    }
}
