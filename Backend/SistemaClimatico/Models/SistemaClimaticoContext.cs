using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SistemaClimatico.Models;

public partial class SistemaClimaticoContext : DbContext
{
    public SistemaClimaticoContext()
    {
    }

    public SistemaClimaticoContext(DbContextOptions<SistemaClimaticoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alerta> Alertas { get; set; }

    public virtual DbSet<BitacoraAccione> BitacoraAcciones { get; set; }

    public virtual DbSet<Comunidad> Comunidades { get; set; }

    public virtual DbSet<HistorialEvento> HistorialEventos { get; set; }

    public virtual DbSet<LecturasSensore> LecturasSensores { get; set; }

    public virtual DbSet<Sensore> Sensores { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.HasKey(e => e.IdAlerta).HasName("PK__Alertas__D2CDBC4F22BC0EDD");

            entity.HasIndex(e => e.FechaHora, "IX_Alertas_FechaHora");

            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Nivel).HasMaxLength(20);
            entity.Property(e => e.TipoFenomeno).HasMaxLength(50);
            entity.Property(e => e.ValorDetectado).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdSensorNavigation).WithMany(p => p.Alerta)
                .HasForeignKey(d => d.IdSensor)
                .HasConstraintName("FK_Alertas_Sensores");
        });

        modelBuilder.Entity<BitacoraAccione>(entity =>
        {
            entity.HasKey(e => e.IdBitacora).HasName("PK__Bitacora__ED3A1B139781935E");

            entity.HasIndex(e => e.FechaHora, "IX_BitacoraAcciones_FechaHora");

            entity.Property(e => e.Accion).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.BitacoraAcciones)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_BitacoraAcciones_Usuarios");
        });

        modelBuilder.Entity<HistorialEvento>(entity =>
        {
            entity.HasKey(e => e.IdEvento).HasName("PK__Historia__034EFC041E556DF6");

            entity.HasIndex(e => e.FechaHora, "IX_HistorialEventos_FechaHora");

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TipoFenomeno).HasMaxLength(50);

            entity.HasOne(d => d.IdAlertaNavigation).WithMany(p => p.HistorialEventos)
                .HasForeignKey(d => d.IdAlerta)
                .HasConstraintName("FK_HistorialEventos_Alertas");
        });

        modelBuilder.Entity<LecturasSensore>(entity =>
        {
            entity.HasKey(e => e.IdLectura).HasName("PK__Lecturas__84BF8B86EEC70DEF");

            entity.HasIndex(e => new { e.IdSensor, e.FechaHora }, "IX_LecturasSensores_IdSensor_FechaHora");

            entity.Property(e => e.FechaHora).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Valor).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.IdSensorNavigation).WithMany(p => p.LecturasSensores)
                .HasForeignKey(d => d.IdSensor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LecturasSensores_Sensores");
        });

        modelBuilder.Entity<Sensore>(entity =>
        {
            entity.HasKey(e => e.IdSensor).HasName("PK__Sensores__4E7D4A8C699E8D35");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(50);
            entity.Property(e => e.Ubicacion).HasMaxLength(200);
            entity.Property(e => e.UnidadMedida).HasMaxLength(30);
            entity.Property(e => e.Latitud).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitud).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.IdComunidadNavigation).WithMany(p => p.Sensores)
                .HasForeignKey(d => d.IdComunidad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sensores_Comunidades");
        });

        modelBuilder.Entity<Comunidad>(entity =>
        {
            entity.HasKey(e => e.IdComunidad);

            entity.ToTable("Comunidades");

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Latitud).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitud).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__Usuarios__5B65BF97EEA64547");

            entity.HasIndex(e => e.Correo, "UQ__Usuarios__60695A1917589D73").IsUnique();

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Correo).HasMaxLength(150);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NombreCompleto).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Rol)
                .HasMaxLength(50)
                .HasDefaultValue("Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}