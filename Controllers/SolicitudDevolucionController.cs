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
    public class SolicitudDevolucionController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public SolicitudDevolucionController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: SolicitudDevolucion
        public async Task<IActionResult> Index()
        {
            return View(await _context.SolicitudDevolucion.ToListAsync());
        }

        // GET: SolicitudDevolucion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var solicitudDevolucion = await _context.SolicitudDevolucion
                .FirstOrDefaultAsync(m => m.IdSolicitudDevolucion == id);
            if (solicitudDevolucion == null)
            {
                return NotFound();
            }

            return View(solicitudDevolucion);
        }

        // GET: SolicitudDevolucion/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SolicitudDevolucion/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdSolicitudDevolucion,IdInventario,Estado,Observaciones,Fecha")] SolicitudDevolucion solicitudDevolucion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(solicitudDevolucion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(solicitudDevolucion);
        }

        // GET: SolicitudDevolucion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var solicitudDevolucion = await _context.SolicitudDevolucion.FindAsync(id);
            if (solicitudDevolucion == null)
            {
                return NotFound();
            }
            return View(solicitudDevolucion);
        }

        // POST: SolicitudDevolucion/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdSolicitudDevolucion,IdInventario,Estado,Observaciones,Fecha")] SolicitudDevolucion solicitudDevolucion)
        {
            if (id != solicitudDevolucion.IdSolicitudDevolucion)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(solicitudDevolucion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SolicitudDevolucionExists(solicitudDevolucion.IdSolicitudDevolucion))
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
            return View(solicitudDevolucion);
        }

        // GET: SolicitudDevolucion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var solicitudDevolucion = await _context.SolicitudDevolucion
                .FirstOrDefaultAsync(m => m.IdSolicitudDevolucion == id);
            if (solicitudDevolucion == null)
            {
                return NotFound();
            }

            return View(solicitudDevolucion);
        }

        // POST: SolicitudDevolucion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var solicitudDevolucion = await _context.SolicitudDevolucion.FindAsync(id);
            if (solicitudDevolucion != null)
            {
                _context.SolicitudDevolucion.Remove(solicitudDevolucion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SolicitudDevolucionExists(int id)
        {
            return _context.SolicitudDevolucion.Any(e => e.IdSolicitudDevolucion == id);
        }
    }
}
