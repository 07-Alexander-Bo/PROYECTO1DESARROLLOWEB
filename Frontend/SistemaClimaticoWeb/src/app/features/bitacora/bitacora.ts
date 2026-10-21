import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BitacoraService } from '../../core/bitacora';
import { BitacoraDto, UsuarioFiltroDto } from '../../core/models';

// Texto legible de cada tipo de accion (las que no esten aqui se muestran tal cual).
const NOMBRE_ACCION: Record<string, string> = {
  ADMIN_INICIAL_CREADO: 'Administrador inicial creado',
  LOGIN: 'Inicio de sesión',
  LOGIN_FALLIDO: 'Inicio de sesión fallido',
  USUARIO_CREADO: 'Usuario creado',
  SENSOR_CREADO: 'Sensor creado',
  SENSOR_ACTUALIZADO: 'Sensor editado',
  SENSOR_ACTIVADO: 'Sensor activado',
  SENSOR_DESACTIVADO: 'Sensor desactivado',
  LECTURA_MANUAL: 'Lectura manual',
  ALERTA_ATENDIDA: 'Alerta atendida',
  SIMULADOR_PAUSADO: 'Simulador pausado',
  SIMULADOR_REANUDADO: 'Simulador reanudado',
  COMUNIDAD_CREADA: 'Comunidad creada',
  COMUNIDAD_ACTUALIZADA: 'Comunidad editada',
  COMUNIDAD_ELIMINADA: 'Comunidad eliminada',
  MONITOREO_REINICIADO: 'Monitoreo reiniciado',
};

@Component({
  selector: 'app-bitacora',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bitacora.html',
  styleUrl: './bitacora.css',
})
export class Bitacora implements OnInit {
  private srv = inject(BitacoraService);

  registros = signal<BitacoraDto[]>([]);
  usuarios = signal<UsuarioFiltroDto[]>([]);
  acciones = signal<string[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  // Filtros
  idUsuario: number | null = null;
  accion = '';
  desde = '';
  hasta = '';
  limite = 100;
  readonly limites = [50, 100, 200, 500];

  nombreAccion = (a: string) => NOMBRE_ACCION[a] ?? a;

  ngOnInit() {
    this.srv.usuarios().subscribe({ next: (u) => this.usuarios.set(u), error: () => {} });
    this.srv.acciones().subscribe({ next: (a) => this.acciones.set(a), error: () => {} });
    this.cargar();
  }

  cargar() {
    this.cargando.set(true);
    this.error.set(null);
    this.srv.listar({
      idUsuario: this.idUsuario ? Number(this.idUsuario) : null,
      accion: this.accion || null,
      desde: this.desde ? `${this.desde}T00:00:00` : null,
      hasta: this.hasta ? `${this.hasta}T23:59:59` : null,
      limite: Number(this.limite),
    }).subscribe({
      next: (data) => { this.registros.set(data); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.error.set('No se pudo cargar la bitácora.'); },
    });
  }

  limpiar() {
    this.idUsuario = null;
    this.accion = '';
    this.desde = '';
    this.hasta = '';
    this.cargar();
  }

  // Color del distintivo segun el tipo de accion.
  tono(a: string): string {
    if (a.includes('FALLIDO') || a.includes('ELIMINAD') || a.includes('REINICIADO')) return 'peligro';
    if (a.startsWith('LOGIN')) return 'neutro';
    if (a.includes('CREAD') || a.includes('ACTIVADO') || a.includes('REANUDADO')) return 'ok';
    return 'info';
  }

  // Descarga lo que se ve en pantalla como archivo CSV (abre bien en Excel).
  exportarCsv() {
    const celda = (t: unknown) => `"${String(t ?? '').replace(/"/g, '""')}"`;
    const filas = [
      ['Fecha', 'Usuario', 'Accion', 'Descripcion'],
      ...this.registros().map((r) => [r.fechaHora.replace('T', ' ').slice(0, 19), r.nombreUsuario ?? 'Sistema', r.accion, r.descripcion ?? '']),
    ];
    const csv = '﻿' + filas.map((f) => f.map(celda).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `bitacora-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
