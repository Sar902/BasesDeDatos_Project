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
    public class VProductoController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public VProductoController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // GET: VProducto
        public async Task<IActionResult> Index()
        {
            return View(await _context.VProducto.ToListAsync());
        }

        // GET: VProducto/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vProducto = await _context.VProducto
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (vProducto == null)
            {
                return NotFound();
            }

            return View(vProducto);
        }

        // GET: VProducto/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VProducto/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProducto,Nombre,IdCategoria,PrecioCompra,PrecioVenta,Cantidad,Estado")] VProducto vProducto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vProducto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vProducto);
        }

        // GET: VProducto/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vProducto = await _context.VProducto.FindAsync(id);
            if (vProducto == null)
            {
                return NotFound();
            }
            return View(vProducto);
        }

        // POST: VProducto/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProducto,Nombre,IdCategoria,PrecioCompra,PrecioVenta,Cantidad,Estado")] VProducto vProducto)
        {
            if (id != vProducto.IdProducto)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vProducto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VProductoExists(vProducto.IdProducto))
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
            return View(vProducto);
        }

        // GET: VProducto/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vProducto = await _context.VProducto
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (vProducto == null)
            {
                return NotFound();
            }

            return View(vProducto);
        }

        // POST: VProducto/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vProducto = await _context.VProducto.FindAsync(id);
            if (vProducto != null)
            {
                _context.VProducto.Remove(vProducto);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VProductoExists(int id)
        {
            return _context.VProducto.Any(e => e.IdProducto == id);
        }
    }
}
