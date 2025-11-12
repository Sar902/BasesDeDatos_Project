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
    public class VDetalleVentumController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public VDetalleVentumController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: VDetalleVentum
        public async Task<IActionResult> Index()
        {
            return View(await _context.VDetalleVentum.ToListAsync());
        }

        // GET: VDetalleVentum/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vDetalleVentum = await _context.VDetalleVentum
                .FirstOrDefaultAsync(m => m.IdDetalleVenta == id);
            if (vDetalleVentum == null)
            {
                return NotFound();
            }

            return View(vDetalleVentum);
        }

        // GET: VDetalleVentum/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VDetalleVentum/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdDetalleVenta,IdVenta,IdProducto,CantidadVendida,PrecioVentaUnitario,PrecioCompraUnitario,Subtotal,GananciaSubtotal")] VDetalleVentum vDetalleVentum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vDetalleVentum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vDetalleVentum);
        }

        // GET: VDetalleVentum/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vDetalleVentum = await _context.VDetalleVentum.FindAsync(id);
            if (vDetalleVentum == null)
            {
                return NotFound();
            }
            return View(vDetalleVentum);
        }

        // POST: VDetalleVentum/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdDetalleVenta,IdVenta,IdProducto,CantidadVendida,PrecioVentaUnitario,PrecioCompraUnitario,Subtotal,GananciaSubtotal")] VDetalleVentum vDetalleVentum)
        {
            if (id != vDetalleVentum.IdDetalleVenta)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vDetalleVentum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VDetalleVentumExists(vDetalleVentum.IdDetalleVenta))
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
            return View(vDetalleVentum);
        }

        // GET: VDetalleVentum/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vDetalleVentum = await _context.VDetalleVentum
                .FirstOrDefaultAsync(m => m.IdDetalleVenta == id);
            if (vDetalleVentum == null)
            {
                return NotFound();
            }

            return View(vDetalleVentum);
        }

        // POST: VDetalleVentum/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vDetalleVentum = await _context.VDetalleVentum.FindAsync(id);
            if (vDetalleVentum != null)
            {
                _context.VDetalleVentum.Remove(vDetalleVentum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VDetalleVentumExists(int id)
        {
            return _context.VDetalleVentum.Any(e => e.IdDetalleVenta == id);
        }
    }
}
