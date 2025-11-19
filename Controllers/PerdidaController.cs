using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;
using ProyectoSistemaInventarioNuevo.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
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
// GET: Perdida/Create
public async Task<IActionResult> Create()
{
    var vm = new PerdidaCreateViewModel
    {
        Fecha = DateTime.Now,
        Items = new List<PerdidaDetalleViewModel>
        {
            new PerdidaDetalleViewModel() // <-- este hace que aparezca un producto
        }
    };

    // Cargar productos activos con cantidad disponible
    var productosDisponibles = await _context.Inventario
        .Include(i => i.IdProductoNavigation)
        .Where(i => i.CantidadDisponible > 0 && i.IdProductoNavigation.Estado == "Activo")
        .Select(i => new SelectListItem
        {
            Value = i.IdProducto.ToString(),
            Text = i.IdProductoNavigation.Nombre + $" (Disponibles: {i.CantidadDisponible})"
        })
        .ToListAsync();

    ViewBag.Productos = productosDisponibles;

    return PartialView("Create", vm);
}

// POST: Perdida/Create
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PerdidaCreateViewModel model)
{
    // Recargar productos para mostrar nuevamente en caso de error
   var productosDisponibles = await _context.Inventario
    .Include(i => i.IdProductoNavigation) // propiedad de navegación correcta
    .Where(i => i.CantidadDisponible > 0 && i.IdProductoNavigation.Estado == "Activo")
    .Select(i => new SelectListItem
    {
        Value = i.IdProducto.ToString(),
        Text = i.IdProductoNavigation.Nombre + $" (Disponibles: {i.CantidadDisponible})"
    })
    .ToListAsync();

ViewBag.Productos = productosDisponibles;


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
        var inventario = await _context.Inventario
            .Include(i => i.IdProductoNavigation)
            .FirstOrDefaultAsync(i => i.IdProducto == item.IdProducto);

        if (inventario == null)
        {
            ModelState.AddModelError("", $"Producto con ID {item.IdProducto} no encontrado.");
            continue;
        }

        if (item.CantidadPerdida <= 0)
        {
            ModelState.AddModelError("", $"Cantidad de pérdida debe ser mayor a 0 para {inventario.IdProductoNavigation.Nombre}.");
            continue;
        }

        if (item.CantidadPerdida > inventario.CantidadDisponible)
        {
            ModelState.AddModelError("", $"No hay suficiente cantidad disponible de {inventario.IdProductoNavigation.Nombre}. Disponible: {inventario.CantidadDisponible}.");
            continue;
        }

     var subtotal = item.CantidadPerdida * inventario.PrecioCompra;

var detalle = new DetallePerdidum
{
    IdPerdida = perdidum.IdPerdida,
    IdProducto = item.IdProducto,
    CantidadPerdida = item.CantidadPerdida,
    PrecioCompraUnitario = inventario.PrecioCompra
};

_context.DetallePerdida.Add(detalle);

inventario.CantidadDisponible -= item.CantidadPerdida;
_context.Inventario.Update(inventario);

var producto = await _context.Producto.FindAsync(item.IdProducto);
if (producto != null)
{
    producto.Cantidad -= item.CantidadPerdida;

    if (producto.Cantidad < 0)
        producto.Cantidad = 0;

    _context.Producto.Update(producto);
}

total += subtotal;

    }

    perdidum.Total = total;
    _context.Perdida.Update(perdidum);
    await _context.SaveChangesAsync();

    if (!ModelState.IsValid)
        return PartialView("Create", model);

    Response.Headers.Add("HX-Trigger", "refreshPerdidaList, htmx:closeModal");
    return Content("");
}

