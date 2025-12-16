using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvoiceManagementAPI.Model;

namespace InvoiceManagementAPI.Data
{
    public class InvoiceDbContext : DbContext
    {
        // Constructor que recibe las opciones de configuración (cadena de conexión)
        public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options)
            : base(options)
        {
        }

        // DbSet para cada tabla - permite operaciones CRUD
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TipoCliente> TiposCliente { get; set; }
        public DbSet<TipoPago> TiposPago { get; set; }
        public DbSet<Establecimiento> Establecimientos { get; set; }
        public DbSet<PuntoEmision> PuntosEmision { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<DetalleFactura> DetallesFactura { get; set; }
        public DbSet<SecuencialFactura> SecuencialesFactura { get; set; }
        public DbSet<MasterAuditoria> Auditorias { get; set; }

        // Configuración de relaciones y reglas de la base de datos
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === CLIENTE ===
            modelBuilder.Entity<Cliente>(entity =>
            {
                // Relación: Un cliente tiene un TipoCliente
                entity.HasOne(c => c.TipoCliente)
                      .WithMany(tc => tc.Clientes)
                      .HasForeignKey(c => c.TicIdTipoc)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índices para mejorar búsquedas
                entity.HasIndex(c => c.CliCedula).IsUnique();
                entity.HasIndex(c => c.CliEmail);
            });

            // === TIPO CLIENTE ===
            modelBuilder.Entity<TipoCliente>(entity =>
            {
                entity.HasKey(tc => tc.TicIdTipoc);
            });

            // === TIPO PAGO ===
            modelBuilder.Entity<TipoPago>(entity =>
            {
                entity.HasKey(tp => tp.TipIdTipop);
            });

            // === ESTABLECIMIENTO ===
            modelBuilder.Entity<Establecimiento>(entity =>
            {
                entity.HasKey(e => e.EstRuc);
                entity.HasIndex(e => e.EstRuc).IsUnique();
            });

            // === PUNTO DE EMISIÓN ===
            modelBuilder.Entity<PuntoEmision>(entity =>
            {
                entity.HasKey(p => p.PemId);

                // Relación: Un punto de emisión pertenece a un Establecimiento
                entity.HasOne(p => p.Establecimiento)
                      .WithMany(e => e.PuntosEmision)
                      .HasForeignKey(p => p.EstRuc)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === FACTURA ===
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.HasKey(f => f.FacNumeroFactura);

                // Relación con Cliente
                entity.HasOne(f => f.Cliente)
                      .WithMany(c => c.Facturas)
                      .HasForeignKey(f => f.CliCedula)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relación con TipoPago
                entity.HasOne(f => f.TipoPago)
                      .WithMany(tp => tp.Facturas)
                      .HasForeignKey(f => f.TipIdTipop)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relación con PuntoEmision - CORREGIDA
                entity.HasOne(f => f.PuntoEmision)
                      .WithMany(p => p.Facturas)
                      .HasForeignKey(f => f.PemId)  // ✅ Ahora usa PemId (int)
                      .OnDelete(DeleteBehavior.Restrict);

                // Configurar precisión de campos decimales (12 dígitos, 2 decimales)
                entity.Property(f => f.FacSubtotalSinIva).HasPrecision(12, 2);
                entity.Property(f => f.FacSubtotalConIva).HasPrecision(12, 2);
                entity.Property(f => f.FacIva).HasPrecision(12, 2);
                entity.Property(f => f.FacTotal).HasPrecision(12, 2);

                // Índices
                entity.HasIndex(f => f.FacFechaEmision);
                entity.HasIndex(f => f.CliCedula);
            });

            // === DETALLE FACTURA ===
            modelBuilder.Entity<DetalleFactura>(entity =>
            {
                entity.HasKey(d => d.DetIdDetalle);

                // Relación: Muchos detalles pertenecen a una Factura
                entity.HasOne(d => d.Factura)
                      .WithMany(f => f.DetallesFactura)
                      .HasForeignKey(d => d.FacNumeroFactura)
                      .OnDelete(DeleteBehavior.Cascade); // Si eliminas factura, elimina sus detalles

                // Configurar precisión de decimales
                entity.Property(d => d.DetPrecioUnitario).HasPrecision(12, 2);
                entity.Property(d => d.DetSubtotal).HasPrecision(12, 2);
                entity.Property(d => d.DetPorcentajeIva).HasPrecision(5, 2);
                entity.Property(d => d.DetValorIva).HasPrecision(12, 2);
                entity.Property(d => d.DetTotal).HasPrecision(12, 2);

                // Índice para búsquedas por factura
                entity.HasIndex(d => d.FacNumeroFactura);
            });

            // === SECUENCIAL FACTURA ===
            modelBuilder.Entity<SecuencialFactura>(entity =>
            {
                entity.HasKey(s => s.SecIdSecuencial);

                // Relación con PuntoEmision
                entity.HasOne(s => s.PuntoEmision)
                      .WithMany(p => p.SecuencialesFactura)
                      .HasForeignKey(s => s.PemId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índice único para evitar duplicados por punto de emisión
                entity.HasIndex(s => s.PemId).IsUnique();
            });

            // === AUDITORÍA ===
            modelBuilder.Entity<MasterAuditoria>(entity =>
            {
                entity.HasKey(a => a.AudIdAuditoria);

                // Configurar campos JSON
                entity.Property(a => a.AudDatosAnteriores)
                      .HasColumnType("json");

                entity.Property(a => a.AudDatosNuevos)
                      .HasColumnType("json");

                // Índices para consultas de auditoría
                entity.HasIndex(a => a.AudFechaHora);
                entity.HasIndex(a => a.AudUsuario);
                entity.HasIndex(a => a.AudTablaAfectada);
            });
        }
    }
}