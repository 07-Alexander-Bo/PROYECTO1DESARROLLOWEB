import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import { AlertaDto, EstadoSensorDto } from './models';

export type EstadoConexion = 'desconectado' | 'conectando' | 'conectado' | 'reconectando';

// Conexion en tiempo real con la API (SignalR, hub /hubs/monitoreo).
// El servidor EMPUJA lecturas, alertas y cambios; las pantallas solo se suscriben
// a los Subject de abajo. Lo inicia y lo detiene el Shell (solo hay conexion
// mientras el usuario tiene sesion iniciada).
@Injectable({ providedIn: 'root' })
export class TiempoRealService {
  private platformId = inject(PLATFORM_ID);

  private hub?: HubConnection;
  private detenido = true;
  private reintento?: ReturnType<typeof setTimeout>;

  readonly estado = signal<EstadoConexion>('desconectado');
  /** null = todavia no se sabe (sin conexion). */
  readonly simuladorActivo = signal<boolean | null>(null);

  /** Llego una lectura nueva (ya evaluada: valor, nivel, mensaje). */
  readonly lectura$ = new Subject<EstadoSensorDto>();
  readonly alertaNueva$ = new Subject<AlertaDto>();
  readonly alertaAtendida$ = new Subject<number>();
  /** Se creo, edito, activo o desactivo un sensor. */
  readonly sensoresActualizados$ = new Subject<void>();
  /** Un administrador reinicio el monitoreo: se borraron lecturas, alertas e historial. */
  readonly monitoreoReiniciado$ = new Subject<void>();
  /** Se emite cada vez que se (re)establece la conexion: las pantallas recargan por si se perdio algo. */
  readonly conectado$ = new Subject<void>();

  iniciar() {
    if (!isPlatformBrowser(this.platformId) || !this.detenido) return;
    this.detenido = false;

    // environment.apiUrl = 'http://localhost:5206/api'  ->  'http://localhost:5206/hubs/monitoreo'
    const urlHub = environment.apiUrl.replace(/\/api\/?$/, '') + '/hubs/monitoreo';

    const hub = new HubConnectionBuilder()
      .withUrl(urlHub, {
        // El token se lee en cada (re)conexion, asi siempre se manda el vigente.
        accessTokenFactory: () => localStorage.getItem('sc_token') ?? '',
        withCredentials: false,
      })
      // Reintenta para siempre: 2 s, 4 s, 6 s ... hasta 30 s entre intentos.
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (ctx) => Math.min(30000, 2000 * (ctx.previousRetryCount + 1)),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    // Los nombres deben coincidir con Hubs/EventosTiempoReal.cs del backend.
    hub.on('LecturaRecibida', (d: EstadoSensorDto) => this.lectura$.next(d));
    hub.on('AlertaNueva', (a: AlertaDto) => this.alertaNueva$.next(a));
    hub.on('AlertaAtendida', (id: number) => this.alertaAtendida$.next(id));
    hub.on('SensoresActualizados', () => this.sensoresActualizados$.next());
    hub.on('MonitoreoReiniciado', () => this.monitoreoReiniciado$.next());
    hub.on('SimuladorEstado', (activo: boolean) => this.simuladorActivo.set(activo));

    hub.onreconnecting(() => this.estado.set('reconectando'));
    hub.onreconnected(() => {
      this.estado.set('conectado');
      this.conectado$.next();
    });
    hub.onclose(() => {
      this.estado.set('desconectado');
      if (!this.detenido) this.programarReintento();
    });

    this.hub = hub;
    void this.conectar();
  }

  detener() {
    this.detenido = true;
    if (this.reintento) clearTimeout(this.reintento);
    this.reintento = undefined;

    const hub = this.hub;
    this.hub = undefined;
    this.estado.set('desconectado');
    this.simuladorActivo.set(null);

    hub?.stop().catch(() => {});
  }

  private async conectar() {
    const hub = this.hub;
    if (this.detenido || !hub) return;

    this.estado.set('conectando');
    try {
      await hub.start();

      // Si mientras conectaba el usuario cerro sesion, se cierra de inmediato.
      if (this.detenido || hub !== this.hub) {
        await hub.stop();
        return;
      }

      this.estado.set('conectado');
      this.conectado$.next();
    } catch {
      // La API no esta disponible (apagada, reiniciando...): se vuelve a intentar.
      if (!this.detenido && hub === this.hub) {
        this.estado.set('desconectado');
        this.programarReintento();
      }
    }
  }

  private programarReintento() {
    if (this.reintento) clearTimeout(this.reintento);
    this.reintento = setTimeout(() => void this.conectar(), 5000);
  }
}
