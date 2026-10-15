import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { HistorialService } from '../../core/historial';
import { TiempoRealService } from '../../core/tiempo-real';
import { HistorialEventoDto, nombreFenomeno } from '../../core/models';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './historial.html',
  styleUrl: './historial.css',
})
export class Historial implements OnInit, OnDestroy {
  private srv = inject(HistorialService);
  private tiempoReal = inject(TiempoRealService);
  private subs = new Subscription();

  // 'IncendioForestal' -> 'Incendio forestal', 'Sequia' -> 'Sequía', etc.
  nombreFenomeno = nombreFenomeno;

  eventos = signal<HistorialEventoDto[]>([]);
  cargando = signal(true);

  ngOnInit() {
    this.cargar();

    // Cada alerta nueva genera un evento en el historial: se vuelve a pedir la lista
    // (tambien al reconectar, por si se perdio algo mientras no habia conexion).
    this.subs.add(this.tiempoReal.alertaNueva$.subscribe(() => this.cargar()));
    this.subs.add(this.tiempoReal.conectado$.subscribe(() => this.cargar()));
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  cargar() {
    this.srv.listar().subscribe({
      next: (data) => { this.eventos.set(data); this.cargando.set(false); },
      error: () => this.cargando.set(false),
    });
  }
}
