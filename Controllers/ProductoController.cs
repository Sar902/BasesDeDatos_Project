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
    public class ProductoController : Controller
    {
        private readonly SistemaInventarioFinalContext _context;

        public ProductoController(SistemaInventarioFinalContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------
        // --------------------   CONFIGURACIÓN HTMX   ------------------------
        // -------------------------------------------------------------------

        // Método que detecta si la solicitud viene desde HTMX
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // Carga de dropdowns (solo Categoría porque Producto no tiene Proveedor)
        private async Task PopulateDropdowns(Producto producto = null)
        {
            // Carga solo las categorías ACTIVAS para la lista desplegable
            var categoriasQuery = _context.Categoria.AsNoTracking();

            if (producto == null)
            {
                // Al crear, solo mostramos las activas
                categoriasQuery = categoriasQuery.Where(c => c.Estado == "Activo");
            }
            else
            {
                // Al editar, debemos incluir la categoría actual del producto 
                // aunque esté inactiva (para que no se pierda la selección), 
                // o simplemente permitir activas si la lógica de negocio lo dicta.
                // Lo más simple suele ser:
                categoriasQuery = categoriasQuery.Where(c => c.Estado == "Activo" || c.IdCategoria == producto.IdCategoria);
            }

            ViewData["IdCategoria"] = new SelectList(
                await categoriasQuery.OrderBy(c => c.Nombre).ToListAsync(),
                "IdCategoria", "Nombre",
                producto?.IdCategoria
            );
        }

        // Carga un diccionario de categorías para mostrar nombre en lugar de ID
        private async Task LoadCategoriasMap()
        {
            ViewData["CategoriasMap"] = await _context.Categoria
                .AsNoTracking()
                .ToDictionaryAsync(c => c.IdCategoria, c => c.Nombre);
        }

        // -------------------------------------------------------------------
        // ---------------------------   LISTA   ------------------------------
        // -------------------------------------------------------------------

        // Pantalla principal de productos
        public IActionResult Index()
        {
            return View();
        }


        // Modificamos este método para aceptar los parámetros de filtros
        [HttpGet]
        public async Task<IActionResult> GetProductoList(string searchString, string sortOrder, string statusFilter)
        {
            // Carga el mapa de categorías para mostrar sus nombres
            await LoadCategoriasMap();

            // 1. Empezamos con la consulta base sobre la vista VProducto
            var query = _context.VProducto.AsNoTracking().AsQueryable();

            // 2. Filtro por Búsqueda (Nombre)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Buscamos ignorando mayúsculas/minúsculas
                query = query.Where(p => p.Nombre.Contains(searchString));
            }

            // 3. Filtro por Estado (Activo/Inactivo)
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Todos")
            {
                query = query.Where(p => p.Estado == statusFilter);
            }

            // 4. Ordenamiento (Precio y Stock)
            switch (sortOrder)
            {
                case "precio_desc":
                    // Asumiendo que VProducto tiene PrecioVenta. Si se llama distinto, cámbialo aquí.
                    query = query.OrderByDescending(p => p.PrecioVenta); 
                    break;
                case "precio_asc":
                    query = query.OrderBy(p => p.PrecioVenta);
                    break;
                case "stock_desc":
                    query = query.OrderByDescending(p => p.Cantidad);
                    break;
                case "stock_asc":
                    query = query.OrderBy(p => p.Cantidad);
                    break;
                default:
                    // Por defecto ordenamos por nombre
                    query = query.OrderBy(p => p.Nombre);
                    break;
            }

            var vProductos = await query.ToListAsync();

            return PartialView("_ProductoList", vProductos);
        }

        // -------------------------------------------------------------------
        // ---------------------------   DETALLES   ---------------------------
        // -------------------------------------------------------------------

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            await LoadCategoriasMap(); // carga los nombres de categorías

            var vProducto = await _context.VProducto
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (vProducto == null) return NotFound();

            // Si es HTMX devuelve parcial; de lo contrario, vista normal
            if (IsHtmxRequest())
                return PartialView("Details", vProducto);

            return View(vProducto);
        }

        // -------------------------------------------------------------------
        // ---------------------------   CREAR   ------------------------------
        // -------------------------------------------------------------------

        // Formulario para crear producto
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns(); // carga las categorías
            var producto = new Producto();

            // Si es HTMX, abrirá modal parcial
            if (IsHtmxRequest())
                return PartialView("Create", producto);

            return View(producto);
        }

        // Guardar producto nuevo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCategoria,Nombre")] Producto producto)
        {
            producto.Cantidad = 0; 
            
            producto.Estado = "Activo";

            ModelState.Remove("Estado");
            ModelState.Remove("Cantidad");

            if (ModelState.IsValid)
            {
                bool existe = await _context.Producto.AnyAsync(p => p.Nombre == producto.Nombre);

                if (existe)
                {
                    ModelState.AddModelError("Nombre", "Ya existe un producto con este nombre.");
                    await PopulateDropdowns(producto);
                    return PartialView("Create", producto);
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();

                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshProductoList");
                return Content("", "text/html");
            }

            await PopulateDropdowns(producto);
            return PartialView("Create", producto);
        }

        // -------------------------------------------------------------------
        // ---------------------------   EDITAR   -----------------------------
        // -------------------------------------------------------------------

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Busca el producto por ID
            var producto = await _context.Producto.FindAsync(id);
            if (producto == null) return NotFound();

            await PopulateDropdowns(producto);

            if (IsHtmxRequest())
                return PartialView("Edit", producto);

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProducto,IdCategoria,Nombre,Cantidad,Estado")] Producto producto)
        {
            if (id != producto.IdProducto) return NotFound();

            if (ModelState.IsValid)
            {
                // Verifica duplicado con otro ID
                bool existe = await _context.Producto
                    .AnyAsync(p => p.Nombre == producto.Nombre && p.IdProducto != id);

                if (existe)
                {
                    ModelState.AddModelError("Nombre", "Ya existe otro producto con este nombre.");
                    await PopulateDropdowns(producto);
                    return PartialView("Edit", producto);
                }

                try
                {
                    // Actualiza registro
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.IdProducto)) return NotFound();
                    else throw;
                }

                // Trigger de HTMX
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshProductoList");
                return Content("", "text/html");
            }

            // Si hay error, recargar dropdowns
            await PopulateDropdowns(producto);
            return PartialView("Edit", producto);
        }

        // -------------------------------------------------------------------
        // ---------------------------   ELIMINAR   ---------------------------
        // -------------------------------------------------------------------

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // Trae información desde vista VProducto
            var vProducto = await _context.VProducto
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (vProducto == null) return NotFound();

            if (IsHtmxRequest())
                return PartialView("Delete", vProducto);

            return View(vProducto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Busca el producto real en la tabla Producto
            var producto = await _context.Producto.FindAsync(id);
            if (producto == null) return NotFound();

            // Verificar relaciones existentes
            bool tieneDetallesVenta = await _context.DetalleVenta.AnyAsync(d => d.IdProducto == id);
            bool tieneInventario = await _context.Inventario.AnyAsync(i => i.IdProducto == id);

            // Si el producto está siendo usado en otras tablas, no puede eliminarse
            if (tieneDetallesVenta || tieneInventario)
            {
                string error = "No se puede eliminar: tiene ";
                if (tieneDetallesVenta) error += "ventas asociadas";
                if (tieneDetallesVenta && tieneInventario) error += " y ";
                if (tieneInventario) error += "registros de inventario";
                error += ".";

                ModelState.AddModelError("", error);

                // Se recarga la vista de confirmación usando VProducto
                var vProducto = await _context.VProducto
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.IdProducto == id);

                return PartialView("Delete", vProducto);
            }

            // Si no tiene relaciones, puede eliminarse
            _context.Producto.Remove(producto);
            await _context.SaveChangesAsync();

            if (IsHtmxRequest())
            {
                // Cierra modal y refresca lista
                Response.Headers.Add("HX-Trigger", "htmx:closeModal, refreshProductoList");
                return Content("", "text/html");
            }

            return RedirectToAction(nameof(Index));
        }

        // Verifica existencia del producto por ID
        private bool ProductoExists(int id)
        {
            return _context.Producto.Any(e => e.IdProducto == id);
        }
    }
}
