using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioWeb.Models;

namespace ProyectoSistemaInventarioWeb.Controllers
{
    public class DetallePerdidumController : Controller
    {
        private readonly SistemaInventarioContext _context;

        public DetallePerdidumController(SistemaInventarioContext context)
        {
            _context = context;
        }

        // GET: DetallePerdidum
        public async Task<IActionResult> Index()
        {
            var sistemaInventarioContext = _context.DetallePerdida.Include(d => d.IdPerdidaNavigation).Include(d => d.IdProductoNavigation);
            return View(await sistemaInventarioContext.ToListAsync());
        }

        // GET: DetallePerdidum/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePerdidum = await _context.DetallePerdida
                .Include(d => d.IdPerdidaNavigation)
                .Include(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(m => m.IdDetallePerdida == id);
            if (detallePerdidum == null)
            {
                return NotFound();
            }

            return View(detallePerdidum);
        }

        // GET: DetallePerdidum/Create
        public IActionResult Create()
        {
            ViewData["IdPerdida"] = new SelectList(_context.Perdida, "IdPerdida", "IdPerdida");
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "IdProducto");
            return View();
        }

        // POST: DetallePerdidum/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetallePerdida,IdPerdida,IdProducto,CantidadPerdida,PrecioCompraUnitario")] DetallePerdidum detallePerdidum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detallePerdidum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdPerdida"] = new SelectList(_context.Perdida, "IdPerdida", "IdPerdida", detallePerdidum.IdPerdida);
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "IdProducto", detallePerdidum.IdProducto);
            return View(detallePerdidum);
        }

        // GET: DetallePerdidum/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePerdidum = await _context.DetallePerdida.FindAsync(id);
            if (detallePerdidum == null)
            {
                return NotFound();
            }
            ViewData["IdPerdida"] = new SelectList(_context.Perdida, "IdPerdida", "IdPerdida", detallePerdidum.IdPerdida);
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "IdProducto", detallePerdidum.IdProducto);
            return View(detallePerdidum);
        }

        // POST: DetallePerdidum/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetallePerdida,IdPerdida,IdProducto,CantidadPerdida,PrecioCompraUnitario")] DetallePerdidum detallePerdidum)
        {
            if (id != detallePerdidum.IdDetallePerdida)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detallePerdidum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DetallePerdidumExists(detallePerdidum.IdDetallePerdida))
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
            ViewData["IdPerdida"] = new SelectList(_context.Perdida, "IdPerdida", "IdPerdida", detallePerdidum.IdPerdida);
            ViewData["IdProducto"] = new SelectList(_context.Productos, "IdProducto", "IdProducto", detallePerdidum.IdProducto);
            return View(detallePerdidum);
        }

        // GET: DetallePerdidum/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePerdidum = await _context.DetallePerdida
                .Include(d => d.IdPerdidaNavigation)
                .Include(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(m => m.IdDetallePerdida == id);
            if (detallePerdidum == null)
            {
                return NotFound();
            }

            return View(detallePerdidum);
        }

        // POST: DetallePerdidum/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detallePerdidum = await _context.DetallePerdida.FindAsync(id);
            if (detallePerdidum != null)
            {
                _context.DetallePerdida.Remove(detallePerdidum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DetallePerdidumExists(int id)
        {
            return _context.DetallePerdida.Any(e => e.IdDetallePerdida == id);
        }
    }
}
