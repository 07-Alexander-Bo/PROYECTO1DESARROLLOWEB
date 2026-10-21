import {
  Component, ElementRef, OnDestroy, PLATFORM_ID, ViewChild, ViewEncapsulation,
  afterNextRender, effect, inject, input, signal,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import type * as Leaflet from 'leaflet';
import {
  COLOR_NIVEL, ComunidadDto, ETIQUETA_NIVEL, EstadoSensorDto, NivelAlerta, nombreFenomeno,
} from '../../core/models';

// Letra que identifica cada tipo de sensor dentro del marcador.
const LETRA_TIPO: Record<string, string> = {
  Temperatura: 'T', Humedad: 'H', Viento: 'V', Lluvia: 'L', NivelRio: 'R', Otro: '?',
};

function esc(texto: unknown): string {
  return String(texto ?? '').replace(/[&<>"']/g, (c) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);
}

// Mapa de la comunidad: un marcador por sensor, pintado con el color de su nivel
// de riesgo y actualizado en vivo. Usa Leaflet con los mapas de OpenStreetMap
// (necesita internet en el navegador para descargar los mosaicos del mapa).
@Component({
  selector: 'app-mapa-sensores',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mapa-sensores.html',
  styleUrl: './mapa-sensores.css',
  encapsulation: ViewEncapsulation.None, // los marcadores de Leaflet viven fuera de la plantilla de Angular
})
export class MapaSensores implements OnDestroy {
  private platformId = inject(PLATFORM_ID);

  estados = input.required<EstadoSensorDto[]>();
  comunidades = input<ComunidadDto[]>([]);
  alto = input<number>(360);

  /** Sensores activos que no se pueden ubicar (sin comunidad ni coordenadas). */
  sinUbicacion = signal(0);

  @ViewChild('contenedor', { static: true }) contenedor!: ElementRef<HTMLDivElement>;

  private L?: typeof Leaflet;
  private mapa?: Leaflet.Map;
  private capaSensores?: Leaflet.LayerGroup;
  private capaComunidades?: Leaflet.LayerGroup;
  private marcadores = new Map<number, Leaflet.Marker>();
  private firmaAjustada = '';
  private observador?: ResizeObserver;
  private destruido = false;

  constructor() {
    afterNextRender(async () => {
      if (!isPlatformBrowser(this.platformId)) return;
      const mod: any = await import('leaflet');
      if (this.destruido) return;
      this.L = mod.default ?? mod;
      this.iniciarMapa();
      this.dibujar();
    });

    // Cada vez que llegan lecturas o cambian las comunidades, se actualizan los marcadores.
    effect(() => {
      this.estados();
      this.comunidades();
      if (this.mapa) this.dibujar();
    });
  }

  ngOnDestroy() {
    this.destruido = true;
    this.observador?.disconnect();
    this.mapa?.remove();
    this.mapa = undefined;
  }

  private iniciarMapa() {
    const L = this.L!;
    const mapa = L.map(this.contenedor.nativeElement, {
      zoomControl: true,
      attributionControl: true,
    }).setView([14.94, -91.11], 14);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(mapa);

    this.capaComunidades = L.layerGroup().addTo(mapa);
    this.capaSensores = L.layerGroup().addTo(mapa);
    this.mapa = mapa;

    // Si el contenedor cambia de tamano (ventana, menu lateral), el mapa se reacomoda.
    if (typeof ResizeObserver !== 'undefined') {
      this.observador = new ResizeObserver(() => this.mapa?.invalidateSize());
      this.observador.observe(this.contenedor.nativeElement);
    }
  }

  private dibujar() {
    const L = this.L;
    const mapa = this.mapa;
    if (!L || !mapa || !this.capaSensores || !this.capaComunidades) return;

    const comunidades = this.comunidades();
    const porId = new Map(comunidades.map((c) => [c.idComunidad, c]));
    const puntos: Leaflet.LatLngExpression[] = [];

    // --- Comunidades: etiqueta fija en su centro ---
    this.capaComunidades.clearLayers();
    for (const c of comunidades) {
      const pos: Leaflet.LatLngExpression = [Number(c.latitud), Number(c.longitud)];
      L.marker(pos, {
        interactive: false,
        keyboard: false,
        icon: L.divIcon({ className: 'mapa-comunidad', html: '<span></span>', iconSize: [12, 12] }),
      })
        .bindTooltip(esc(c.nombre), { permanent: true, direction: 'bottom', offset: [0, 6], className: 'mapa-etiqueta' })
        .addTo(this.capaComunidades);
      puntos.push(pos);
    }

    // --- Sensores: un marcador por sensor, actualizado en su lugar ---
    const vistos = new Set<number>();
    let sinUbicar = 0;

    for (const e of this.estados()) {
      let lat = e.latitud ?? null;
      let lng = e.longitud ?? null;
      if ((lat === null || lng === null) && e.idComunidad != null) {
        const c = porId.get(e.idComunidad);
        if (c) { lat = Number(c.latitud); lng = Number(c.longitud); }
      }
      if (lat === null || lng === null) { sinUbicar++; continue; }

      vistos.add(e.idSensor);
      const pos: Leaflet.LatLngExpression = [Number(lat), Number(lng)];
      puntos.push(pos);

      const icono = this.icono(e);
      const popup = this.popup(e);
      const existente = this.marcadores.get(e.idSensor);

      if (existente) {
        existente.setLatLng(pos);
        existente.setIcon(icono);
        existente.getPopup()?.setContent(popup);
      } else {
        const m = L.marker(pos, { icon: icono, title: e.nombreSensor }).bindPopup(popup).addTo(this.capaSensores);
        this.marcadores.set(e.idSensor, m);
      }
    }

    // Sensores que ya no existen / se desactivaron
    for (const [id, m] of this.marcadores) {
      if (!vistos.has(id)) { this.capaSensores.removeLayer(m); this.marcadores.delete(id); }
    }
    this.sinUbicacion.set(sinUbicar);

    // El mapa se encuadra solo cuando cambia el conjunto de puntos,
    // para no pelear con la persona que esta moviendo o acercando el mapa.
    const firma = [...vistos].sort().join(',') + '|' + comunidades.map((c) => c.idComunidad).join(',');
    if (firma !== this.firmaAjustada && puntos.length > 0) {
      this.firmaAjustada = firma;
      if (puntos.length === 1) mapa.setView(puntos[0], 15);
      else mapa.fitBounds(L.latLngBounds(puntos), { padding: [40, 40], maxZoom: 16 });
    }
  }

  private icono(e: EstadoSensorDto): Leaflet.DivIcon {
    const L = this.L!;
    const nivel = e.nivel as NivelAlerta;
    const letra = LETRA_TIPO[e.tipoSensor] ?? '?';
    return L.divIcon({
      className: 'mapa-sensor-wrap',
      html: `<div class="mapa-sensor mapa-sensor-${nivel}" style="background:${COLOR_NIVEL[nivel] ?? 'gray'}">${letra}</div>`,
      iconSize: [30, 30],
      iconAnchor: [15, 15],
      popupAnchor: [0, -16],
    });
  }

  private popup(e: EstadoSensorDto): string {
    const nivel = e.nivel as NivelAlerta;
    const valor = e.ultimoValor === null || e.ultimoValor === undefined
      ? 'Sin lecturas' : `${esc(e.ultimoValor)} ${esc(e.unidadMedida)}`;
    const fenomeno = e.tipoFenomeno ? ` · ${esc(nombreFenomeno(e.tipoFenomeno))}` : '';
    return `<div class="mapa-popup">
      <strong>${esc(e.nombreSensor)}</strong>
      <div class="mapa-popup-valor">${valor}</div>
      <div class="mapa-popup-nivel" style="color:${COLOR_NIVEL[nivel] ?? 'inherit'}">${esc(ETIQUETA_NIVEL[nivel] ?? nivel)}${fenomeno}</div>
      <div class="mapa-popup-msg">${esc(e.mensaje)}</div>
    </div>`;
  }
}
