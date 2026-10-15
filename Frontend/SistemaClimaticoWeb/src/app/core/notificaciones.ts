import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AlertaDto, ETIQUETA_NIVEL, NivelAlerta, nombreFenomeno } from './models';
import { TiempoRealService } from './tiempo-real';

export interface Toast {
  id: number;
  nivel: NivelAlerta;
  titulo: string;
  detalle: string;
  sensor: string;
  hora: Date;
}

interface Pitido {
  frecuencia: number; // Hz
  duracion: number; // segundos
  pausa: number; // segundos de silencio despues del pitido
}

interface PatronSonoro {
  onda: OscillatorType;
  volumen: number; // 0..1
  pitidos: Pitido[];
}

// Cuanto mas grave el nivel, mas insistente el sonido.
const PATRONES: Record<NivelAlerta, PatronSonoro> = {
  Verde: { onda: 'sine', volumen: 0.1, pitidos: [] },
  // Precaucion: un pitido suave
  Amarillo: { onda: 'sine', volumen: 0.18, pitidos: [{ frecuencia: 660, duracion: 0.22, pausa: 0 }] },
  // Alerta: dos pitidos
  Naranja: {
    onda: 'triangle',
    volumen: 0.2,
    pitidos: [
      { frecuencia: 760, duracion: 0.16, pausa: 0.08 },
      { frecuencia: 760, duracion: 0.16, pausa: 0 },
    ],
  },
  // Emergencia: sirena de cuatro tonos
  Rojo: {
    onda: 'square',
    volumen: 0.12,
    pitidos: [
      { frecuencia: 880, duracion: 0.2, pausa: 0.04 },
      { frecuencia: 620, duracion: 0.2, pausa: 0.04 },
      { frecuencia: 880, duracion: 0.2, pausa: 0.04 },
      { frecuencia: 620, duracion: 0.2, pausa: 0 },
    ],
  },
};

// Cuanto tiempo se queda visible cada aviso (ms).
const DURACION_TOAST: Record<NivelAlerta, number> = {
  Verde: 5000,
  Amarillo: 8000,
  Naranja: 12000,
  Rojo: 30000,
};

const MAX_TOASTS = 5;
const CLAVE_SILENCIO = 'sc_silenciado';

// Avisos de alerta: cuadro visual (toast) + sonido. Se activa desde el Shell,
// asi funciona en cualquier pantalla de la aplicacion.
@Injectable({ providedIn: 'root' })
export class NotificacionesService {
  private platformId = inject(PLATFORM_ID);
  private tiempoReal = inject(TiempoRealService);

  readonly toasts = signal<Toast[]>([]);
  readonly silenciado = signal(false);

  private contexto?: AudioContext;
  private iniciado = false;
  private contador = 0;
  private temporizadores = new Map<number, ReturnType<typeof setTimeout>>();

  iniciar() {
    if (!isPlatformBrowser(this.platformId) || this.iniciado) return;
    this.iniciado = true;

    this.silenciado.set(this.leerSilenciado());

    // Safari y otros navegadores solo dejan sonar el audio despues de un gesto del usuario:
    // al primer clic o tecla se "desbloquea" el audio.
    const desbloquear = () => void this.obtenerContexto()?.resume();
    document.addEventListener('pointerdown', desbloquear, { once: true });
    document.addEventListener('keydown', desbloquear, { once: true });

    this.tiempoReal.alertaNueva$.subscribe((alerta) => this.avisar(alerta));
  }

  /** Cierra un aviso (clic en la X o en el aviso). */
  cerrar(id: number) {
    const t = this.temporizadores.get(id);
    if (t) clearTimeout(t);
    this.temporizadores.delete(id);
    this.toasts.update((lista) => lista.filter((x) => x.id !== id));
  }

  alternarSilencio() {
    const nuevo = !this.silenciado();
    this.silenciado.set(nuevo);
    this.guardarSilenciado(nuevo);
    // Al activar el sonido se reproduce un pitido de prueba (y se desbloquea el audio).
    if (!nuevo) this.reproducir('Amarillo');
  }

  private avisar(alerta: AlertaDto) {
    const id = ++this.contador;
    const toast: Toast = {
      id,
      nivel: alerta.nivel,
      titulo: `${ETIQUETA_NIVEL[alerta.nivel] ?? alerta.nivel} · ${nombreFenomeno(alerta.tipoFenomeno)}`,
      detalle: alerta.mensaje,
      sensor: alerta.nombreSensor ?? 'Sensor',
      hora: new Date(),
    };

    // Maximo MAX_TOASTS visibles: si hay mas, se descartan los mas viejos.
    this.toasts.update((lista) => {
      const nueva = [toast, ...lista];
      for (const vieja of nueva.slice(MAX_TOASTS)) this.cancelarTemporizador(vieja.id);
      return nueva.slice(0, MAX_TOASTS);
    });

    this.temporizadores.set(
      id,
      setTimeout(() => this.cerrar(id), DURACION_TOAST[alerta.nivel] ?? 8000),
    );

    this.reproducir(alerta.nivel);
  }

  private cancelarTemporizador(id: number) {
    const t = this.temporizadores.get(id);
    if (t) clearTimeout(t);
    this.temporizadores.delete(id);
  }

  // ---------- Sonido (Web Audio API: no necesita archivos de audio) ----------

  private obtenerContexto(): AudioContext | undefined {
    if (this.contexto) return this.contexto;
    // Safari mas viejo usa el prefijo webkit.
    const Ctor: typeof AudioContext | undefined =
      window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return undefined;
    try {
      this.contexto = new Ctor();
    } catch {
      return undefined;
    }
    return this.contexto;
  }

  private reproducir(nivel: NivelAlerta) {
    if (this.silenciado()) return;
    const patron = PATRONES[nivel];
    if (!patron || patron.pitidos.length === 0) return;

    const ctx = this.obtenerContexto();
    if (!ctx) return;
    if (ctx.state === 'suspended') void ctx.resume();

    let t = ctx.currentTime + 0.03;
    for (const p of patron.pitidos) {
      this.pitar(ctx, patron.onda, patron.volumen, p.frecuencia, t, p.duracion);
      t += p.duracion + p.pausa;
    }
  }

  private pitar(ctx: AudioContext, onda: OscillatorType, volumen: number, frecuencia: number, inicio: number, duracion: number) {
    const osc = ctx.createOscillator();
    const ganancia = ctx.createGain();

    osc.type = onda;
    osc.frequency.setValueAtTime(frecuencia, inicio);

    // Subida y bajada suaves del volumen para que no suene "chasquido".
    ganancia.gain.setValueAtTime(0.0001, inicio);
    ganancia.gain.exponentialRampToValueAtTime(volumen, inicio + 0.02);
    ganancia.gain.exponentialRampToValueAtTime(0.0001, inicio + duracion);

    osc.connect(ganancia);
    ganancia.connect(ctx.destination);
    osc.start(inicio);
    osc.stop(inicio + duracion + 0.03);
  }

  // ---------- Preferencia "silenciar" (se recuerda en este navegador) ----------

  private leerSilenciado(): boolean {
    try {
      return localStorage.getItem(CLAVE_SILENCIO) === '1';
    } catch {
      return false;
    }
  }

  private guardarSilenciado(valor: boolean) {
    try {
      localStorage.setItem(CLAVE_SILENCIO, valor ? '1' : '0');
    } catch {
      /* modo privado o almacenamiento bloqueado: se ignora */
    }
  }
}
