using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;
using ProyectoSistemaInventarioNuevo.ViewModels;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class PerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public PerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // GET: Perdida
        public IActionResult Index()
        {
            return View();
        }

        // GET: HTMX lista de pérdidas
        public async Task<IActionResult> GetPerdidaList()
        {
            var perdidas = await _context.Perdida.ToListAsync();
            return PartialView("_PerdidaList", perdidas);
        }

        // GET: Perdida/Create
        public IActionResult Create()
        {
            var vm = new PerdidaCreateViewModel
            {
                Fecha = DateTime.Now,
                Items = new List<PerdidaDetalleViewModel>()
            };
            return PartialView("Create", vm);
        }

        // POST: Perdida/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PerdidaCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("Create", model);

            var perdidum = new Perdidum
            {
                Fecha = model.Fecha,
                Motivo = model.Motivo,
                Total = 0m
            };

            _context.Perdida.Add(perdidum);
            await _context.SaveChangesAsync();

            decimal total = 0m;
            foreach (var item in model.Items)
            {
                var inventario = await _context.Inventario.FirstOrDefaultAsync(i => i.IdProducto == item.IdProducto);
                if (inventario == null) continue;

                if (item.CantidadPerdida > inventario.CantidadDisponible)
                    item.CantidadPerdida = inventario.CantidadDisponible;

                var detalle = new DetallePerdidum
                {
                    IdPerdida = perdidum.IdPerdida,
                    IdProducto = item.IdProducto,
                    CantidadPerdida = item.CantidadPerdida,
                    PrecioCompraUnitario = inventario.PrecioCompra,
                    SubtotalPerdida = item.CantidadPerdida * inventario.PrecioCompra
                };

                _context.DetallePerdida.Add(detalle);
                inventario.CantidadDisponible -= item.CantidadPerdida;
                _context.Inventario.Update(inventario);

                total += detalle.SubtotalPerdida;
            }

            perdidum.Total = total;
            _context.Perdida.Update(perdidum);
            await _context.SaveChangesAsync();

            Response.Headers.Add("HX-Trigger", "refreshPerdidaList, htmx:closeModal");
            return Content("");
        }

        // GET: Perdida/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var perdidum = await _context.Perdida.FindAsync(id);
            if (perdidum == null) return NotFound();

            if (IsHtmxRequest()) return PartialView("Edit", perdidum);
            return View(perdidum);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPerdida,Fecha,Motivo,Total")] Perdidum perdidum)
        {
            if (id != perdidum.IdPerdida) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(perdidum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Perdida.Any(e => e.IdPerdida == id)) return NotFound();
                    else throw;
                }

                if (IsHtmxRequest())
                {
                    Response.Headers.Add("HX-Trigger", "htmx:closeModal");
                    return Content("", "text/html");
                }
                return RedirectToAction(nameof(Index));
            }

            if (IsHtmxRequest()) return PartialView("Edit", perdidum);
            return View(perdidum);
        }

        // GET: Perdida/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var perdidum = await _context.Perdida.FindAsync(id);
            if (perdidum == null) return NotFound();

            if (IsHtmxRequest()) return PartialView("Delete", perdidum);
            return View(perdidum);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var perdidum = await _context.Perdida.FindAsync(id);
            if (perdidum != null)
            {
                _context.Perdida.Remove(perdidum);
                await _context.SaveChangesAsync();
            }

            if (IsHtmxRequest())
            {
                Response.Headers.Add("HX-Trigger", "htmx:closeModal");
                return Content("", "text/html");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
