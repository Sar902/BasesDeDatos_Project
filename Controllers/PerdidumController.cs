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
    public class PerdidumController : Controller
    {
        private readonly SistemaInventarioContext _context;

        public PerdidumController(SistemaInventarioContext context)
        {
            _context = context;
        }

        // GET: Perdidum
        public async Task<IActionResult> Index()
        {
            return View(await _context.Perdida.ToListAsync());
        }

        // GET: Perdidum/Details/5
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

        // GET: Perdidum/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Perdidum/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPerdida,Fecha,Total")] Perdidum perdidum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(perdidum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(perdidum);
        }

        // GET: Perdidum/Edit/5
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

        // POST: Perdidum/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPerdida,Fecha,Total")] Perdidum perdidum)
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

        // GET: Perdidum/Delete/5
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

        // POST: Perdidum/Delete/5
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
