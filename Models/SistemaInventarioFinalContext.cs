using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProyectoSistemaInventarioNuevo.Models;

public partial class SistemaInventarioFinalContext : DbContext
{
    public SistemaInventarioFinalContext()
    {
    }

    public SistemaInventarioFinalContext(DbContextOptions<SistemaInventarioFinalContext> options)
        : base(options)
    {
    }

    // DbSets con los nombres usados en los controllers
    public virtual DbSet<Categorium> Categoria { get; set; }
    public virtual DbSet<DetallePerdidum> DetallePerdida { get; set; }
    public virtual DbSet<DetalleSolicitudDevolucion> DetalleSolicitudDevolucion { get; set; }
    public virtual DbSet<DetalleVentum> DetalleVenta { get; set; }
    public virtual DbSet<Inventario> Inventario { get; set; }
    public virtual DbSet<Perdidum> Perdida { get; set; }
    public virtual DbSet<Producto> Producto { get; set; }
    public virtual DbSet<Proveedor> Proveedor { get; set; }
    public virtual DbSet<SolicitudDevolucion> SolicitudDevolucion { get; set; }
    public virtual DbSet<Ventum> Venta { get; set; }

    // Vistas
    public virtual DbSet<VDetallePerdidum> VDetallePerdidum { get; set; }
    public virtual DbSet<VDetalleVentum> VDetalleVentum { get; set; }
    public virtual DbSet<VDetalleSoltum> VDetalleSoltum { get; set; }
    public virtual DbSet<VProducto> VProducto { get; set; }
    public virtual DbSet<VVentum> VVentum { get; set; }
    public virtual DbSet<VSoltum> VSoltum { get; set; }


   protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Categoría
   modelBuilder.Entity<Categorium>(entity =>
{
    entity.HasKey(e => e.IdCategoria); // Definir PK primero
    entity.Property(e => e.IdCategoria).ValueGeneratedOnAdd(); // ID autogenerado

    entity.ToTable("Categoria");
    entity.Property(e => e.Nombre)
          .HasMaxLength(100)
          .IsUnicode(false);
    entity.Property(e => e.Estado)
          .HasMaxLength(20)
          .IsUnicode(false)
          .HasDefaultValue("Activo");
    entity.Property(e => e.PorcentajeGanancia)
          .HasColumnType("decimal(5,2)");
});


    // Inventario
    modelBuilder.Entity<Inventario>(entity =>
    {
        entity.HasKey(e => e.IdInventario);
        entity.ToTable("Inventario");

        // Relaciones
        entity.HasOne(i => i.IdProductoNavigation)
              .WithMany(p => p.Inventario)
              .HasForeignKey(i => i.IdProducto)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(i => i.IdProveedorNavigation)
              .WithMany(p => p.Inventario)
              .HasForeignKey(i => i.IdProveedor)
              .OnDelete(DeleteBehavior.Restrict);
    });

    // DetalleVenta
   // DetalleVenta
        modelBuilder.Entity<DetalleVentum>(entity =>
        {
            entity.HasKey(e => e.IdDetalleVenta);
            entity.ToTable("DetalleVenta");

            // Le decimos a EF Core que 'Subtotal' es calculado en la BD.
            // Asumimos que el cálculo es [CantidadVendida] * [PrecioVentaUnitario]
            entity.Property(e => e.Subtotal)
                  .HasComputedColumnSql("([CantidadVendida] * [PrecioVentaUnitario])");
            // === FIN DEL CAMBIO ===

            entity.HasOne(dv => dv.IdProductoNavigation)
                  .WithMany(p => p.DetalleVenta)
                  .HasForeignKey(dv => dv.IdProducto)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(dv => dv.IdVentaNavigation)
                  .WithMany(v => v.DetalleVenta)
                  .HasForeignKey(dv => dv.IdVenta)
                  .OnDelete(DeleteBehavior.Restrict);
        });

    // DetalleSolicitudDevolucion
    modelBuilder.Entity<DetalleSolicitudDevolucion>(entity =>
    {
        entity.HasKey(e => e.IdDetalleSolicitudDevolucion);
        entity.ToTable("DetalleSolicitudDevolucion");

        entity.HasOne(dsd => dsd.IdSolicitudNavigation)
              .WithMany(sd => sd.DetalleSolicitudDevolucion)
              .HasForeignKey(dsd => dsd.IdSolicitudDevolucion)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(dsd => dsd.IdProductoNavigation)
              .WithMany(p => p.DetalleSolicitudDevolucion)
              .HasForeignKey(dsd => dsd.IdProducto)
              .OnDelete(DeleteBehavior.Restrict);
    });

