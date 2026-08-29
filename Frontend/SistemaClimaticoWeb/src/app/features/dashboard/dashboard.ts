import { Component, OnDestroy, OnInit, inject, signal, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AlertasService } from '../../core/alertas';
import { EstadoSensorDto, AlertaDto } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  private alertasSrv = inject(AlertasService);
  private platformId = inject(PLATFORM_ID);

  estados = signal<EstadoSensorDto[]>([]);
  alertasActivas = signal<AlertaDto[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);

  private timer?: ReturnType<typeof setInterval>;

  ngOnInit() {
    this.cargar();
    // El polling solo arranca en el navegador: en el servidor (SSR) una sola
    // carga inicial es suficiente, y un setInterval ahi quedaria sin limpiar.
    if (isPlatformBrowser(this.platformId)) {
      // Actualizacion "en tiempo real" simulada por polling cada 5s.
      // (Cuando agreguemos SignalR, esto se puede reemplazar por push del servidor.)
      this.timer = setInterval(() => this.cargar(), 5000);
    }
  }

  ngOnDestroy() {
    if (this.timer) clearInterval(this.timer);
  }

  cargar() {
    this.alertasSrv.estadoActual().subscribe({
      next: (data) => { this.estados.set(data); this.cargando.set(false); this.error.set(null); },
      error: () => { this.cargando.set(false); this.error.set('No se pudo conectar con la API. Revisa CORS y que el backend este corriendo.'); },
    });
    this.alertasSrv.listar({ atendida: false }).subscribe({
      next: (data) => this.alertasActivas.set(data),
      error: () => {},
    });
  }

  nivelComunidad(): string {
    const rank: Record<string, number> = { Verde: 0, Amarillo: 1, Naranja: 2, Rojo: 3 };
    let peor = 'Verde';
    for (const e of this.estados()) {
      if (rank[e.nivel] > rank[peor]) peor = e.nivel;
    }
    return peor;
  }
}
