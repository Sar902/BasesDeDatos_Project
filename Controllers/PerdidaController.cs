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

        // ---------------------------------------------------------
        // HELPER: Detectar si la petición es HTMX (Modal)
        // ---------------------------------------------------------
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // GET: Perdida
        public async Task<IActionResult> Index()
        {
            // CORRECCIÓN: Quitamos el .Include(IdInventarioNavigation) porque no existe en este modelo
            var perdidas = await _context.Perdida.ToListAsync();

            // Si es petición HTMX, devolvemos SOLO la tabla (sin layout)
            if (IsHtmxRequest())
            {
                return PartialView(perdidas);
            }

            // Si es petición normal, devolvemos la vista completa
            return View(perdidas);
        }

        // GET: Perdida/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var perdidum = await _context.Perdida
                .FirstOrDefaultAsync(m => m.IdPerdida == id);
                
            if (perdidum == null) return NotFound();

            if (IsHtmxRequest()) return PartialView(perdidum);

            return View(perdidum);
        }

        // GET: Perdida/Create
        public IActionResult Create()
        {
            if (IsHtmxRequest()) return PartialView();
            return View();
        }

        // POST: Perdida/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // CORRECCIÓN: Eliminado IdSolicitudDevolucion del Bind porque no está en el modelo
        public async Task<IActionResult> Create([Bind("IdPerdida,Fecha,Total,Motivo")] Perdidum perdidum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(perdidum);
                await _context.SaveChangesAsync();
                
                // Respuesta HTMX para cerrar modal
                if (IsHtmxRequest())
                {
                     Response.Headers.Add("HX-Trigger", "htmx:closeModal"); 
                     return Content("", "text/html");
                }
                return RedirectToAction(nameof(Index));
            }
            
            if (IsHtmxRequest()) return PartialView(perdidum);
            return View(perdidum);
        }

        // GET: Perdida/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var perdidum = await _context.Perdida.FindAsync(id);
            if (perdidum == null) return NotFound();
            
            if (IsHtmxRequest()) return PartialView(perdidum);
            return View(perdidum);
        }

        // POST: Perdida/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPerdida,Fecha,Total,Motivo")] Perdidum perdidum)
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
                    if (!PerdidumExists(perdidum.IdPerdida)) return NotFound();
                    else throw;
                }
                
                if (IsHtmxRequest())
                {
                     Response.Headers.Add("HX-Trigger", "htmx:closeModal");
                     return Content("", "text/html");
                }
                return RedirectToAction(nameof(Index));
            }
            
            if (IsHtmxRequest()) return PartialView(perdidum);
            return View(perdidum);
        }

        // GET: Perdida/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var perdidum = await _context.Perdida
                .FirstOrDefaultAsync(m => m.IdPerdida == id);
            if (perdidum == null) return NotFound();

            if (IsHtmxRequest()) return PartialView(perdidum);
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
                await _context.SaveChangesAsync();
            }
            
            if (IsHtmxRequest())
            {
                 Response.Headers.Add("HX-Trigger", "htmx:closeModal");
                 return Content("", "text/html");
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PerdidumExists(int id)
        {
            return _context.Perdida.Any(e => e.IdPerdida == id);
        }
    }
}