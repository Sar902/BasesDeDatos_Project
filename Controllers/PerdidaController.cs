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
    public class PerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public PerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: Perdida
        public async Task<IActionResult> Index()
        {
            return View(await _context.Perdida.ToListAsync());
        }

        // GET: Perdida/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var perdidum = await _context.Perdida
                .FirstOrDefaultAsync(m => m.IdPerdida == id);
            if (perdidum == null)
            {
                return NotFound();
            }

            return View(perdidum);
        }

        // GET: Perdida/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Perdida/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPerdida,Fecha,Total,Motivo,IdSolicitudDevolucion")] Perdidum perdidum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(perdidum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(perdidum);
        }

        // GET: Perdida/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var perdidum = await _context.Perdida.FindAsync(id);
            if (perdidum == null)
            {
                return NotFound();
            }
            return View(perdidum);
        }

        // POST: Perdida/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPerdida,Fecha,Total,Motivo,IdSolicitudDevolucion")] Perdidum perdidum)
        {
            if (id != perdidum.IdPerdida)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(perdidum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PerdidumExists(perdidum.IdPerdida))
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
            return View(perdidum);
        }

        // GET: Perdida/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var perdidum = await _context.Perdida
                .FirstOrDefaultAsync(m => m.IdPerdida == id);
            if (perdidum == null)
            {
                return NotFound();
            }

            return View(perdidum);
        }

        // POST: Perdida/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var perdidum = await _context.Perdida.FindAsync(id);
            if (perdidum != null)
            {
                _context.Perdida.Remove(perdidum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PerdidumExists(int id)
        {
            return _context.Perdida.Any(e => e.IdPerdida == id);
        }
    }
}
