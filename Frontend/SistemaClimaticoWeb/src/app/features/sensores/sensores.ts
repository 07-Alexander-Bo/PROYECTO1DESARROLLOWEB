import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SensoresService } from '../../core/sensores';
import { AuthService } from '../../core/auth';
import { SensorDto } from '../../core/models';

@Component({
  selector: 'app-sensores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sensores.html',
  styleUrl: './sensores.css',
})
export class Sensores implements OnInit {
  private srv = inject(SensoresService);
  auth = inject(AuthService);

  sensores = signal<SensorDto[]>([]);
  cargando = signal(true);
  incluirInactivos = signal(false);

  // valor a registrar por sensor, mientras se escribe en el input
  valorLectura: Record<number, number | null> = {};
  guardandoLectura = signal<number | null>(null);
  mensajeLectura = signal<{ id: number; texto: string } | null>(null);

  get puedeAdministrar(): boolean {
    return this.auth.currentUser()?.rol !== 'Usuario';
  }

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.cargando.set(true);
    this.srv.listar(this.incluirInactivos()).subscribe({
      next: (data) => { this.sensores.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false),
    });
  }

  toggleIncluirInactivos() {
    this.incluirInactivos.set(!this.incluirInactivos());
    this.cargar();
  }

  activarDesactivar(s: SensorDto) {
    const accion = s.activo ? this.srv.desactivar(s.idSensor) : this.srv.activar(s.idSensor);
    accion.subscribe({ next: () => this.cargar() });
  }

  registrarLectura(s: SensorDto) {
    const valor = this.valorLectura[s.idSensor];
    if (valor === null || valor === undefined || Number.isNaN(valor)) return;

    this.guardandoLectura.set(s.idSensor);
    this.srv.registrarLectura(s.idSensor, { valor }).subscribe({
      next: () => {
        this.guardandoLectura.set(null);
        this.valorLectura[s.idSensor] = null;
        this.mensajeLectura.set({ id: s.idSensor, texto: 'Lectura registrada.' });
        setTimeout(() => this.mensajeLectura.set(null), 3000);
      },
      error: () => this.guardandoLectura.set(null),
    });
  }
}
