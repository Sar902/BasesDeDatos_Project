using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;
using ProyectoSistemaInventarioNuevo.ViewModels;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class DetallePerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public DetallePerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // ===============================
        // Traer productos activos
        // ===============================
        private async Task PopulateProductos(int? idProducto = null)
        {
            var productos = await _context.Producto
                .Where(p => p.Estado == "Activo")
                .Join(_context.Inventario,
                      p => p.IdProducto,
                      i => i.IdProducto,
                      (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
                .Where(x => x.CantidadDisponible > 0)
                .OrderBy(x => x.Nombre)
                .ToListAsync();

            var productosUnicos = productos
                .GroupBy(x => x.IdProducto)
                .Select(g => new { g.Key, g.First().Nombre, CantidadDisponible = g.Sum(p => p.CantidadDisponible) })
                .ToList();

            ViewBag.Productos = new SelectList(productosUnicos, "Key", "Nombre", idProducto);
            ViewBag.ProductosConCantidad = productosUnicos.ToDictionary(x => x.Key, x => x.CantidadDisponible);
        }

        // ===============================
        // Ajustar inventario
        // ===============================
        private async Task AjustarInventario(int idProducto, int cantidadDelta)
        {
            var producto = await _context.Producto.FindAsync(idProducto);
            var inventario = await _context.Inventario.FirstOrDefaultAsync(i => i.IdProducto == idProducto);

            if (producto == null || inventario == null) return;

            producto.Cantidad -= cantidadDelta;
            inventario.CantidadDisponible -= cantidadDelta;

            producto.Estado = producto.Cantidad > 0 ? "Activo" : "Agotado";
            inventario.Estado = inventario.CantidadDisponible > 0 ? "Activo" : "Agotado";

            _context.Producto.Update(producto);
            _context.Inventario.Update(inventario);

            await _context.SaveChangesAsync();
        }

        // ===============================
        // Index
        // ===============================
        public IActionResult Index() => View();

        // ===============================
        // Create
        // ===============================
     // GET: DetallePerdida/Create
public async Task<IActionResult> Create(int idPerdida)
{
    // Cargamos productos activos con cantidad disponible
    var productos = await _context.Producto
        .Join(_context.Inventario,
              p => p.IdProducto,
              i => i.IdProducto,
              (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
        .Where(x => x.CantidadDisponible > 0)
        .OrderBy(x => x.Nombre)
        .ToListAsync();

    ViewBag.Productos = productos;
    ViewBag.IdPerdida = idPerdida;

    // Siempre enviamos un VM inicializado
    var vm = new PerdidaDetalleViewModel();
    return PartialView("Create", vm);
}

// POST: DetallePerdida/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(int idPerdida, PerdidaDetalleViewModel vm)
{
    if (!ModelState.IsValid)
    {
        // Re-cargar productos si hubo error
        var productos = await _context.Producto
            .Join(_context.Inventario,
                  p => p.IdProducto,
                  i => i.IdProducto,
                  (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
            .Where(x => x.CantidadDisponible > 0)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        ViewBag.Productos = productos;
        ViewBag.IdPerdida = idPerdida;
        return PartialView("Create", vm);
    }

    // Validación de inventario
    var inventario = await _context.Inventario.FirstOrDefaultAsync(i => i.IdProducto == vm.IdProducto);
    if (inventario == null || inventario.CantidadDisponible < vm.CantidadPerdida)
    {
        ModelState.AddModelError("", "No hay inventario suficiente para este producto.");
        var productos = await _context.Producto
            .Join(_context.Inventario,
                  p => p.IdProducto,
                  i => i.IdProducto,
                  (p, i) => new { p.IdProducto, p.Nombre, i.CantidadDisponible })
            .Where(x => x.CantidadDisponible > 0)
            .OrderBy(x => x.Nombre)
            .ToListAsync();
        ViewBag.Productos = productos;
        ViewBag.IdPerdida = idPerdida;
        return PartialView("Create", vm);
    }

    // Guardamos detalle
   var detalle = new DetallePerdidum
{
    IdPerdida = idPerdida,
    IdProducto = vm.IdProducto,
    CantidadPerdida = vm.CantidadPerdida,
    PrecioCompraUnitario = inventario.PrecioCompra
    // SubtotalPerdida se calcula automáticamente en la BD
};

    _context.DetallePerdida.Add(detalle);

    // Ajustamos inventario
    inventario.CantidadDisponible -= vm.CantidadPerdida;
  
    _context.Inventario.Update(inventario);

    var producto = await _context.Producto.FindAsync(vm.IdProducto);
if (producto != null)
{
    producto.Cantidad -= vm.CantidadPerdida;

    if (producto.Cantidad < 0)
        producto.Cantidad = 0; // por seguridad

    _context.Producto.Update(producto);
}

    // Actualizamos total de la pérdida
    var perdida = await _context.Perdida.FindAsync(idPerdida);
    if (perdida != null)
    {
        perdida.Total = await _context.DetallePerdida
            .Where(d => d.IdPerdida == idPerdida)
            .SumAsync(d => d.SubtotalPerdida);
        _context.Perdida.Update(perdida);
    }

    await _context.SaveChangesAsync();

    // Trigger para HTMX: cerrar modal y refrescar lista
    Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
    return Content("");
}

        // ===============================
        // Edit
        // ===============================
         public async Task<IActionResult> Edit(int id)
    {
        var detalle = await _context.DetallePerdida
            .Include(d => d.IdProductoNavigation)
            .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);

        if (detalle == null) return NotFound();

        var vm = new DetallePerdidaViewModel
        {
            IdDetallePerdida = detalle.IdDetallePerdida,
            IdProducto = detalle.IdProducto,
            CantidadPerdida = detalle.CantidadPerdida
        };

        ViewBag.Productos = new SelectList(
            _context.Producto.Select(p => new { p.IdProducto, p.Nombre }),
            "IdProducto", "Nombre", detalle.IdProducto
        );

        return PartialView("Edit", vm);
    }

    // POST: Detalle/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DetallePerdidaViewModel model)
    {
        if (id != model.IdDetallePerdida) return BadRequest();

        var detalle = await _context.DetallePerdida
            .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);
        if (detalle == null) return NotFound();

        // Inventarios y productos
        var inventarioAnterior = await _context.Inventario
            .FirstOrDefaultAsync(i => i.IdProducto == detalle.IdProducto);
        var inventarioNuevo = await _context.Inventario
            .FirstOrDefaultAsync(i => i.IdProducto == model.IdProducto);

        var productoAnterior = await _context.Producto.FindAsync(detalle.IdProducto);
        var productoNuevo = await _context.Producto.FindAsync(model.IdProducto);

        if (inventarioAnterior == null || inventarioNuevo == null)
            return BadRequest("Inventario no encontrado.");

        // 1) Revertir la pérdida anterior
        inventarioAnterior.CantidadDisponible += detalle.CantidadPerdida;
        productoAnterior.Cantidad += detalle.CantidadPerdida;

        // 2) Validar stock del nuevo producto
        if (model.CantidadPerdida > inventarioNuevo.CantidadDisponible)
        {
            ModelState.AddModelError("", 
                $"No hay suficiente inventario para {productoNuevo.Nombre}. Disponible: {inventarioNuevo.CantidadDisponible}");
            
            ViewBag.Productos = new SelectList(
                _context.Producto.Select(p => new { p.IdProducto, p.Nombre }),
                "IdProducto", "Nombre", model.IdProducto
            );

            return PartialView("Edit", model);
        }

        // 3) Aplicar nuevo stock
        inventarioNuevo.CantidadDisponible -= model.CantidadPerdida;
        productoNuevo.Cantidad -= model.CantidadPerdida;

        // 4) Actualizar detalle
        detalle.IdProducto = model.IdProducto;
        detalle.CantidadPerdida = model.CantidadPerdida;

        await _context.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
        return Content("");
    }

    // GET Delete
    public async Task<IActionResult> Delete(int id)
    {
        var detalle = await _context.DetallePerdida
            .Include(d => d.IdProductoNavigation)
            .FirstOrDefaultAsync(d => d.IdDetallePerdida == id);
        if (detalle == null) return NotFound();

        ViewBag.IdPerdida = detalle.IdPerdida;
        return PartialView("Delete", detalle);
    }

    // POST Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var detalle = await _context.DetallePerdida.FindAsync(id);
        if (detalle == null) return NotFound();

        // Ajustar inventario antes de eliminar
        var inventario = await _context.Inventario
            .FirstOrDefaultAsync(i => i.IdProducto == detalle.IdProducto);
        if (inventario != null)
        {
            inventario.CantidadDisponible += detalle.CantidadPerdida;
            _context.Inventario.Update(inventario);
        }

        var producto = await _context.Producto.FindAsync(detalle.IdProducto);
        if (producto != null)
        {
            producto.Cantidad += detalle.CantidadPerdida;
            _context.Producto.Update(producto);
        }

        _context.DetallePerdida.Remove(detalle);
        await _context.SaveChangesAsync();

        // Actualizar total de la pérdida
        var perdida = await _context.Perdida.FindAsync(detalle.IdPerdida);
        if (perdida != null)
        {
            perdida.Total = await _context.DetallePerdida
                .Where(d => d.IdPerdida == detalle.IdPerdida)
                .SumAsync(d => d.SubtotalPerdida);
            _context.Perdida.Update(perdida);
            await _context.SaveChangesAsync();
        }

        Response.Headers["HX-Trigger"] = "refreshDetallePerdidaList, htmx:closeModal";
        return Content("");
    }
    }
}