    // SolicitudDevolucion
    modelBuilder.Entity<SolicitudDevolucion>(entity =>
    {
    
    entity.HasKey(e => e.IdSolicitudDevolucion);
    entity.ToTable("SolicitudDevolucion");

    entity.Property(e => e.Estado)
          .HasMaxLength(20)
          .IsUnicode(false)
          .HasDefaultValue("Pendiente");

    entity.Property(e => e.Observaciones)
          .HasMaxLength(255)
          .IsUnicode(false);

    entity.Property(e => e.Fecha)
          .HasColumnType("datetime");

    // Relación con Inventario
    entity.HasOne(d => d.IdInventarioNavigation)
          .WithMany(i => i.SolicitudDevolucion)
          .HasForeignKey(d => d.IdInventario)
          .OnDelete(DeleteBehavior.Restrict);
});


    // Perdida
    modelBuilder.Entity<Perdidum>(entity =>
    {
        entity.HasKey(e => e.IdPerdida);
        entity.ToTable("Perdida");
    });

    // DetallePerdida
    modelBuilder.Entity<DetallePerdidum>(entity =>
    {
        entity.HasKey(e => e.IdDetallePerdida);
        entity.ToTable("DetallePerdida");

        entity.HasOne(dp => dp.IdPerdidaNavigation)
              .WithMany(p => p.DetallePerdida)
              .HasForeignKey(dp => dp.IdPerdida)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(dp => dp.IdProductoNavigation)
              .WithMany(p => p.DetallePerdida)
              .HasForeignKey(dp => dp.IdProducto)
              .OnDelete(DeleteBehavior.Restrict);
    });

   // Producto
modelBuilder.Entity<Producto>(entity =>
{
    entity.HasKey(e => e.IdProducto);
    entity.ToTable("Producto");

    entity.Property(e => e.Nombre)
          .HasMaxLength(100)
          .IsUnicode(false);


    entity.Property(e => e.Estado)
          .HasMaxLength(20)
          .IsUnicode(false)
          .HasDefaultValue("Activo");

    // Relación con Categoría
    entity.HasOne(p => p.IdCategoriaNavigation)
          .WithMany(c => c.Producto)
          .HasForeignKey(p => p.IdCategoria)
          .OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<Ventum>(entity =>
{
    entity.HasKey(e => e.IdVenta);
    entity.ToTable("Venta");
});

    // Proveedor
    modelBuilder.Entity<Proveedor>(entity =>
    {
        entity.HasKey(e => e.IdProveedor);
        entity.ToTable("Proveedor");
    });

modelBuilder.Entity<VDetallePerdidum>(entity =>
{
    // Definir la PK ficticia
    entity.HasKey(e => e.IdDetallePerdida);

    entity.ToView("v_DetallePerdida", "dbo");

    entity.Property(e => e.PrecioCompraUnitario).HasColumnType("decimal(18,4)");
    entity.Property(e => e.SubtotalPerdida).HasColumnType("decimal(18,2)");
});

modelBuilder.Entity<VDetalleVentum>(entity =>
{
    // Definir la PK ficticia
    entity.HasKey(e => e.IdDetalleVenta);

    entity.ToView("v_DetalleVenta", "dbo");

    entity.Property(e => e.PrecioCompraUnitario).HasColumnType("decimal(18,4)");
    entity.Property(e => e.PrecioVentaUnitario).HasColumnType("decimal(18,4)");
    entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
    entity.Property(e => e.GananciaSubtotal).HasColumnType("decimal(18,2)");
});

modelBuilder.Entity<VDetalleSoltum>(entity =>
{
    // Definir la PK ficticia
    entity.HasKey(e => e.IdDetalleSolicitudDevolucion);

    entity.ToView("v_DetalleSolicitud", "dbo");
});

modelBuilder.Entity<VProducto>(entity =>
{
    // Definir la PK ficticia
    entity.HasKey(e => e.IdProducto);

    entity.ToView("v_Producto", "dbo");

    entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18,4)");
    entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18,4)");
});

modelBuilder.Entity<VVentum>(entity =>
{
    // Definir la PK ficticia
    entity.HasKey(e => e.IdVenta);

    entity.ToView("v_Venta", "dbo");

    entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
    entity.Property(e => e.GananciaTotal).HasColumnType("decimal(18,2)");
});

modelBuilder.Entity<VSoltum>(entity =>
{
    // Definir la PK ficticia
    entity.HasKey(e => e.IdSolicitudDevolucion);

    entity.ToView("v_Solicitud", "dbo");

    entity.Property(e => e.IdSolicitudDevolucion);
    entity.Property(e => e.Proveedor);
    entity.Property(e => e.Observaciones);
    entity.Property(e => e.Fecha);
    entity.Property(e => e.Estado);

});

}

}
