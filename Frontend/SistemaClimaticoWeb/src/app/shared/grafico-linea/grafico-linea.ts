import {
  Component, ElementRef, OnDestroy, PLATFORM_ID, afterNextRender, computed, inject, input, signal,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { COLOR_NIVEL, ETIQUETA_NIVEL, NivelAlerta, UMBRALES_GRAFICO } from '../../core/models';

export interface PuntoGrafico {
  /** Momento de la lectura (ms desde 1970). */
  t: number;
  v: number;
}

let contador = 0;
const ALTO = 190;
const MARGEN = { izq: 44, der: 16, arr: 14, abajo: 26 };

// Grafico de lineas de UNA serie (un sensor), dibujado en SVG sin librerias.
// - Se actualiza solo cuando cambia la lista de puntos (tiempo real).
// - Muestra las lineas de umbral de riesgo que caen dentro del rango visible.
// - Al pasar el mouse (o tocar) muestra una guia vertical con el valor exacto.
@Component({
  selector: 'app-grafico-linea',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './grafico-linea.html',
  styleUrl: './grafico-linea.css',
})
export class GraficoLinea implements OnDestroy {
  private host = inject<ElementRef<HTMLElement>>(ElementRef);
  private platformId = inject(PLATFORM_ID);

  puntos = input.required<PuntoGrafico[]>();
  unidad = input<string>('');
  /** Tipo de sensor (Temperatura, Lluvia...): decide que umbrales se dibujan. */
  tipo = input<string>('');
  titulo = input<string>('');

  readonly alto = ALTO;
  readonly idRelleno = `rel-grafico-${++contador}`;
  ancho = signal(480);
  indiceHover = signal<number | null>(null);

  private observador?: ResizeObserver;

  constructor() {
    afterNextRender(() => {
      if (!isPlatformBrowser(this.platformId) || typeof ResizeObserver === 'undefined') return;
      const el = this.host.nativeElement;
      this.ancho.set(Math.max(240, Math.floor(el.clientWidth)));
      this.observador = new ResizeObserver(() => this.ancho.set(Math.max(240, Math.floor(el.clientWidth))));
      this.observador.observe(el);
    });
  }

  ngOnDestroy() {
    this.observador?.disconnect();
  }

  // ---------- Geometria ----------
  private areaW = computed(() => this.ancho() - MARGEN.izq - MARGEN.der);
  private areaH = ALTO - MARGEN.arr - MARGEN.abajo;

  private dominio = computed(() => {
    const pts = this.puntos();
    if (pts.length === 0) return { minY: 0, maxY: 1, minT: 0, maxT: 1 };
    let minY = Infinity, maxY = -Infinity;
    for (const p of pts) {
      if (p.v < minY) minY = p.v;
      if (p.v > maxY) maxY = p.v;
    }
    let span = maxY - minY;
    if (span < 1e-6) span = Math.max(1, Math.abs(maxY) * 0.1);
    const pad = span * 0.18;
    minY -= pad;
    maxY += pad;
    const minT = pts[0].t;
    const maxT = pts[pts.length - 1].t === minT ? minT + 1 : pts[pts.length - 1].t;
    return { minY, maxY, minT, maxT };
  });

  private x = (t: number) => {
    const d = this.dominio();
    return MARGEN.izq + ((t - d.minT) / (d.maxT - d.minT)) * this.areaW();
  };
  private y = (v: number) => {
    const d = this.dominio();
    return MARGEN.arr + (1 - (v - d.minY) / (d.maxY - d.minY)) * this.areaH;
  };

  coords = computed(() => this.puntos().map((p) => ({ x: this.x(p.t), y: this.y(p.v), p })));

  linea = computed(() => {
    const c = this.coords();
    if (c.length === 0) return '';
    return c.map((q, i) => `${i === 0 ? 'M' : 'L'}${q.x.toFixed(1)},${q.y.toFixed(1)}`).join(' ');
  });

  area = computed(() => {
    const c = this.coords();
    if (c.length < 2) return '';
    const base = MARGEN.arr + this.areaH;
    return `${this.linea()} L${c[c.length - 1].x.toFixed(1)},${base} L${c[0].x.toFixed(1)},${base} Z`;
  });

  ultimo = computed(() => {
    const c = this.coords();
    return c.length ? c[c.length - 1] : null;
  });

  // Marcas del eje Y (4-5 valores "redondos").
  ticksY = computed(() => {
    const d = this.dominio();
    const rango = d.maxY - d.minY;
    const bruto = rango / 4;
    const mag = Math.pow(10, Math.floor(Math.log10(bruto)));
    const norm = bruto / mag;
    const paso = (norm < 1.5 ? 1 : norm < 3 ? 2 : norm < 7 ? 5 : 10) * mag;
    const res: { y: number; texto: string }[] = [];
    for (let v = Math.ceil(d.minY / paso) * paso; v <= d.maxY; v += paso) {
      res.push({ y: this.y(v), texto: this.formato(v, paso) });
    }
    return res;
  });

  // Marcas del eje X: primera, central y ultima hora.
  ticksX = computed(() => {
    const pts = this.puntos();
    if (pts.length < 2) return [];
    const idx = [0, Math.floor((pts.length - 1) / 2), pts.length - 1];
    // Si el grafico abarca pocos minutos, las horas llevan segundos para poder distinguirlas.
    const conSegundos = pts[pts.length - 1].t - pts[0].t < 10 * 60 * 1000;
    return idx.map((i, k) => ({
      x: this.x(pts[i].t),
      texto: this.hora(pts[i].t, conSegundos),
      anchor: k === 0 ? 'start' : k === 2 ? 'end' : 'middle',
    }));
  });

  // Umbrales de riesgo que caen dentro del rango visible.
  umbrales = computed(() => {
    const d = this.dominio();
    return (UMBRALES_GRAFICO[this.tipo()] ?? [])
      .filter((u) => u.valor > d.minY && u.valor < d.maxY)
      .map((u) => ({
        y: this.y(u.valor),
        color: COLOR_NIVEL[u.nivel as NivelAlerta],
        etiqueta: `${ETIQUETA_NIVEL[u.nivel as NivelAlerta]} ${u.valor}`,
      }));
  });

  resumen = computed(() => {
    const pts = this.puntos();
    if (pts.length === 0) return 'Sin datos todavía.';
    const vs = pts.map((p) => p.v);
    const u = this.unidad();
    return `${this.titulo()}: ${pts.length} lecturas, último ${vs[vs.length - 1]} ${u}, mínimo ${Math.min(...vs)} ${u}, máximo ${Math.max(...vs)} ${u}.`;
  });

  // ---------- Hover ----------
  hover = computed(() => {
    const i = this.indiceHover();
    const c = this.coords();
    if (i === null || !c[i]) return null;
    const q = c[i];
    // El globo se acomoda para no salirse del grafico.
    const derecha = q.x > this.ancho() * 0.62;
    return { ...q, derecha, hora: this.hora(q.p.t, true) };
  });

  mover(ev: MouseEvent | PointerEvent) {
    const c = this.coords();
    if (c.length === 0) return;
    const svg = (ev.currentTarget as SVGElement).ownerSVGElement ?? (ev.currentTarget as SVGSVGElement);
    const rect = svg.getBoundingClientRect();
    const px = ((ev.clientX - rect.left) / rect.width) * this.ancho();
    let mejor = 0, dist = Infinity;
    for (let i = 0; i < c.length; i++) {
      const dd = Math.abs(c[i].x - px);
      if (dd < dist) { dist = dd; mejor = i; }
    }
    this.indiceHover.set(mejor);
  }

  salir() {
    this.indiceHover.set(null);
  }

  private hora(t: number, segundos = false): string {
    const f = new Date(t);
    const dos = (n: number) => String(n).padStart(2, '0');
    return segundos ? `${dos(f.getHours())}:${dos(f.getMinutes())}:${dos(f.getSeconds())}`
                    : `${dos(f.getHours())}:${dos(f.getMinutes())}`;
  }

  private formato(v: number, paso: number): string {
    const dec = paso >= 1 ? 0 : Math.min(3, Math.ceil(-Math.log10(paso)));
    return v.toFixed(dec);
  }
}
