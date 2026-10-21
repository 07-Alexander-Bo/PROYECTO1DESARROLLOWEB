import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AlertasService } from '../../core/alertas';
import { AuthService } from '../../core/auth';
import { ComunidadesService } from '../../core/comunidades';
import { SensoresService } from '../../core/sensores';
import { GraficoLinea, PuntoGrafico } from '../../shared/grafico-linea/grafico-linea';
import { MapaSensores } from '../../shared/mapa-sensores/mapa-sensores';
import { SimuladorService } from '../../core/simulador';
import { TiempoRealService } from '../../core/tiempo-real';
import {
  AlertaDto,
  COLOR_NIVEL,
  ComunidadDto,
  ETIQUETA_TIPO_SENSOR,
  ETIQUETA_NIVEL,
  EstadoSensorDto,
  NivelAlerta,
  nombreFenomeno,
} from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, GraficoLinea, MapaSensores],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  private alertasSrv = inject(AlertasService);
  private simuladorSrv = inject(SimuladorService);
  private tiempoReal = inject(TiempoRealService);
  private sensoresSrv = inject(SensoresService);
  private comunidadesSrv = inject(ComunidadesService);
  auth = inject(AuthService);

  // Alertas pendientes que se muestran en el panel (las demas estan en la pantalla Alertas).
  readonly maxAlertasVisibles = 8;

  estados = signal<EstadoSensorDto[]>([]);
  alertasActivas = signal<AlertaDto[]>([]);
  cargando = signal(true);
  error = signal<string | null>(null);
  cambiandoSimulador = signal(false);

  // ---------- Comunidades, graficos y mapa ----------
  comunidades = signal<ComunidadDto[]>([]);
  comunidadSel = signal<number | null>(null); // null = todas
  readonly ventanas = [30, 60, 120];
  ventana = signal(60); // cuantas lecturas por grafico
  series = signal<Record<number, PuntoGrafico[]>>({});

  // Sensores que se muestran (todos, o solo los de la comunidad elegida).
  estadosVisibles = computed(() => {
    const id = this.comunidadSel();
    return id === null ? this.estados() : this.estados().filter((e) => e.idComunidad === id);
  });

  conexion = this.tiempoReal.estado;
  simuladorActivo = this.tiempoReal.simuladorActivo;

  etiquetaNivel = (n: string) => ETIQUETA_NIVEL[n as NivelAlerta] ?? n;
  colorNivel = (n: string) => COLOR_NIVEL[n as NivelAlerta] ?? 'var(--sc-text-muted)';
  nombreFenomeno = nombreFenomeno;
  etiquetaTipo = (t: string) => ETIQUETA_TIPO_SENSOR[t] ?? t;
  puntosDe = (id: number): PuntoGrafico[] => this.series()[id] ?? [];

  private subs = new Subscription();

  get esAdmin(): boolean {
    return this.auth.currentUser()?.rol === 'Administrador';
  }

  ngOnInit() {
    this.cargar();

    // Tiempo real: el servidor empuja los cambios, ya no hay polling cada 5 s.
    this.subs.add(this.tiempoReal.lectura$.subscribe((e) => this.aplicarLectura(e)));

    // El monitoreo se reinicio: se vuelve a pedir todo (queda vacio).
    this.subs.add(this.tiempoReal.monitoreoReiniciado$.subscribe(() => this.cargar()));

    this.subs.add(
      this.tiempoReal.alertaNueva$.subscribe((a) =>
        this.alertasActivas.update((lista) => [a, ...lista.filter((x) => x.idAlerta !== a.idAlerta)]),
      ),
    );

    this.subs.add(
      this.tiempoReal.alertaAtendida$.subscribe((id) =>
        this.alertasActivas.update((lista) => lista.filter((x) => x.idAlerta !== id)),
      ),
    );

    // Un sensor se creo/activo/desactivo, o la conexion se (re)establecio:
    // se vuelve a pedir el estado completo para no quedar desfasados.
    this.subs.add(this.tiempoReal.sensoresActualizados$.subscribe(() => this.cargar()));
    this.subs.add(this.tiempoReal.conectado$.subscribe(() => this.cargar()));
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
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
    this.comunidadesSrv.listar().subscribe({
      next: (data) => {
        this.comunidades.set(data);
        // Si la comunidad elegida ya no existe, se vuelve a "todas".
        const sel = this.comunidadSel();
        if (sel !== null && !data.some((c) => c.idComunidad === sel)) this.comunidadSel.set(null);
      },
      error: () => {},
    });
    this.cargarSeries();
  }

  cargarSeries() {
    this.sensoresSrv.series(this.ventana()).subscribe({
      next: (data) => {
        const mapa: Record<number, PuntoGrafico[]> = {};
        for (const s of data) mapa[s.idSensor] = s.puntos.map((p) => ({ t: Date.parse(p.fechaHora), v: p.valor }));
        this.series.set(mapa);
      },
      error: () => {},
    });
  }

  cambiarVentana(n: number) {
    this.ventana.set(n);
    this.cargarSeries();
  }

  cambiarComunidad(valor: string) {
    this.comunidadSel.set(valor ? Number(valor) : null);
  }

  // Reemplaza la tarjeta del sensor con la lectura recien llegada (o la agrega si es nuevo),
  // y suma el punto al grafico de ese sensor.
  private aplicarLectura(e: EstadoSensorDto) {
    if (e.ultimoValor !== null && e.ultimoValor !== undefined && e.fechaUltimaLectura) {
      const punto: PuntoGrafico = { t: Date.parse(e.fechaUltimaLectura), v: Number(e.ultimoValor) };
      this.series.update((m) => {
        const lista = [...(m[e.idSensor] ?? []), punto];
        return { ...m, [e.idSensor]: lista.slice(-this.ventana()) };
      });
    }

    this.estados.update((lista) => {
      const i = lista.findIndex((x) => x.idSensor === e.idSensor);
      if (i === -1) return [...lista, e];
      const copia = [...lista];
      copia[i] = e;
      return copia;
    });
  }

  alternarSimulador() {
    if (this.cambiandoSimulador()) return;
    this.cambiandoSimulador.set(true);
    const accion = this.simuladorActivo() ? this.simuladorSrv.pausar() : this.simuladorSrv.reanudar();
    // El nuevo estado llega por SignalR (SimuladorEstado) a todos los navegadores.
    accion.subscribe({
      next: () => this.cambiandoSimulador.set(false),
      error: () => this.cambiandoSimulador.set(false),
    });
  }

  nivelComunidad(): string {
    const rank: Record<string, number> = { Verde: 0, Amarillo: 1, Naranja: 2, Rojo: 3 };
    let peor = 'Verde';
    for (const e of this.estadosVisibles()) {
      if (rank[e.nivel] > rank[peor]) peor = e.nivel;
    }
    return peor;
  }
}
