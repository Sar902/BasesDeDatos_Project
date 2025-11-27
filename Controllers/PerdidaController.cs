using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSistemaInventarioNuevo.Models;
using ProyectoSistemaInventarioNuevo.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ProyectoSistemaInventarioNuevo.Controllers
{
    public class PerdidaController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public PerdidaController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // Método auxiliar para detectar peticiones HTMX
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // GET: Perdida
        public IActionResult Index()
        {
            return View();
        }

        // GET: HTMX lista de pérdidas
       public async Task<IActionResult> GetPerdidaList()
{
    // Trae las pérdidas con detalle si quieres, o solo la info resumida
    var perdidas = await _context.Perdida.ToListAsync();

    // Mapear a ViewModel
    var viewModel = perdidas.Select(p => new PerdidaRowViewModel
    {
        IdPerdida = p.IdPerdida,
        Fecha = p.Fecha,
        Motivo = p.Motivo,
        TotalPerdida = p.Total
    }).ToList();

    return PartialView("_PerdidaList", viewModel);
}


      // GET: Perdida/Create (Ahora carga los precios y el stock para JS)
public async Task<IActionResult> Create()
{
    var vm = new PerdidaCreateViewModel
    {
        Fecha = DateTime.Now,
        Items = new List<PerdidaDetalleViewModel>()
    };

    // Consulta base para productos activos y disponibles
    var productosQuery = _context.Inventario
        .Include(i => i.IdProductoNavigation)
        .Where(i => i.CantidadDisponible > 0 && i.IdProductoNavigation.Estado == "Activo");

    // Obtener el stock total y el precio de compra (del lote más antiguo) por producto
  var productosInfo = await productosQuery
    .GroupBy(i => i.IdProducto)
    .Select(g => new
    {
        IdProducto = g.Key,
        NombreProducto = g.First().IdProductoNavigation.Nombre,
        CantidadTotal = g.Sum(i => i.CantidadDisponible),
        
        // ✅ CORRECCIÓN: Calcular el precio unitario para el frontend
        PrecioUnitario = g.OrderBy(i => i.IdInventario).First().Cantidad > 0 
                         ? g.OrderBy(i => i.IdInventario).First().PrecioCompra / g.OrderBy(i => i.IdInventario).First().Cantidad 
                         : 0m
    })
    .ToDictionaryAsync(
            keySelector: x => x.IdProducto.ToString(),
            elementSelector: x => new { x.CantidadTotal, x.PrecioUnitario, x.NombreProducto }
        );

    // Cargar productos activos con cantidad disponible para el Dropdown
    ViewBag.Productos = productosInfo.Select(x => new SelectListItem
    {
        Value = x.Key,
        Text = x.Value.NombreProducto + $" (Disponibles: {x.Value.CantidadTotal})"
    }).ToList();

    // Diccionario de Stock para validación en JS
    ViewBag.ProductosStock = productosInfo.ToDictionary(
        k => k.Key,
        v => v.Value.CantidadTotal
    );

    // Diccionario de Precios para el cálculo inicial en JS
    ViewBag.ProductosPrecios = productosInfo.ToDictionary(
        k => k.Key,
        v => v.Value.PrecioUnitario
    );
    
    // Retorna View. Si es HTMX, se cargará como parcial, si se accede directo, como página completa.
    return View(vm); 
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PerdidaCreateViewModel model)
{
    // Recargar ViewBags para el caso de error
    var productosQuery = _context.Inventario
        .Include(i => i.IdProductoNavigation)
        .Where(i => i.CantidadDisponible > 0 && i.IdProductoNavigation.Estado == "Activo");

    var productosInfo = await productosQuery
        .GroupBy(i => i.IdProducto)
        .Select(g => new
        {
            IdProducto = g.Key,
            NombreProducto = g.First().IdProductoNavigation.Nombre,
            CantidadTotal = g.Sum(i => i.CantidadDisponible),
            PrecioUnitario = g.OrderBy(i => i.IdInventario).First().PrecioCompra
        })
        .ToDictionaryAsync(
            keySelector: x => x.IdProducto.ToString(),
            elementSelector: x => new { x.CantidadTotal, x.PrecioUnitario, x.NombreProducto }
        );

    ViewBag.Productos = productosInfo.Select(x => new SelectListItem
    {
        Value = x.Key,
        Text = x.Value.NombreProducto + $" (Disponibles: {x.Value.CantidadTotal})"
    }).ToList();
    ViewBag.ProductosStock = productosInfo.ToDictionary(k => k.Key, v => v.Value.CantidadTotal);
    ViewBag.ProductosPrecios = productosInfo.ToDictionary(k => k.Key, v => v.Value.PrecioUnitario);

    // Validaciones básicas
    if (!ModelState.IsValid || model.Items == null || !model.Items.Any())
    {
        if (!ModelState.IsValid)
            ModelState.AddModelError("", "Debe corregir los errores en el formulario.");

        if (model.Items == null || !model.Items.Any())
            ModelState.AddModelError("", "Debe agregar al menos un producto perdido.");

        // Si es HTMX (modal) devolvemos PartialView, si es página completa, View normal
        return IsHtmxRequest() ? PartialView("Create", model) : View(model);
    }

    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
            var perdidum = new Perdidum
            {
                Fecha = model.Fecha,
                Motivo = model.Motivo,
                Total = 0m,
                DetallePerdida = new List<DetallePerdidum>()
            };
            _context.Perdida.Add(perdidum);

            decimal total = 0m;

            foreach (var item in model.Items)
            {
                var stockTotal = productosInfo.GetValueOrDefault(item.IdProducto.ToString())?.CantidadTotal ?? 0;
                if (item.CantidadPerdida > stockTotal)
                    throw new InvalidOperationException($"No hay suficiente stock del producto ID {item.IdProducto}.");

                var inventariosAfectados = await _context.Inventario
                    .Where(i => i.IdProducto == item.IdProducto && i.CantidadDisponible > 0)
                    .OrderBy(i => i.IdInventario)
                    .ToListAsync();


// CÓDIGO CORREGIDO
var cantidadRestante = item.CantidadPerdida;
decimal costoTotalPerdida = 0m; 
decimal precioUnitarioReferencia = 0m; // Variable para guardar el último precio unitario usado (o el promedio)

foreach (var inv in inventariosAfectados)
{
    if (cantidadRestante <= 0) break;

    var cantidadADescontar = Math.Min(cantidadRestante, inv.CantidadDisponible);
    
    decimal precioUnitarioReal = inv.Cantidad > 0 ? inv.PrecioCompra / inv.Cantidad : 0m;
    
    costoTotalPerdida += cantidadADescontar * precioUnitarioReal;

    precioUnitarioReferencia = precioUnitarioReal;

    inv.CantidadDisponible -= cantidadADescontar;
    _context.Inventario.Update(inv);
    cantidadRestante -= cantidadADescontar;
}

if (cantidadRestante > 0)
    throw new InvalidOperationException($"Error concurrente: stock insuficiente del producto ID {item.IdProducto}.");

var subtotal = costoTotalPerdida; 
total += subtotal;

 decimal precioUnitarioParaDetalle = item.CantidadPerdida > 0 ? subtotal / item.CantidadPerdida : 0m;


 var detalle = new DetallePerdidum
  {
    IdProducto = item.IdProducto,
    CantidadPerdida = item.CantidadPerdida,
    PrecioCompraUnitario = precioUnitarioParaDetalle, 
    IdPerdidaNavigation = perdidum
   };
                _context.DetallePerdida.Add(detalle);
    


                var producto = await _context.Producto.FindAsync(item.IdProducto);
                if (producto != null)
                {
                    producto.Cantidad -= item.CantidadPerdida;
                    if (producto.Cantidad < 0) producto.Cantidad = 0;
                    _context.Producto.Update(producto);
                }
            }

            perdidum.Total = total;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (IsHtmxRequest())
            {
                Response.Headers.Add("HX-Trigger", "refreshPerdidaList, htmx:closeModal");
                return Content("");
            }
            else
            {
                // Página completa: redirige al índice
                TempData["SuccessMessage"] = "Pérdida registrada correctamente.";
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            foreach (var item in model.Items)
            {
                var info = productosInfo.GetValueOrDefault(item.IdProducto.ToString());
                if (info != null)
                {
                    item.NombreProducto = info.NombreProducto;
                    item.PrecioCompraUnitario = info.PrecioUnitario;
                }
            }

            ModelState.AddModelError("", "Error: No se pudo completar la pérdida. " + ex.Message);
            return IsHtmxRequest() ? PartialView("Create", model) : View(model);
        }
    }
}



// GET: Detalles pérdida
public async Task<IActionResult> Details(int id)
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
            CantidadPerdida = d.CantidadPerdida,
            NombreProducto = d.IdProductoNavigation.Nombre,
            PrecioCompraUnitario = d.PrecioCompraUnitario
        }).ToList()
    };

    return IsHtmxRequest() ? PartialView("Details", vm) : View(vm);
}



// GET Edit
// GET: Perdida/Edit/5
public async Task<IActionResult> Edit(int id)
{
    var perdida = await _context.Perdida
        .FirstOrDefaultAsync(p => p.IdPerdida == id);

    if (perdida == null) return NotFound();

    var vm = new PerdidaEditViewModel
    {
        IdPerdida = perdida.IdPerdida,
        Fecha = perdida.Fecha,
        Motivo = perdida.Motivo
    };

    return PartialView("Edit", vm);
}

// POST: Perdida/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, PerdidaEditViewModel model)
{
    if (id != model.IdPerdida) return BadRequest();

    var perdida = await _context.Perdida
        .FirstOrDefaultAsync(p => p.IdPerdida == id);

    if (perdida == null) return NotFound();

    perdida.Fecha = model.Fecha;
    perdida.Motivo = model.Motivo;

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