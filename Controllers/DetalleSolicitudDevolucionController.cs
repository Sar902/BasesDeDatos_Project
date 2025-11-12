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
    public class DetalleSolicitudDevolucionController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public DetalleSolicitudDevolucionController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: DetalleSolicitudDevolucion
        public async Task<IActionResult> Index()
        {
            return View(await _context.DetalleSolicitudDevolucion.ToListAsync());
        }

        // GET: DetalleSolicitudDevolucion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleSolicitudDevolucion = await _context.DetalleSolicitudDevolucion
                .FirstOrDefaultAsync(m => m.IdDetalleSolicitudDevolucion == id);
            if (detalleSolicitudDevolucion == null)
            {
                return NotFound();
            }

            return View(detalleSolicitudDevolucion);
        }

        // GET: DetalleSolicitudDevolucion/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DetalleSolicitudDevolucion/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetalleSolicitudDevolucion,IdSolicitudDevolucion,IdProducto,MotivoRechazo,CantidadSolicitada,CantidadAceptada,PrecioCompraUnitario,EstadoItem")] DetalleSolicitudDevolucion detalleSolicitudDevolucion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(detalleSolicitudDevolucion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(detalleSolicitudDevolucion);
        }

        // GET: DetalleSolicitudDevolucion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleSolicitudDevolucion = await _context.DetalleSolicitudDevolucion.FindAsync(id);
            if (detalleSolicitudDevolucion == null)
            {
                return NotFound();
            }
            return View(detalleSolicitudDevolucion);
        }

        // POST: DetalleSolicitudDevolucion/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetalleSolicitudDevolucion,IdSolicitudDevolucion,IdProducto,MotivoRechazo,CantidadSolicitada,CantidadAceptada,PrecioCompraUnitario,EstadoItem")] DetalleSolicitudDevolucion detalleSolicitudDevolucion)
        {
            if (id != detalleSolicitudDevolucion.IdDetalleSolicitudDevolucion)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(detalleSolicitudDevolucion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DetalleSolicitudDevolucionExists(detalleSolicitudDevolucion.IdDetalleSolicitudDevolucion))
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
            return View(detalleSolicitudDevolucion);
        }

        // GET: DetalleSolicitudDevolucion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalleSolicitudDevolucion = await _context.DetalleSolicitudDevolucion
                .FirstOrDefaultAsync(m => m.IdDetalleSolicitudDevolucion == id);
            if (detalleSolicitudDevolucion == null)
            {
                return NotFound();
            }

            return View(detalleSolicitudDevolucion);
        }

        // POST: DetalleSolicitudDevolucion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalleSolicitudDevolucion = await _context.DetalleSolicitudDevolucion.FindAsync(id);
            if (detalleSolicitudDevolucion != null)
            {
                _context.DetalleSolicitudDevolucion.Remove(detalleSolicitudDevolucion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DetalleSolicitudDevolucionExists(int id)
        {
            return _context.DetalleSolicitudDevolucion.Any(e => e.IdDetalleSolicitudDevolucion == id);
        }
    }
}
