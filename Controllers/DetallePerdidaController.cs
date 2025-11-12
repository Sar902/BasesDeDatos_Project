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
    public class DetallePerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public DetallePerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: DetallePerdida
        public async Task<IActionResult> Index()
        {
            return View(await _context.DetallePerdida.ToListAsync());
        }

        // GET: DetallePerdida/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePerdidum = await _context.DetallePerdida
                .FirstOrDefaultAsync(m => m.IdDetallePerdida == id);
            if (detallePerdidum == null)
            {
                return NotFound();
            }

            return View(detallePerdidum);
        }

        // GET: DetallePerdida/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DetallePerdida/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetallePerdida,IdPerdida,IdProducto,CantidadPerdida,PrecioCompraUnitario,SubtotalPerdida")] DetallePerdidum detallePerdidum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detallePerdidum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(detallePerdidum);
        }

        // GET: DetallePerdida/Edit/5
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
            return View(detallePerdidum);
        }

        // POST: DetallePerdida/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetallePerdida,IdPerdida,IdProducto,CantidadPerdida,PrecioCompraUnitario,SubtotalPerdida")] DetallePerdidum detallePerdidum)
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
            return View(detallePerdidum);
        }

        // GET: DetallePerdida/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detallePerdidum = await _context.DetallePerdida
                .FirstOrDefaultAsync(m => m.IdDetallePerdida == id);
            if (detallePerdidum == null)
            {
                return NotFound();
            }

            return View(detallePerdidum);
        }

        // POST: DetallePerdida/Delete/5
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