public async Task<IActionResult> Details(int? id)
{
    if (id == null) return NotFound();

    var perdidum = await _context.Perdida
        .Include(p => p.DetallePerdida) // incluir detalles
        .ThenInclude(d => d.IdProductoNavigation) // si quieres el nombre del producto
        .FirstOrDefaultAsync(m => m.IdPerdida == id);

    if (perdidum == null) return NotFound();

    // Mapear a ViewModel
    var vm = new PerdidaCreateViewModel
    {
        IdPerdida = perdidum.IdPerdida,
        Fecha = perdidum.Fecha,
        Motivo = perdidum.Motivo,
        Total = perdidum.Total,
        Items = perdidum.DetallePerdida.Select(d => new PerdidaDetalleViewModel
        {
            IdProducto = d.IdProducto,
            NombreProducto = d.IdProductoNavigation?.Nombre ?? "Desconocido",
            CantidadPerdida = d.CantidadPerdida,
            PrecioCompraUnitario = d.PrecioCompraUnitario,
            SubtotalPerdida = d.SubtotalPerdida
        }).ToList()
    };

    if (IsHtmxRequest()) return PartialView(vm);

    return View(vm);
}

// GET Edit
public async Task<IActionResult> Edit(int id)
{
    var perdida = await _context.Perdida
        .Include(p => p.DetallePerdida)
        .ThenInclude(d => d.IdProductoNavigation)
        .FirstOrDefaultAsync(p => p.IdPerdida == id);

    if (perdida == null) return NotFound();

    var vm = new PerdidaCreateViewModel
    {
        IdPerdida = perdida.IdPerdida,
        Fecha = perdida.Fecha,
        Motivo = perdida.Motivo,
      Items = perdida.DetallePerdida.Select(d => new PerdidaDetalleViewModel
     {
    IdProducto = d.IdProducto,
    NombreProducto = d.IdProductoNavigation.Nombre,  // <--- llenar el nombre
    CantidadPerdida = d.CantidadPerdida,
    PrecioCompraUnitario = d.PrecioCompraUnitario,
    SubtotalPerdida = d.SubtotalPerdida
   }).ToList()

    };

    ViewBag.Productos = _context.Producto
        .Select(p => new { p.IdProducto, p.Nombre })
        .ToList()
        .Select(p => new SelectListItem { Value = p.IdProducto.ToString(), Text = p.Nombre })
        .ToList();

    return PartialView("Edit", vm);
}

// POST Edit
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, PerdidaCreateViewModel model)
{
    if (id != model.IdPerdida) return BadRequest();

    var perdida = await _context.Perdida
        .Include(p => p.DetallePerdida)
        .FirstOrDefaultAsync(p => p.IdPerdida == id);
    if (perdida == null) return NotFound();

    perdida.Fecha = model.Fecha;
    perdida.Motivo = model.Motivo;

    // Iterar detalles y actualizar cada uno
    foreach (var item in model.Items)
    {
        var detalle = await _context.DetallePerdida
            .FirstOrDefaultAsync(d => d.IdDetallePerdida == item.IdDetallePerdida);
        if (detalle != null)
        {
            // Lógica de stock ya está en DetallePerdidaController.Edit,
            // aquí solo podrías llamar a ese método o actualizar directamente si quieres.
        }
    }

    await _context.SaveChangesAsync();
    Response.Headers["HX-Trigger"] = "refreshPerdidaList, htmx:closeModal";
    return Content("");
}

// GET Delete
public async Task<IActionResult> Delete(int? id)
{
    if (id == null) return NotFound();
    var perdidum = await _context.Perdida.FindAsync(id);
    if (perdidum == null) return NotFound();

    if (IsHtmxRequest()) return PartialView("Delete", perdidum);
    return View(perdidum);
}

// POST Delete
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var perdida = await _context.Perdida
        .Include(p => p.DetallePerdida)
        .FirstOrDefaultAsync(p => p.IdPerdida == id);
    if (perdida == null) return NotFound();

    foreach (var det in perdida.DetallePerdida)
    {
        var inventario = await _context.Inventario
            .FirstOrDefaultAsync(i => i.IdProducto == det.IdProducto);
        if (inventario != null)
        {
            inventario.CantidadDisponible += det.CantidadPerdida;
            _context.Inventario.Update(inventario);
        }

        var producto = await _context.Producto.FindAsync(det.IdProducto);
        if (producto != null)
        {
            producto.Cantidad += det.CantidadPerdida;
            _context.Producto.Update(producto);
        }
    }

    _context.DetallePerdida.RemoveRange(perdida.DetallePerdida);
    _context.Perdida.Remove(perdida);
    await _context.SaveChangesAsync();

    Response.Headers["HX-Trigger"] = "refreshPerdidaList, htmx:closeModal";
    return Content("");
}



    }
}
