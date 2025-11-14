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
    public class ProductoController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public ProductoController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: Producto
        public async Task<IActionResult> Index()
        {
            // El cambio importante es el ".Include":
            var sistemaInventarioFinalContext = _context.Producto.Include(p => p.IdCategoriaNavigation);
            return View(await sistemaInventarioFinalContext.ToListAsync());
        }

        // GET: Producto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Producto
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Producto/Create
       public IActionResult Create()
       {
        ViewBag.IdCategoria = new SelectList(_context.Categoria, "IdCategoria", "Nombre");
        return View(new Producto()); // <--- PASAR UN MODELO VACÍO
       }

        // POST: Producto/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Producto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCategoria,Nombre,Estado")] Producto producto)
        {
            if (ModelState.IsValid)
            {
                producto.Cantidad = 0;
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Si algo falla, recargamos la lista para que no dé error:
            ViewData["IdCategoria"] = new SelectList(_context.Categoria, "IdCategoria", "Nombre", producto.IdCategoria);
            return View(producto);
        }

        // GET: Producto/Edit/5
        // GET: Producto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Producto.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            // === INICIO DE CAMBIOS ===
            // Agregamos la lista de categorías. El "producto.IdCategoria" 
            // le dice al dropdown cuál valor debe seleccionar por defecto.
            ViewData["IdCategoria"] = new SelectList(_context.Categoria, "IdCategoria", "Nombre", producto.IdCategoria);
            // === FIN DE CAMBIOS ===
            return View(producto);
        }

        // POST: Producto/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Producto/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProducto,IdCategoria,Nombre,Estado")] Producto producto)
        {
            if (id != producto.IdProducto)
            {
                return NotFound();
            }

        if (ModelState.IsValid)
{
    try
    {
        var productoDB = await _context.Producto
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProducto == id);

        if (productoDB == null) return NotFound();

        // Mantener cantidad original (no editable)
        producto.Cantidad = productoDB.Cantidad;

        _context.Update(producto);
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!ProductoExists(producto.IdProducto))
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

            // === INICIO DE CAMBIOS ===
            // Si algo falla, recargamos la lista para que no dé error:
            ViewData["IdCategoria"] = new SelectList(_context.Categoria, "IdCategoria", "Nombre", producto.IdCategoria);
            // === FIN DE CAMBIOS ===
            return View(producto);
        }

        // GET: Producto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Producto
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Producto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]


public async Task<IActionResult> DeleteConfirmed(int id)
{
    // Buscar el producto
    var producto = await _context.Producto.FindAsync(id);
    if (producto == null)
    {
        return NotFound();
    }

    // VALIDACIÓN: revisar si tiene relaciones
    var tieneRelacion = _context.Inventario.Any(I => I.IdProducto == id) ||
                        _context.DetallePerdida.Any(dp => dp.IdProducto == id) ||
                        _context.DetalleVenta.Any(dv => dv.IdProducto == id) ||
                        _context.DetalleSolicitudDevolucion.Any(dd => dd.IdProducto == id);

    if (tieneRelacion)
    {
        ModelState.AddModelError("", "No se puede eliminar este producto.");
        return View(producto); // regresamos a la vista sin eliminar
    }

    // Si no tiene relaciones, eliminar
    _context.Producto.Remove(producto);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}


        private bool ProductoExists(int id)
        {
            return _context.Producto.Any(e => e.IdProducto == id);
        }
    }
}
