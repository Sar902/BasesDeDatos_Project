using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ProyectoSistemaInventarioWeb.Models;

public partial class SistemaInventarioContext : DbContext
{
    public SistemaInventarioContext()
    {
    }

    public SistemaInventarioContext(DbContextOptions<SistemaInventarioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<DetallePerdidum> DetallePerdida { get; set; }

    public virtual DbSet<DetalleSolicitudDevolucion> DetalleSolicitudDevolucions { get; set; }

    public virtual DbSet<DetalleVentum> DetalleVenta { get; set; }

    public virtual DbSet<Inventario> Inventarios { get; set; }

    public virtual DbSet<Perdidum> Perdida { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<SolicitudDevolucion> SolicitudDevolucions { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=SistemaInventario;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK__Categori__A3C02A1009E7E54B");

            entity.HasIndex(e => e.Nombre, "UQ__Categori__75E3EFCF7DEF1E25").IsUnique();

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PorcentajeGanancia).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<DetallePerdidum>(entity =>
        {
            entity.HasKey(e => e.IdDetallePerdida).HasName("PK__DetalleP__4D9DFE456451C5F5");

            entity.Property(e => e.PrecioCompraUnitario).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdPerdidaNavigation).WithMany(p => p.DetallePerdida)
                .HasForeignKey(d => d.IdPerdida)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetallePe__IdPer__5812160E");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetallePerdida)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetallePe__IdPro__59063A47");
        });

        modelBuilder.Entity<DetalleSolicitudDevolucion>(entity =>
        {
            entity.HasKey(e => e.IdDetalleSolicitudDevolucion).HasName("PK__DetalleS__C4E6C325FE54464F");

            entity.ToTable("DetalleSolicitudDevolucion");

            entity.Property(e => e.EstadoItem)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MotivoRechazo).HasColumnType("text");
            entity.Property(e => e.PrecioCompraUnitario).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleSolicitudDevolucions)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetalleSo__IdPro__6383C8BA");

            entity.HasOne(d => d.IdSolicitudDevolucionNavigation).WithMany(p => p.DetalleSolicitudDevolucions)
                .HasForeignKey(d => d.IdSolicitudDevolucion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetalleSo__IdSol__628FA481");
        });

        modelBuilder.Entity<DetalleVentum>(entity =>
        {
            entity.HasKey(e => e.IdDetalleVenta).HasName("PK__DetalleV__AAA5CEC2D364AC42");

            entity.Property(e => e.PrecioVentaUnitario).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetalleVe__IdPro__5070F446");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DetalleVe__IdVen__4F7CD00D");
        });

        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.HasKey(e => e.IdInventario).HasName("PK__Inventar__1927B20C6B71BB47");

            entity.ToTable("Inventario");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaEntrada).HasColumnType("datetime");
            entity.Property(e => e.FechaSalida).HasColumnType("datetime");
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Inventarios)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventari__IdPro__46E78A0C");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Inventarios)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventari__IdPro__47DBAE45");
        });

        modelBuilder.Entity<Perdidum>(entity =>
        {
            entity.HasKey(e => e.IdPerdida).HasName("PK__Perdida__25C1E9F8CFC0A1BD");

            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK__Producto__0988921064BCA569");

            entity.ToTable("Producto");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Producto__IdCate__403A8C7D");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor).HasName("PK__Proveedo__E8B631AFD8E74077");

            entity.ToTable("Proveedor");

            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SolicitudDevolucion>(entity =>
        {
            entity.HasKey(e => e.IdSolicitudDevolucion).HasName("PK__Solicitu__E6BAD2BE7F87585A");

            entity.ToTable("SolicitudDevolucion");

            entity.Property(e => e.Estado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Observaciones).HasColumnType("text");

            entity.HasOne(d => d.IdInventarioNavigation).WithMany(p => p.SolicitudDevolucions)
                .HasForeignKey(d => d.IdInventario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Solicitud__IdInv__5CD6CB2B");
        });

        modelBuilder.Entity<Ventum>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("PK__Venta__BC1240BD862321D3");

            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
