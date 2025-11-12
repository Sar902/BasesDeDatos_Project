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
    public class VVentumController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public VVentumController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: VVentum
        public async Task<IActionResult> Index()
        {
            return View(await _context.VVentum.ToListAsync());
        }

        // GET: VVentum/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vVentum = await _context.VVentum
                .FirstOrDefaultAsync(m => m.IdVenta == id);
            if (vVentum == null)
            {
                return NotFound();
            }

            return View(vVentum);
        }

        // GET: VVentum/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VVentum/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdVenta,Fecha,Total,GananciaTotal")] VVentum vVentum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vVentum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vVentum);
        }

        // GET: VVentum/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vVentum = await _context.VVentum.FindAsync(id);
            if (vVentum == null)
            {
                return NotFound();
            }
            return View(vVentum);
        }

        // POST: VVentum/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdVenta,Fecha,Total,GananciaTotal")] VVentum vVentum)
        {
            if (id != vVentum.IdVenta)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vVentum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VVentumExists(vVentum.IdVenta))
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
            return View(vVentum);
        }

        // GET: VVentum/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vVentum = await _context.VVentum
                .FirstOrDefaultAsync(m => m.IdVenta == id);
            if (vVentum == null)
            {
                return NotFound();
            }

            return View(vVentum);
        }

        // POST: VVentum/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vVentum = await _context.VVentum.FindAsync(id);
            if (vVentum != null)
            {
                _context.VVentum.Remove(vVentum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VVentumExists(int id)
        {
            return _context.VVentum.Any(e => e.IdVenta == id);
        }
    }
}
