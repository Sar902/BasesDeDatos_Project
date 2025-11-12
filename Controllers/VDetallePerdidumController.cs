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
    public class VDetallePerdidumController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public VDetallePerdidumController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: VDetallePerdidum
        public async Task<IActionResult> Index()
        {
            return View(await _context.VDetallePerdidum.ToListAsync());
        }

        // GET: VDetallePerdidum/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vDetallePerdidum = await _context.VDetallePerdidum
                .FirstOrDefaultAsync(m => m.IdDetallePerdida == id);
            if (vDetallePerdidum == null)
            {
                return NotFound();
            }

            return View(vDetallePerdidum);
        }

        // GET: VDetallePerdidum/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VDetallePerdidum/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetallePerdida,IdPerdida,IdProducto,CantidadPerdida,PrecioCompraUnitario,SubtotalPerdida")] VDetallePerdidum vDetallePerdidum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vDetallePerdidum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vDetallePerdidum);
        }

        // GET: VDetallePerdidum/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vDetallePerdidum = await _context.VDetallePerdidum.FindAsync(id);
            if (vDetallePerdidum == null)
            {
                return NotFound();
            }
            return View(vDetallePerdidum);
        }

        // POST: VDetallePerdidum/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetallePerdida,IdPerdida,IdProducto,CantidadPerdida,PrecioCompraUnitario,SubtotalPerdida")] VDetallePerdidum vDetallePerdidum)
        {
            if (id != vDetallePerdidum.IdDetallePerdida)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vDetallePerdidum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VDetallePerdidumExists(vDetallePerdidum.IdDetallePerdida))
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
            return View(vDetallePerdidum);
        }

        // GET: VDetallePerdidum/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vDetallePerdidum = await _context.VDetallePerdidum
                .FirstOrDefaultAsync(m => m.IdDetallePerdida == id);
            if (vDetallePerdidum == null)
            {
                return NotFound();
            }

            return View(vDetallePerdidum);
        }

        // POST: VDetallePerdidum/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vDetallePerdidum = await _context.VDetallePerdidum.FindAsync(id);
            if (vDetallePerdidum != null)
            {
                _context.VDetallePerdidum.Remove(vDetallePerdidum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VDetallePerdidumExists(int id)
        {
            return _context.VDetallePerdidum.Any(e => e.IdDetallePerdida == id);
        }
    }
}
